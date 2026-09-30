using UnityEngine;

namespace Submodules.Utility.UI
{
    public class PanelButton : AbstractButton
    {
        [SerializeField] protected SimplePanel panel;
        protected override void OnClick() => panel.Expand();
    }
}