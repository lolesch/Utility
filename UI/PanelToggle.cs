using Submodules.Utility.UI.InteractiveElements;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class PanelToggle : AbstractToggle
    {
        [SerializeField] protected AbstractPanel panel;

        protected override void ToggleSideEffects()
        {
            if (!panel) 
                return;
            
            if (IsOn)
                panel.FadeIn();
            else
                panel.FadeOut();
        }
    }
}
