using System;
using System.Collections.Generic;
using UnityEngine.LowLevel;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Submodules.Utility.Tools
{
    /// <summary>
    /// Runs a plain delegate every frame from the player loop, with no <c>MonoBehaviour</c> and no
    /// GameObject: nothing in the hierarchy, nothing to hide, destroy or leak. A system is
    /// identified by its <c>marker</c> type, and <see cref="Install{TPhase}"/> first removes any
    /// system with that marker, so a hook is installed at most once however many Play entries
    /// ran. Hooks are removed on leaving Play Mode, because the player loop is global and, with
    /// domain reload disabled, outlives Stop (the same reason <c>TimerBootstrapper</c> tears its
    /// system down).
    ///
    /// The system is appended to the end of its phase, so a hook in <c>Update</c> runs after every
    /// <c>MonoBehaviour.Update</c>.
    /// </summary>
    public static class PlayerLoopHook
    {
        private static readonly HashSet<Type> installed = new();

        public static bool IsInstalled(Type marker) => Contains(PlayerLoop.GetCurrentPlayerLoop(), marker);

        /// <summary>Appends <paramref name="update"/> to the <typeparamref name="TPhase"/> phase
        /// (e.g. <c>UnityEngine.PlayerLoop.Update</c>), replacing any system with the same
        /// <paramref name="marker"/>.</summary>
        public static void Install<TPhase>(Type marker, PlayerLoopSystem.UpdateFunction update)
        {
            if (marker == null)
                throw new ArgumentNullException(nameof(marker));
            if (update == null)
                throw new ArgumentNullException(nameof(update));

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            _ = RemoveAll(ref loop, marker);

            var system = new PlayerLoopSystem { type = marker, updateDelegate = update };

            if (!Append(ref loop, typeof(TPhase), system))
                throw new InvalidOperationException($"The player loop has no {typeof(TPhase).Name} phase to install {marker.Name} into.");

            PlayerLoop.SetPlayerLoop(loop);
            _ = installed.Add(marker);

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        public static void Remove(Type marker)
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();

            if (RemoveAll(ref loop, marker))
                PlayerLoop.SetPlayerLoop(loop);

            _ = installed.Remove(marker);
        }

#if UNITY_EDITOR
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
                return;

            foreach (var marker in new List<Type>(installed))
                Remove(marker);
        }
#endif

        private static bool Contains(in PlayerLoopSystem loop, Type marker)
        {
            if (loop.subSystemList == null)
                return false;

            foreach (var sub in loop.subSystemList)
                if (sub.type == marker || Contains(sub, marker))
                    return true;

            return false;
        }

        private static bool RemoveAll(ref PlayerLoopSystem system, Type marker)
        {
            if (system.subSystemList == null)
                return false;

            var changed = false;
            var kept = new List<PlayerLoopSystem>(system.subSystemList.Length);

            foreach (var sub in system.subSystemList)
            {
                if (sub.type == marker)
                    changed = true;
                else
                    kept.Add(sub);
            }

            if (changed)
                system.subSystemList = kept.ToArray();

            for (var i = system.subSystemList.Length; i-- > 0;)
                changed |= RemoveAll(ref system.subSystemList[i], marker);

            return changed;
        }

        private static bool Append(ref PlayerLoopSystem system, Type phase, in PlayerLoopSystem toAppend)
        {
            if (system.type == phase)
            {
                var list = new List<PlayerLoopSystem>(system.subSystemList ?? Array.Empty<PlayerLoopSystem>()) { toAppend };
                system.subSystemList = list.ToArray();
                return true;
            }

            if (system.subSystemList == null)
                return false;

            for (var i = 0; i < system.subSystemList.Length; i++)
                if (Append(ref system.subSystemList[i], phase, toAppend))
                    return true;

            return false;
        }
    }
}
