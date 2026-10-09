using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.TestSupport
{
    /// <summary>A <see cref="ValueSlider"/> that maps 0..1 onto 0..10, the way a game slider (sim speed)
    /// overrides <c>Map</c>.
    /// Lives in the runtime test-support assembly so Unity will attach it.</summary>
    internal sealed class SpyMappedValueSlider : ValueSlider
    {
        protected override float Map(float value) => value * 10f;
    }
}
