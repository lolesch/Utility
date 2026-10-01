using System.Collections.Generic;
using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.TestSupport
{
    /// <summary>Non-contiguous on purpose, like <c>ItemRarity</c>: the slider index must not be mistaken for the enum's number.</summary>
    public enum TestRarity
    {
        None = 0,
        Common = 5,
        Magic = 15,
        Rare = 20,
        Unique = 30,
    }

    /// <summary>The smallest concrete <see cref="EnumSlider{TEnum}"/>, narrowed the way a real one
    /// drops a "none" member. Lives in a runtime assembly for the same reason as <see cref="SpyToggle"/>.</summary>
    public sealed class SpyEnumSlider : EnumSlider<TestRarity>
    {
        private static readonly TestRarity[] Offered = { TestRarity.Common, TestRarity.Magic, TestRarity.Rare, TestRarity.Unique };

        protected override IReadOnlyList<TestRarity> Members => Offered;
    }
}
