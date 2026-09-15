using Submodules.Utility.Extensions;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public sealed class TestButton : AbstractButton
    {
        protected override void OnClick() => Debug.Log($"BUTTON:\t{name.ColoredComponent()} was {"clicked".Colored(ColorExtensions.Orange)}", this);
    }
}
