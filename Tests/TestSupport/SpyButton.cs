using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.TestSupport
{
    /// <summary>The smallest real <see cref="AbstractButton"/>: it counts what its one extension point,
    /// <c>OnClick</c>, was told. Lives in a runtime assembly for the reason <see cref="SpyToggle"/> does.</summary>
    internal sealed class SpyButton : AbstractButton
    {
        public int Clicks { get; private set; }

        protected override void OnClick() => Clicks++;
    }
}
