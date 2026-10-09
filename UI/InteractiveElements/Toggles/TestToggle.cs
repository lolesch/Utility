using Submodules.Utility.Extensions;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public sealed class TestToggle : AbstractToggle
    {
        protected override void OnToggle()
        {
            if(!Debug.isDebugBuild)
                return;
            Debug.Log(
                $"TOGGLE:\t{name.ColoredComponent()} was toggled {(IsOn ? "on" : "off").Colored(ColorExtensions.Orange)}",
                this);
        }
    }
}
