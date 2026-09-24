using UnityEngine;

namespace Submodules.Utility.UI
{
    public class PanelToggle : AbstractToggle
    {
        [SerializeField] protected SimplePanel panel;
        
        protected override void OnToggle()
        {
            if (!panel)
                return;

            panel.Toggle(IsOn);
        }
    }
}
