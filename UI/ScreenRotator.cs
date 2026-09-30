using UnityEngine;

namespace Submodules.Utility.UI
{
    public sealed class ScreenRotator : RootCanvas
    {
        private void Update()
        {
            if (Screen.orientation == orientation) 
                return;
            
            orientation = Screen.orientation;

            Screen.orientation = Screen.width < Screen.height 
                ? ScreenOrientation.LandscapeRight 
                : ScreenOrientation.AutoRotation;
        }
    }
}
