using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.TestSupport
{
    /// <summary>
    /// A <see cref="PanelPeekToggle"/> whose peek key is a field, so a test holds and releases it
    /// without a keyboard. Lives beside <see cref="SpyToggle"/> for the same reason: a MonoBehaviour
    /// compiled into an Editor-only assembly can't be attached to a GameObject.
    /// </summary>
    public sealed class SpyPeekToggle : PanelPeekToggle
    {
        public bool KeyHeld { get; set; }

        protected override bool PeekKeyHeld => KeyHeld;
    }
}
