using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// The <b>driver</b> of a two-panel switch: one toggle owns a single bool, and exactly two panels
    /// follow it - <see cref="panelWhenOff"/> while the toggle is off, <see cref="panelWhenOn"/> while
    /// it is on. The panels follow the bool, never the other way round, so neither both showing nor
    /// neither showing can be authored.
    ///
    /// <para>The driver is a member of a <see cref="ToggleGroup"/> that can never be empty, and the
    /// group does the exclusion: the driver switching on switches off whichever member was on, and any
    /// other member switching on switches the driver off. The off-state button the player sees is a
    /// <see cref="TwoPanelMirrorToggle"/> beside it, authored on as the group's
    /// <see cref="AbstractGroup{TMember}.FirstMember"/>, so a click on either button flips both. A click
    /// on the driver while it is on is refused (<see cref="AbstractToggle.SetToggle"/>), and the group's
    /// reset on its panel closing goes home to its first member, which switches the driver off through
    /// the same <see cref="AbstractToggle.ToggleState"/> a click does. The group is the one mirror of
    /// the bool; no second piece of state is kept.</para>
    ///
    /// <para>Other toggles may share the group; each one switching on switches the driver off. Their
    /// panels belong elsewhere than beside <see cref="panelWhenOff"/> in a <see cref="PanelGroup"/>,
    /// or the off panel would clash with them: that is scene layout, not something this component
    /// can check.</para>
    ///
    /// <para>Every write to the bool is counted (<see cref="Writes"/>), so a caller that flipped it
    /// for a while can tell, on giving it back, whether anything else wrote it meanwhile.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TwoPanelToggle : AbstractToggle, ITwoPanelDriver
    {
        [SerializeField] private SimplePanel panelWhenOff;
        [SerializeField] private SimplePanel panelWhenOn;

        /// <summary>How often the bool was written, whether it changed or not: a click, the group's
        /// reset and any other driver of it all pass through <see cref="OnToggle"/>. Compare two
        /// reads rather than trusting the absolute number - the toggle writes its authored state once
        /// on <c>Start</c>.</summary>
        public int Writes { get; private set; }

        protected override void OnToggle()
        {
            Writes++;

            var shown = IsOn ? panelWhenOn : panelWhenOff;
            var hidden = IsOn ? panelWhenOff : panelWhenOn;

            // The incoming panel first: a PanelGroup hides its sibling itself, and refuses to collapse
            // the sole active panel of a group that can never be empty. A sibling it has already hidden
            // is left alone: collapsing it again would restart its fade-out.
            if (shown)
                shown.ToggleState(true);

            if (hidden && !SharesPanelGroup(shown, hidden))
                hidden.ToggleState(false);
        }

        /// <summary>The group-side write a <see cref="TwoPanelPeek"/> makes. Switching the driver off from
        /// the group's side normally goes home to the group's first member; when that is the driver
        /// itself there is nowhere to go and the write would do nothing, so the pair is switched off by
        /// switching its <see cref="TwoPanelMirrorToggle"/> on instead, which the group answers by
        /// switching this toggle off.</summary>
        void ITwoPanelDriver.SetFromGroup(bool on)
        {
            if (!on && IsOn && RadioGroup && RadioGroup.FirstMember == this)
            {
                var mirror = MirrorInGroup();

                if (mirror)
                {
                    mirror.SyncToggle(true);
                    return;
                }
            }

            SyncToggle(on);
        }

        private TwoPanelMirrorToggle MirrorInGroup() =>
            RadioGroup.GetComponentsInChildren<TwoPanelMirrorToggle>(true)
                .FirstOrDefault(mirror => mirror.Driver == this && mirror.RadioGroup == RadioGroup);

        private static bool SharesPanelGroup(SimplePanel a, SimplePanel b) =>
            a && b && a.RadioGroup && a.RadioGroup == b.RadioGroup;

        /// <summary>What is wrong with how this driver is authored, one sentence each; empty when it is
        /// wired as a two-panel switch has to be.</summary>
        internal IEnumerable<string> AuthoringProblems()
        {
            if (!panelWhenOff || !panelWhenOn)
                yield return "a panel is unset: a two-panel switch needs both 'panelWhenOff' and 'panelWhenOn'.";
            else if (panelWhenOff == panelWhenOn)
                yield return "'panelWhenOff' and 'panelWhenOn' are the same panel, which would be shown and " +
                             "hidden by every write. A two-panel switch needs two.";

            if (!RadioGroup)
            {
                yield return "it has no ToggleGroup on its parent, so nothing switches it off when the " +
                             "other tab button is clicked. Put it and its TwoPanelMirrorToggle under a ToggleGroup.";
                yield break;
            }

            if (RadioGroup.CanUntoggle(byUser: false))
                yield return $"its ToggleGroup '{RadioGroup.name}' allows switch-off, so both tab buttons can " +
                             "be off and the group cannot go home. Disable 'UserCanUntoggle' and " +
                             "'GroupCanUntoggle' on the group.";

            var first = RadioGroup.FirstMember ? RadioGroup.FirstMember : RadioGroup.ActiveMember;

            if (!first || first == this)
                yield return $"the first member of its ToggleGroup '{RadioGroup.name}' is not another toggle, " +
                             "so a reset would not return the pair to the panel shown while off. " +
                             "Author the TwoPanelMirrorToggle on, not this toggle.";
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            foreach (var problem in AuthoringProblems())
                Debug.LogWarning($"{name}: {problem}", this);
        }
#endif // UNITY_EDITOR
    }
}
