using UnityEngine;

namespace Submodules.Utility.UI
{
    public class PanelToggle : AbstractToggle
    {
        [SerializeField] protected AbstractPanel panel;

        public override void SetToggle(bool isOn)
        {
            base.SetToggle(isOn);

            if (panel)
                if (isOn)
                    panel.FadeIn();
                else
                    panel.FadeOut();
        }
    }
}
