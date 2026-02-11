using System;
using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.Tools.Timer
{
    [Serializable]
    public sealed class Stopwatch : IStopwatch
    {
        [SerializeField, ReadOnly, AllowNesting] private float timeElapsed;

        public static implicit operator float( Stopwatch stopwatch ) => stopwatch!.timeElapsed;
        public void Reset() => timeElapsed = 0;
        public void Tick( float tickInterval ) => timeElapsed += tickInterval;
    }

    public interface IStopwatch
    {
        //bool IsRunning { get; }
        void Tick( float tickInterval );
        void Reset();
        //void Pause();
        //void Resume();
    }
}