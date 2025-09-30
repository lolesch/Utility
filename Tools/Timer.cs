using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;

namespace Submodules.Utility.Tools
{
    [Serializable]
    public sealed class Timer : ITimer
    {
        [SerializeField, ReadOnly] private float duration;
        [SerializeField, ReadOnly] private int laps;
        [SerializeField] private Stopwatch stopwatch;

        public Timer( float duration, int laps = 1, bool startFinished = false )
        {
            Assert.IsTrue( 0 < duration, $"Duration {duration} must be positive" );
            Assert.IsTrue( 0 <= laps, $"Repetitions {laps} must be positive" );

            this.duration = duration;
            this.laps = laps;
            stopwatch = new Stopwatch();
            
            if( startFinished )
                stopwatch?.Tick( duration );
        }

        public static implicit operator float( Timer timer ) => timer.duration;
        
        public event Action OnRewind;
        public event Action OnComplete;
        public event Action<float> OnTick;
        
        public float remaining => Mathf.Clamp( duration - stopwatch, 0, duration );
        public float progress01 => 1 - Mathf.Clamp01( remaining / duration );
        
        public bool Tick( float tickInterval )
        { 
            if ( laps < 1 )
            {
               Debug.LogWarning("This timer has expired and should no longer receive ticks");
               return false;
            } 
            
            stopwatch?.Tick( tickInterval );
            OnTick?.Invoke( progress01 );
            
            if ( stopwatch < duration )
                return false;

            laps--;
           
            if ( 0 < laps )
                Rewind();
            
            if ( laps == 0 )
                OnComplete?.Invoke();
            
            return true;
        }

        public void Restart( int laps = 1 )
        {
            stopwatch?.Reset();
            this.laps = laps;
        }

        public void Repeat( int amount = 1 ) => laps += amount;
        
        private void Rewind()
        {
            stopwatch?.Tick( -duration );
            OnRewind?.Invoke();
        }
    }

    public interface ITimer
    {
        float remaining { get; }
        float progress01 { get; }
        bool Tick( float tickInterval );
        void Restart( int laps = 1 );
        void Repeat( int amount = 1 );

        event Action OnRewind;
        event Action OnComplete;
        event Action<float> OnTick;
    }
}
