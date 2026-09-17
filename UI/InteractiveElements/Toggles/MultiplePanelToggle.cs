using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class MultiplePanelToggle : AbstractToggle
    {
        [SerializeField] private List<SimplePanel> panelsToTurnOn;
        [SerializeField] private List<SimplePanel> panelsToTurnOff;
        [SerializeField] private List<RadioGroup> groupsToTurnOff;

        protected override void OnToggle()
        {
            foreach (var panel in panelsToTurnOn)
            {
                if (panel == null) continue;
                panel.Toggle(IsOn);
            }

            foreach (var panel in panelsToTurnOff)
            {
                if (panel == null) continue;
                panel.Toggle(!IsOn);
            }

            foreach (var rg in groupsToTurnOff)
            {
                if (rg == null) continue;
                if (IsOn && rg.SelectedToggle != null)
                    rg.SelectedToggle.SetToggle(false);
                else if (!IsOn && rg.PreviouslySelectedToggle != null)
                        rg.PreviouslySelectedToggle.SetToggle(true);
            }
        }
    }
}
