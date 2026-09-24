#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Submodules.Utility.Tools.Timer
{
    internal static class TimerBootstrapper
    {
        static PlayerLoopSystem timerSystem;
        
        // AfterAssembliesLoaded ties this to assembly loading, which does not happen again on
        // Play re-entry once domain reload is disabled (ProjectSettings/EditorSettings.asset,
        // 2026-09-18) — see docs/agents/codebase-notes.md. AfterSceneLoad, like
        // SimulationProvider's own [RuntimeInitializeOnLoadMethod], re-fires on every Play
        // entry regardless, so the tween pump is reinstalled instead of staying torn down
        // after the first Stop.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
#endif
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