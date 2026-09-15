using Submodules.Utility.Extensions;
using UnityEngine;

namespace Submodules.Utility.UI.InteractiveElements
{
    public class TestToggle : AbstractToggle
    {
        protected override void ToggleSideEffects() => Debug.Log($"TOGGLE:\t{name.ColoredComponent()} was toggled {(IsOn ? "on" : "off").Colored(ColorExtensions.Orange)}", this);
    }
}
