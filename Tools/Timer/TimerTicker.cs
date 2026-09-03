using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.Tools.Timer
{
    internal static class TimerTicker
    {
        private static List<ITimer> timers = new();

        // Kept apart from the timer list so Tween.ActiveCount and the target queries stay
        // O(tweens), and reused as the tick buffer below so a running tween costs no
        // per-frame allocation.
        private static readonly List<ITickable> tickables = new();
        private static readonly List<ITickable> tickBuffer = new();

        public static void RegisterTimer( ITimer timer ) => timers.Add( timer );
        public static void DeregisterTimer( ITimer timer ) => timers.Remove( timer );

        public static void Register( ITickable tickable ) => tickables.Add( tickable );
        public static void Deregister( ITickable tickable ) => tickables.Remove( tickable );

        public static void TickTimers()
        {
            var deltaTime = Time.deltaTime;

            foreach( var timer in new List<ITimer>( timers ) )
                timer.Tick( deltaTime );

            tickBuffer.Clear();
            tickBuffer.AddRange( tickables );
            foreach( var tickable in tickBuffer )
                tickable.Tick( deltaTime );
            tickBuffer.Clear();
        }

        public static void Clear()
        {
            timers.Clear();
            tickables.Clear();
            tickBuffer.Clear();
        }
    }
}
