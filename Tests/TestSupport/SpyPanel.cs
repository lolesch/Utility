using System.Collections.Generic;
using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.TestSupport
{
    /// <summary>
    /// The smallest real <see cref="SimplePanel"/>: it counts <c>FadeIn</c> / <c>FadeOut</c>
    /// calls via the two hooks <see cref="SimplePanel"/> already calls synchronously before
    /// starting its tween, so a caller is observable without the tween pump ticking (which it
    /// does not, in EditMode). Lives beside <see cref="SpyToggle"/> for the same reason: a
    /// MonoBehaviour compiled into an Editor-only assembly can't be attached to a GameObject.
    ///
    /// <para><see cref="Log"/> is the ordering counterpart to the two counters: a handover
    /// between siblings is not just "one went out, one came in" but "the loser went out
    /// <i>first</i>", and only a shared record can tell the two apart. Set it to a list the
    /// test owns, and give the panel a <see cref="Label"/> to keep the entries readable.</para>
    /// </summary>
    public sealed class SpyPanel : SimplePanel
    {
        public int FadeInCalls { get; private set; }
        public int FadeOutCalls { get; private set; }

        /// <summary>How this panel names itself in <see cref="Log"/>. Defaults to the
        /// GameObject's name, which every panel built by the scene helper shares.</summary>
        public string Label { get; set; }

        /// <summary>Optional shared order record: <c>"label+"</c> per appear,
        /// <c>"label-"</c> per disappear, appended in call order.</summary>
        public List<string> Log { get; set; }

        protected override void BeforeAppear()
        {
            base.BeforeAppear();
            
            FadeInCalls++;
            Log?.Add($"{Label ?? name}+");
        }

        protected override void BeforeDisappear()
        {
            FadeOutCalls++;
            Log?.Add($"{Label ?? name}-");
        }
    }
}
