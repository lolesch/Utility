using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Submodules.Utility.Tools.Timer
{
    internal static class TimerBootstrapper
    {
        static PlayerLoopSystem timerSystem;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            
            if( !InsertTimerManager<Update>( ref currentPlayerLoop, 0 ) )
                Debug.LogError("Failed to insert timer manager into Update loop");
            
            PlayerLoop.SetPlayerLoop( currentPlayerLoop );
            //PlayerLoopUtils.PrintPlayerLoop( currentPlayerLoop );

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
            static void OnPlayModeStateChanged( PlayModeStateChange state )
            {
                if( state == PlayModeStateChange.ExitingPlayMode )
                {
                    var currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                    RemoveTimerManager<Update>( ref currentPlayerLoop );
                    PlayerLoop.SetPlayerLoop( currentPlayerLoop );
                    
                    TimerTicker.Clear();
                }
            }
        }
        
        static void RemoveTimerManager<T>( ref PlayerLoopSystem loop )
        {
            PlayerLoopUtils.RemoveSystem<T>( ref loop, in timerSystem );
        }
        
        static bool InsertTimerManager<T>( ref PlayerLoopSystem loop, int index )
        {
            timerSystem = new PlayerLoopSystem
            {
                type = typeof(TimerTicker),
                updateDelegate = TimerTicker.TickTimers,
                subSystemList = null
            };
            
            return PlayerLoopUtils.InsertSystem<T>( ref loop, timerSystem, index );
        }
    }
}