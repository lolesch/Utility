using Submodules.Utility.Extensions;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class HyperlinkButton : AbstractButton
    {
        [SerializeField] private string linkToOpen;

        protected override void OnClick()
        {
            if (Application.isPlaying)
                Application.OpenURL(linkToOpen);

            Debug.Log($"OPEN URL:\t{linkToOpen.Colored(ColorExtensions.LightBlue)}");
        }
    }
}
