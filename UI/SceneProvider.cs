using Submodules.Utility.Provider;
using System;
using System.Collections;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    [RequireComponent(typeof(RootCanvas), typeof(LoadingScreenPanel), typeof(Image))]
    public class SceneProvider : AbstractProvider<SceneProvider>
    {
        private LoadingScreenPanel display = null;
        private LoadingScreenPanel Display => display ? display : display = GetComponent<LoadingScreenPanel>();

        private float delta;
        private float timeStamp;
        private float targetProgress;
        private float currentProgress;

        /// <summary>True from the moment a load is accepted until its loading screen has faded out again.</summary>
        public bool IsLoading { get; private set; }

        public event Action<string> OnSceneLoaded;

        private void OnEnable()
        {
            var rootCanvas = GetComponent<RootCanvas>();
            rootCanvas.name = "[LOADING_SCREEN]";
            rootCanvas.Canvas.sortingOrder = 1;

            var image = GetComponent<Image>();
            if (image.sprite == null)
                image.color = Color.black;

            Display.Collapse();
        }

        /// <summary>
        /// Starts a scene change. <c>false</c>, with a log line and nothing started (no fade), when another load is
        /// still under way, the name is blank or the build does not hold the scene.
        /// </summary>
        public bool LoadScene(string sceneToLoad, bool showProgress = true)
        {
            if (IsLoading)
            {
                Debug.LogError("Can't load a sceneName while another is still loading");
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                Debug.LogError($"Cant load '{sceneToLoad}' as sceneName");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
            {
                Debug.LogError($"Can't load '{sceneToLoad}': it is not in the build");
                return false;
            }

            // Set before the first yield, so a second request in the same frame is refused too.
            IsLoading = true;
            _ = StartCoroutine(LoadNextScene(sceneToLoad, showProgress));
            return true;
        }

        private IEnumerator LoadNextScene(string sceneToLoad, bool showProgress = true)
        {
            try
            {
                yield return null;

                Debug.Log($"{"LOADING:".Colored(ColorExtensions.Orange)}\t{sceneToLoad.Colored(ColorExtensions.LightBlue)}");

                if (Display && showProgress)
                {
                    Display.SetLoadingProgression(0);
                    Display.SetLoadingText(sceneToLoad);
                    Display.Expand();

                    timeStamp = Time.unscaledTime;

                    // Wait for Expand
                    yield return new WaitWhile(() => Time.unscaledTime - timeStamp < Display.FadeDuration);

                    var loaderOperation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
                    loaderOperation.allowSceneActivation = false;

                    currentProgress = 0;

                    // Wait for progressionBar
                    while (currentProgress < 1)
                    {
                        yield return null;

                        // map progress (from 0f to .9f) to 0f to 1f
                        targetProgress = loaderOperation.progress.MapTo01(0, 0.9f);

                        // Wait for the new sceneName to preload
                        if (targetProgress < .45f)
                            targetProgress *= 0.2f;

                        delta += Time.unscaledDeltaTime * 0.2f;

                        currentProgress = Mathf.Clamp01(Mathf.Lerp(currentProgress, targetProgress, delta));

                        Display.SetLoadingProgression(currentProgress);

                        if (targetProgress >= 1 && !loaderOperation.allowSceneActivation)
                            loaderOperation.allowSceneActivation = true;
                    }

                    yield return new WaitForSeconds(.3f);

                    Display.Collapse();

                    yield return null;
                }
                else
                {
                    LogExtensions.MissingComponent(nameof(LoadingScreenPanel), gameObject);

                    var loaderOperation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);

                    loaderOperation.allowSceneActivation = false;

                    yield return new WaitWhile(() => (loaderOperation.progress / .9f) < 1);

                    loaderOperation.allowSceneActivation = true;

                    yield return null;
                }
            }
            finally
            {
                // Every way out, an exception included, frees the loader; a handler of OnSceneLoaded may load next.
                IsLoading = false;
            }

            OnSceneLoaded?.Invoke(sceneToLoad);
        }
    }
}
