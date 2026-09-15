using UnityEngine;

namespace Submodules.Utility.UI
{
    public class PanelToggle : AbstractToggle
    {
        [SerializeField] protected SimplePanel panel;

        [Tooltip("When set, the panel shows while this toggle is off and hides while it is on")]
        [SerializeField] private bool invert;

        protected override void OnToggle()
        {
            if (!panel)
                return;

            panel.Toggle(invert ? !IsOn : IsOn);
        }
    }
}
