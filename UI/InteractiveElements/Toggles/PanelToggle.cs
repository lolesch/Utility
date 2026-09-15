using UnityEngine;

namespace Submodules.Utility.UI
{
    public class PanelToggle : AbstractToggle
    {
        [SerializeField] protected AbstractPanel panel;

        protected override void OnToggle()
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
