using UnityEngine;

namespace Submodules.Utility.Provider
{
    /// <summary>
    /// An <see cref="AbstractSceneSingleton{T}"/> that also promises to survive a scene load
    /// via <see cref="DontDestroyOnLoad"/> - which Unity only grants a root object, so this
    /// must be authored as one. <see cref="OnValidate"/> errors loudly on a non-root instance
    /// rather than relying on the runtime rescue in <see cref="OnResolved"/> to quietly patch
    /// it up. A component that must stay nested under a specific parent to function (a UI
    /// element under its Canvas) does not fit this contract - it should derive from
    /// <see cref="AbstractSceneSingleton{T}"/> directly instead.
    /// </summary>
    public abstract class AbstractProvider<T> : AbstractSceneSingleton<T> where T : MonoBehaviour
    {
        protected override void OnResolved()
        {
            // Edit-mode reads (an EditMode test, an [InitializeOnLoad] hook, an inspector)
            // must never mutate the scene - OnValidate is the sole author-time guard there.
            if (!Application.isPlaying) return;

            // Rescues a non-root instance authored before this was enforced at edit time
            // (see OnValidate) - belt-and-braces, not the primary guard.
            if (transform.parent != null)
            {
                Debug.LogWarning($"{name} was not a root object - reparented so DontDestroyOnLoad() can persist it", this);
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (transform.parent != null)
                Debug.LogError($"{name} must be authored as a root object - a provider promises " +
                    "to survive a scene load via DontDestroyOnLoad, which Unity only grants a " +
                    "root object. If this needs to live under something (a Canvas, a layout " +
                    "group) it is not a cross-scene singleton and should derive from " +
                    "AbstractSceneSingleton<T> instead.", gameObject);
        }
#endif // UNITY_EDITOR
    }
}
