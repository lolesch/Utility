using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Submodules.Utility.Provider;
using UnityEditor;

namespace Submodules.Utility.Editor
{
    /// <summary>
    /// <see cref="AbstractSceneSingleton{T}"/> sets a static <c>_isQuitting</c> flag on
    /// <c>OnApplicationQuit</c> and never clears it — harmless in a build, where the process
    /// exits right after quitting and there is no "next session" to leak into. But with this
    /// project's domain reload disabled (<c>ProjectSettings/EditorSettings.asset</c>,
    /// 2026-09-18) that static state survives Stop: the next Play entry inherits
    /// <c>_isQuitting == true</c>, and every <see cref="AbstractSceneSingleton{T}.Instance"/>
    /// across the project then returns null for the rest of the Editor session (see
    /// <c>docs/agents/codebase-notes.md</c>).
    ///
    /// Editor-only fix for an editor-only problem: reset each singleton's flag the moment Play
    /// is pressed, mirroring what a fresh process would do. Reflection is fine here — only the
    /// AI-assistant's dynamic RunCommand scripts choke on <c>System.Reflection</c>, not a
    /// compiled editor assembly (see <see cref="SceneSavePromptGuard"/>).
    ///
    /// Targets the closed <see cref="AbstractSceneSingleton{T}"/>, not
    /// <see cref="AbstractProvider{T}"/>: <c>_isQuitting</c> is declared on the former, and a
    /// private field declared on a base type is never visible through <c>GetField</c> on a
    /// derived type — <c>BindingFlags.FlattenHierarchy</c> only surfaces inherited public/
    /// protected static members, never private ones. An earlier version of this guard targeted
    /// <c>AbstractProvider&lt;&gt;</c> (the only subclass that existed at the time); the later
    /// split that introduced <see cref="AbstractSceneSingleton{T}"/> as a separate base moved
    /// the field out from under it, so the reset silently stopped firing for every provider,
    /// and never reached direct <see cref="AbstractSceneSingleton{T}"/> consumers
    /// (<c>DragProvider</c>, <c>PreviewProvider</c>) at all.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProviderQuittingResetGuard
    {
        static ProviderQuittingResetGuard() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            foreach (var singletonBase in AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(SafeGetTypes)
                         .Select(ClosedAbstractSceneSingletonBaseOf)
                         .Where(t => t != null && !t.ContainsGenericParameters)
                         .Distinct())
                singletonBase.GetField("_isQuitting", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, false);
        }

        private static Type ClosedAbstractSceneSingletonBaseOf(Type type)
        {
            for (var t = type; t != null; t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AbstractSceneSingleton<>))
                    return t;
            return null;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
        }
    }
}
