using UnityEngine;

namespace Submodules.Utility.UI
{
    public class PanelToggle : AbstractToggle
    {
        [Space]
        [SerializeField] protected SimplePanel panel;
        
        protected override void OnToggle()
        {
            if (!panel)
                return;

            panel.ToggleState(IsOn);
        }
    }
}
