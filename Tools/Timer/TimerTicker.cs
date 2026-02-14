using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.Tools.Timer
{
    internal static class TimerTicker
    {
        private static List<ITimer> timers = new();
        
        public static void RegisterTimer( ITimer timer ) => timers.Add( timer );
        public static void DeregisterTimer( ITimer timer ) => timers.Remove( timer );
        
        public static void TickTimers()
        {
            foreach( var timer in new List<ITimer>(timers) )
                timer.Tick( Time.deltaTime );
        }
        
        public static void Clear() => timers.Clear();
    }
}