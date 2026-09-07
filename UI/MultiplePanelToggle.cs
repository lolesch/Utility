using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class MultiplePanelToggle : AbstractToggle
    {
        [SerializeField] protected List<AbstractPanel> panelsToTurnOn;
        [SerializeField] protected List<AbstractPanel> panelsToTurnOff;

        public override void SetToggle(bool isOn)
        {
            base.SetToggle(isOn);

            foreach (var panel in panelsToTurnOn)
            {
                if (panel == null) continue;
                if (isOn)
                    panel.FadeIn();
                else
                    panel.FadeOut();
            }

            foreach (var panel in panelsToTurnOff)
            {
                if (panel == null) continue;
                if (isOn)
                    panel.FadeOut();
                else
                    panel.FadeIn();
            }
        }
    }
}
