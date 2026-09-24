using System;
using System.Text.RegularExpressions;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Submodules.Utility.Provider
{
    /// <summary>
    /// Exactly one enabled <typeparamref name="T"/> in the scene - whichever is found or
    /// created first, with every later duplicate disabled. This is the whole of the
    /// contract: no opinion on where in the hierarchy it lives or whether it survives a scene
    /// load. <see cref="AbstractProvider{T}"/> is this plus that promise, for the common case
    /// of a game-logic singleton; a component that must stay nested under a specific parent
    /// to function (a UI element under its Canvas) derives from this directly instead.
    /// </summary>
    [DefaultExecutionOrder(0)]
    public abstract class AbstractSceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance = null;
        private static bool _isQuitting = false;

        public static T Instance
        {
            get
            {
                // A teardown-ordering pitfall: something disabling during shutdown (e.g. a
                // display's OnDisable) can still ask for Instance after the real one was
                // already destroyed, which would otherwise spin up a fresh, unconfigured
                // instance that immediately leaks past scene teardown.
                if (_isQuitting)
                    return _instance;

                if (_instance != null) 
                    return _instance;
                
                if (!InstanceExists())
                    CreateNewInstance();

                (_instance as AbstractSceneSingleton<T>)?.OnResolved();

                return _instance;

                static bool InstanceExists()
                {
                    var candidates = FindObjectsOfType<T>();

                    if (0 >= candidates.Length)
                        return false;

                    _instance = candidates[0];
                    _instance.name = GetProviderName();

                    Debug.Log($"Found existing {_instance.name.ColoredComponent()}", _instance);

                    // disable remaining instances
                    for (var i = candidates.Length; i-- > 1;)
                    {
                        if (candidates[i] == null) continue;
                        DisableCandidate(candidates[i]);
                    }

                    return true;
                }

                static void CreateNewInstance()
                {
                    _instance = new GameObject(GetProviderName()).AddComponent<T>(); // this calls Awake on the new GameObject

                    Debug.Log($"Created new {_instance.name.ColoredComponent()}", _instance);
                }
            }
        }

        /// <summary>
        /// Runs once, right after this becomes the resolved <see cref="Instance"/> - whether
        /// found already in the scene or freshly created. The base does nothing; override to
        /// layer on more than plain scene-singleton resolution (see
        /// <see cref="AbstractProvider{T}"/>).
        /// </summary>
        protected virtual void OnResolved() { }

        private void Start()
        {
            if (Instance != this)
                DisableCandidate(this as T);
        }

        private void Reset() => name = GetProviderName();

        private static string GetProviderName() => Regex.Replace(typeof(T).Name, "(?<=[a-z])([A-Z])", "_$1", RegexOptions.Compiled).ToUpper();

        private static void DisableCandidate(T candidate)
        {
            Debug.Log($"Disabled {_instance.name.Colored(Color.red)} because there is already an Instance!", candidate);

            candidate.enabled = false;
        }

        private void OnApplicationQuit() => _isQuitting = true;
    }
}
