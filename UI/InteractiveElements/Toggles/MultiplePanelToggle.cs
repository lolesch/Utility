using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class MultiplePanelToggle : AbstractToggle
    {
        [SerializeField] private List<AbstractPanel> panelsToTurnOn;
        [SerializeField] private List<AbstractPanel> panelsToTurnOff;
        [SerializeField] private List<RadioGroup> groupsToTurnOff;

        protected override void OnToggle()
        {
            foreach (var panel in panelsToTurnOn)
            {
                if (panel == null) continue;
                if (IsOn)
                    panel.FadeIn();
                else
                    panel.FadeOut();
            }

            foreach (var panel in panelsToTurnOff)
            {
                if (panel == null) continue;
                if (IsOn)
                    panel.FadeOut();
                else
                    panel.FadeIn();
            }

            foreach (var rg in groupsToTurnOff)
            {
                if (rg == null) continue;
                if (IsOn && rg.ActivatedToggle != null)
                    rg.ActivatedToggle.SetToggle(false);
                else if (!IsOn && rg.PreviouslyActivatedToggle != null)
                        rg.PreviouslyActivatedToggle.SetToggle(true);
            }
        }
    }
}
