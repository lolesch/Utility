using System.Collections.Generic;
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
    /// other member switching on switches the driver off. The two buttons the player sees are the driver
    /// and a <see cref="TwoPanelMirrorToggle"/> beside it, so a click on either button flips both. A
    /// click on the driver while it is on is refused (<see cref="AbstractToggle.SetToggle"/>), and the
    /// group's reset on its panel closing goes home to its
    /// <see cref="AbstractGroup{TMember}.FirstMember"/> through the same
    /// <see cref="AbstractToggle.ToggleState"/> a click does. The group is the one mirror of the bool;
    /// no second piece of state is kept.</para>
    ///
    /// <para>Either button may be home, and the author says which by switching it on in the group. With
    /// the <b>mirror first</b> the pair rests off; with the <b>driver first</b> (the Vendor and the
    /// Healer, where the driver is the Supply tab) it rests on. The driver does nothing differently.</para>
    ///
    /// <para>Other toggles may share the group; each one switching on switches the driver off. Their
    /// panels belong elsewhere than beside <see cref="panelWhenOff"/> in a <see cref="PanelGroup"/>,
    /// or the off panel would clash with them: that is scene layout, not something this component
    /// can check.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TwoPanelToggle : AbstractToggle
    {
        [SerializeField] private SimplePanel panelWhenOff;
        [SerializeField] private SimplePanel panelWhenOn;

        protected override void OnToggle()
        {
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

        private static bool SharesPanelGroup(SimplePanel a, SimplePanel b) =>
            a && b && a.RadioGroup && a.RadioGroup == b.RadioGroup;

        /// <summary>What is wrong with how this driver is authored, one sentence each; empty when it is
        /// wired as a two-panel switch has to be. Whether the driver or its mirror is the group's first
        /// member is the author's choice and is not a problem; a first member must exist.</summary>
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

            if (!(RadioGroup.FirstMember ? RadioGroup.FirstMember : RadioGroup.ActiveMember))
                yield return $"its ToggleGroup '{RadioGroup.name}' has no first member, so a reset has no " +
                             "home to return the pair to. Author one of the group's toggles on.";
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
