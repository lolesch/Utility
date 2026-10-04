using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Submodules.Utility.Editor
{
    /// <summary>
    /// The Play-exit check: enters Play Mode in a scene, runs a few frames, stops, and fails if
    /// anything at <c>Error</c> level (or an exception or assert) is logged between the Stop and the
    /// return to Edit Mode - the window in which every <c>OnDisable</c> / <c>OnDestroy</c> runs.
    /// It exists because the services, the timer pump and the locator are armed before the first
    /// scene and have to outlive its teardown: a view that reads one on the way out, or a static
    /// cleared too early, throws once per object, and no EditMode or PlayMode test can see it (a
    /// PlayMode test cannot stop Play Mode). It lives here, with the lifetimes it guards, so every
    /// project that uses this module can run it.
    ///
    /// Run it headless, with the project not open in an Editor:
    /// <code>Unity.exe -batchmode -nographics -projectPath &lt;proj&gt; -executeMethod Submodules.Utility.Editor.PlayExitCheck.Run -playExitScene Assets/Scenes/Example.unity -logFile out.log</code>
    /// The process exit code is the verdict: 0 clean, 1 teardown errors (listed in the log under
    /// <c>[PlayExitCheck]</c>), 2 never got back to Edit Mode, 3 no scene given. It does nothing
    /// unless <see cref="Run"/> started it, so it is inert in a normal Editor session.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayExitCheck
    {
        private const string SceneArgument = "-playExitScene";
        private const string Flag = "Temp/playexitcheck.flag";
        private const int FramesInPlay = 20;
        private const double TimeoutSeconds = 300;

        private static readonly List<string> teardownErrors = new();
        private static int frames;
        private static bool stopped;
        private static double startedAt;

        static PlayExitCheck()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Application.logMessageReceived += OnLog;
        }

        public static void Run()
        {
            var scene = SceneFromCommandLine();

            if (scene == null)
            {
                Debug.LogError($"[PlayExitCheck] pass the scene to play: {SceneArgument} Assets/Scenes/<name>.unity");
                EditorApplication.Exit(3);
                return;
            }

            _ = EditorSceneManager.OpenScene(scene);
            File.WriteAllText(Flag, "go");
            startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.EnterPlaymode();
        }

        private static string SceneFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == SceneArgument)
                    return args[i + 1];

            return null;
        }

        private static void Tick()
        {
            if (!File.Exists(Flag))
                return;

            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                Debug.LogError("[PlayExitCheck] timed out before Edit Mode came back");
                Finish(2);
                return;
            }

            if (stopped || !EditorApplication.isPlaying || ++frames < FramesInPlay)
                return;

            stopped = true;
            Debug.Log("[PlayExitCheck] stopping Play Mode");
            EditorApplication.ExitPlaymode();
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (stopped && type is LogType.Error or LogType.Exception or LogType.Assert)
                teardownErrors.Add(message.Split('\n')[0]);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!File.Exists(Flag) || !stopped || state != PlayModeStateChange.EnteredEditMode)
                return;

            foreach (var error in teardownErrors)
                Debug.Log($"[PlayExitCheck] teardown error: {error}");

            Debug.Log($"[PlayExitCheck] back in Edit Mode with {teardownErrors.Count} teardown error(s)");
            Finish(teardownErrors.Count == 0 ? 0 : 1);
        }

        private static void Finish(int exitCode)
        {
            File.Delete(Flag);
            EditorApplication.Exit(exitCode);
        }
    }
}
