using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;

namespace Submodules.Utility.Tools.Timer
{
    [Serializable]
    public sealed class Timer : ITimer
    {
        [SerializeField, ReadOnly, AllowNesting] private float duration;
        [SerializeField, ReadOnly, AllowNesting] private bool repeat;
        [SerializeField] private Stopwatch stopwatch;

        public Timer( float duration ) : this( duration, false ) { }
        
        public Timer( float duration, bool repeat )
        {
            Assert.IsTrue( 0 < duration, $"Duration {duration} must be positive" );

            this.duration = duration;
            this.repeat = repeat;
            stopwatch = new Stopwatch();
        }

        public static implicit operator float( Timer timer ) => timer.duration;
        
        public event Action OnRewind;
        public event Action OnComplete;
        //public event Action<float> OnTick;
        
        public float remaining => Mathf.Clamp( duration - stopwatch, 0, duration );
        public float progress01 => 1 - Mathf.Clamp01( remaining / duration );
        public bool IsRunning { get; private set; }

        public void Start()
        {
            stopwatch?.Reset();
            if( IsRunning ) 
                return;
            
            IsRunning = true;
            TimerTicker.RegisterTimer( this );
            OnRewind?.Invoke();
        }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public void Stop()
        {
            if( !IsRunning )
                return;
            
            IsRunning = false;
            TimerTicker.DeregisterTimer( this );
            OnComplete?.Invoke();
        }
        
        private void Rewind()
        {
            stopwatch?.Tick( -duration );
            OnRewind?.Invoke();
        }

        public void Tick( float tickInterval )
        {
            if( !IsRunning)
                return;
            
            stopwatch?.Tick( tickInterval );
            //OnTick?.Invoke( progress01 );
            
            if( 0 < remaining )
                return;
            
            if( repeat )
                Rewind();
            else
                Stop();
        }

        public void Dispose()
        {
            TimerTicker.DeregisterTimer( this );
        }
    }

    public interface ITimer : IDisposable
    {
        float remaining { get; }
        float progress01 { get; }
        bool IsRunning { get; }
        void Tick( float tickInterval );
        void Start();
        void Pause();
        void Resume();
        void Stop();

        event Action OnRewind;
        event Action OnComplete;
        //event Action<float> OnTick;
    }
}
