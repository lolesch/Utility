using System.Collections.Generic;
using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.TestSupport
{
    /// <summary>
    /// The smallest real <see cref="AbstractToggle"/>: it records what its one extension
    /// point — <c>OnToggle</c> — was told, and does nothing else.
    ///
    /// It lives in a runtime assembly rather than beside the tests because Unity refuses to
    /// attach a MonoBehaviour that was compiled into an Editor-only assembly ("Can't add
    /// script behaviour ... because it is an editor script"). The
    /// <c>UNITY_INCLUDE_TESTS</c> define constraint on the asmdef keeps it out of player
    /// builds all the same.
    ///
    /// Tests assert on <em>deltas</em> of <see cref="OnToggleCalls"/> rather than absolutes:
    /// <c>Selectable</c> is <c>[ExecuteAlways]</c>, so the editor may tick <c>Start</c>
    /// (which calls <c>SetToggle(IsOn)</c>) at a moment no test controls.
    /// </summary>
    public sealed class SpyToggle : AbstractToggle
    {
        public int OnToggleCalls { get; private set; }

        /// <summary>The value of <see cref="AbstractToggle.IsOn"/> at each call, in order.</summary>
        public List<bool> ObservedStates { get; } = new();

        protected override void OnToggle()
        {
            OnToggleCalls++;
            ObservedStates.Add(IsOn);
        }
    }
}
