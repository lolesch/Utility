using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.TestSupport
{
    /// <summary>
    /// The smallest real <see cref="SimplePanel"/>: it counts <c>FadeIn</c> / <c>FadeOut</c>
    /// calls via the two hooks <see cref="SimplePanel"/> already calls synchronously before
    /// starting its tween, so a caller is observable without the tween pump ticking (which it
    /// does not, in EditMode). Lives beside <see cref="SpyToggle"/> for the same reason: a
    /// MonoBehaviour compiled into an Editor-only assembly can't be attached to a GameObject.
    /// </summary>
    public sealed class SpyPanel : SimplePanel
    {
        public int FadeInCalls { get; private set; }
        public int FadeOutCalls { get; private set; }

        protected override void BeforeAppear() => FadeInCalls++;
        protected override void BeforeDisappear() => FadeOutCalls++;
    }
}
