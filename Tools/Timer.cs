using System;
using Sirenix.Serialization;
using Submodules.Utility.Tools.Statistics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Submodules.Utility.Tools
{
    public sealed class Timer
    {
        [OdinSerialize] private MutableFloat _duration;
        [OdinSerialize] private Stopwatch _stopwatch = new();
        [OdinSerialize] public int Laps { get; private set; }

        public event Action OnRewind;
        public event Action OnComplete;
        public event Action<float> OnTick;
        
        public float Remaining => Mathf.Clamp( _duration - _stopwatch, 0, _duration );
        public float Progress01 => 1 - Mathf.Clamp01( Remaining / _duration );

        public Timer( float duration, int laps = 1, bool startFinished = false )
        {
            Assert.IsTrue( 0 < duration, $"Duration {duration} must be positive" );
            Assert.IsTrue( 0 <= laps, $"Repetitions {laps} must be positive" );

            _duration = new MutableFloat( duration );
            Laps = laps;
            
            if( startFinished )
                _stopwatch.Tick( duration );
        }

       public bool Tick( float tickInterval )
       {
           if ( Laps < 1 )
           {
               Debug.LogWarning("This timer has expired and should no longer receive ticks");
               return false;
           }

            _stopwatch.Tick( tickInterval );
            OnTick?.Invoke( Progress01 );
            
            if ( _stopwatch < _duration )
                return false;

            Laps--;
           
            if ( 0 < Laps )
                Rewind();
            
            if ( Laps == 0 )
                OnComplete?.Invoke();
            
            return true;
        }

       private void Rewind()
       {
           _stopwatch.Tick( -_duration );
           OnRewind?.Invoke();
       }

        public void Restart( int laps = 1 )
        {
            _stopwatch.Reset();
            Laps = laps;
        }

        public void Repeat( int amount = 1 ) => Laps += amount;
        
        public void AddModifier( Modifier modifier ) => _duration.AddModifier( modifier );
        public bool TryRemoveModifier( Modifier modifier ) => _duration.TryRemoveModifier( modifier );

        public static implicit operator float( Timer timer ) => timer._duration;
    }
}
