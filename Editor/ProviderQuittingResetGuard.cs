using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Submodules.Utility.Provider;
using UnityEditor;

namespace Submodules.Utility.Editor
{
    /// <summary>
    /// <see cref="AbstractProvider{T}"/> sets a static <c>_isQuitting</c> flag on
    /// <c>OnApplicationQuit</c> and never clears it — harmless in a build, where the process
    /// exits right after quitting and there is no "next session" to leak into. But with this
    /// project's domain reload disabled (<c>ProjectSettings/EditorSettings.asset</c>,
    /// 2026-09-18) that static state survives Stop: the next Play entry inherits
    /// <c>_isQuitting == true</c>, and every <see cref="AbstractProvider{T}.Instance"/> across
    /// the project then returns null for the rest of the Editor session (see
    /// <c>docs/agents/codebase-notes.md</c>).
    ///
    /// Editor-only fix for an editor-only problem: reset each provider's flag the moment Play
    /// is pressed, mirroring what a fresh process would do. Reflection is fine here — only the
    /// AI-assistant's dynamic RunCommand scripts choke on <c>System.Reflection</c>, not a
    /// compiled editor assembly (see <see cref="SceneSavePromptGuard"/>).
    /// </summary>
    [InitializeOnLoad]
    internal static class ProviderQuittingResetGuard
    {
        static ProviderQuittingResetGuard() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            foreach (var providerBase in AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(SafeGetTypes)
                         .Select(ClosedAbstractProviderBaseOf)
                         .Where(t => t != null && !t.ContainsGenericParameters)
                         .Distinct())
                providerBase.GetField("_isQuitting", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, false);
        }

        private static Type ClosedAbstractProviderBaseOf(Type type)
        {
            for (var t = type; t != null; t = t.BaseType)
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AbstractProvider<>))
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
