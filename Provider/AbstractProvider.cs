using System.Text.RegularExpressions;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Submodules.Utility.Provider
{
    [DefaultExecutionOrder(0)]
    public abstract class AbstractProvider<T> : MonoBehaviour where T : MonoBehaviour
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

                if (_instance == null)
                {
                    if (!InstanceExists())
                        CreateNewInstance();

                    if (Application.isPlaying)
                        DontDestroyOnLoad(_instance.gameObject);
                }

                return _instance;

                static bool InstanceExists()
                {
                    var candidates = FindObjectsOfType<T>();

                    if (0 >= candidates.Length) 
                        return false;
                    
                    _instance = candidates[0];
                    _instance.name = GetProviderName();

                    Debug.Log($"Found existing {_instance.name.ColoredComponent()}", _instance);

                    // instance as component of "non-root" gameObjects
                    if (_instance.transform.parent != null)
                    {
                        Debug.LogWarning($"{_instance.name.Colored(Color.yellow)} was not a root object - reparented so DontDestroyOnLoad() can persist it", _instance);
                        _instance.transform.SetParent(null);
                    }

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

        private void Start()
        {
            if (Instance == this) return;

            DisableCandidate(this as T);
        }

        private static string GetProviderName() => Regex.Replace(typeof(T).Name, "(?<=[a-z])([A-Z])", "_$1", RegexOptions.Compiled).ToUpper();

        private static void DisableCandidate( T candidate)
        {
            Debug.Log($"Disabled {_instance.name.Colored(Color.red)} because there is already an Instance!", candidate);

            candidate.enabled = false;
        }

        private void OnApplicationQuit() => _isQuitting = true;
    }
}
