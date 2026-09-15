using Submodules.Utility.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class QuitGameButton : AbstractButton
    {
        protected override void OnClick()
        {
            // TODO
            // - implement a confirmationPrompt
            // - save progression?

            Debug.LogWarning("Quitting the game".Colored(Color.red));

#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#elif UNITY_WEBPLAYER
            Application.OpenURL(webplayerQuitURL);
#else
            Application.Quit();
#endif
        }
    }
}
