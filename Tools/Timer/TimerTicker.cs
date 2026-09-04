using System.Collections.Generic;
using Submodules.Utility.Tools.Tweening;
using UnityEngine;

namespace Submodules.Utility.Tools.Timer
{
    internal static class TimerTicker
    {
        private static List<ITimer> timers = new();

        // The one registry of running tweens: the tick loop, Tween.ActiveCount and the
        // target queries all read it, and Clear() resets it alongside the timers so a
        // play session that ends mid-tween leaves nothing behind.
        private static readonly List<Tween> tweens = new();
        private static readonly List<Tween> tickBuffer = new();

        internal static IReadOnlyList<Tween> Tweens => tweens;

        public static void RegisterTimer( ITimer timer ) => timers.Add( timer );
        public static void DeregisterTimer( ITimer timer ) => timers.Remove( timer );

        public static void RegisterTween( Tween tween ) => tweens.Add( tween );
        public static void DeregisterTween( Tween tween ) => tweens.Remove( tween );

        public static void TickTimers()
        {
            var deltaTime = Time.deltaTime;

            foreach( var timer in new List<ITimer>( timers ) )
                timer.Tick( deltaTime );

            // Reused buffer, not a fresh list: a running tween adds no per-frame garbage.
            tickBuffer.Clear();
            tickBuffer.AddRange( tweens );
            foreach( var tween in tickBuffer )
                tween.Tick( deltaTime );
            tickBuffer.Clear();
        }

        public static void Clear()
        {
            timers.Clear();
            tweens.Clear();
            tickBuffer.Clear();
        }
    }
}
