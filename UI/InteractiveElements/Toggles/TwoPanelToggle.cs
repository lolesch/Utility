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
    /// <para>The pair of tab buttons the player sees is two toggles in one <see cref="ToggleGroup"/>
    /// that can never be empty: this driver and an inert partner (a toggle with no panel), authored
    /// on, as the group's <see cref="AbstractGroup{TMember}.FirstMember"/>. A click on either button
    /// flips both, because the group deactivates whichever member was on. A click on the driver
    /// while it is on is refused (<see cref="AbstractToggle.SetToggle"/>), and the group's reset
    /// on its panel closing goes home to the partner, which switches the driver off through the
    /// same <see cref="AbstractToggle.ToggleState"/> a click does. The group is the one mirror of
    /// the bool; no second piece of state is kept.</para>
    ///
    /// <para>Every write to the bool is counted (<see cref="Writes"/>), so a caller that flipped it
    /// for a while can tell, on giving it back, whether anything else wrote it meanwhile.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TwoPanelToggle : AbstractToggle
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
            // the sole active panel of a group that can never be empty.
            if (shown)
                shown.ToggleState(true);

            if (hidden)
                hidden.ToggleState(false);
        }

        /// <summary>What is wrong with how this pair is authored, one sentence each; empty when it is
        /// wired as a two-panel switch has to be.</summary>
        internal IEnumerable<string> AuthoringProblems()
        {
            if (!panelWhenOff || !panelWhenOn)
                yield return "a panel is unset: a two-panel switch needs both 'panelWhenOff' and 'panelWhenOn'.";

            if (!RadioGroup)
            {
                yield return "it has no ToggleGroup on its parent, so nothing mirrors its bool to the other " +
                             "tab button. Put it and its inert partner under a ToggleGroup.";
                yield break;
            }

            var members = RadioGroup.GetComponentsInChildren<AbstractToggle>(true).Count(t => t.RadioGroup == RadioGroup);

            if (members != 2)
                yield return $"its ToggleGroup '{RadioGroup.name}' has {members} toggles; a two-panel switch is " +
                             "exactly the driver and one inert partner.";

            if (RadioGroup.CanUntoggle(byUser: false))
                yield return $"its ToggleGroup '{RadioGroup.name}' allows switch-off, so both tab buttons can " +
                             "be off and the group cannot go home. Disable 'UserCanUntoggle' and " +
                             "'GroupCanUntoggle' on the group.";

            var first = RadioGroup.FirstMember ? RadioGroup.FirstMember : RadioGroup.ActiveMember;

            if (!first || first == this)
                yield return $"the first member of its ToggleGroup '{RadioGroup.name}' is not the off-state " +
                             "toggle, so a reset would not return the pair to the panel shown while off. " +
                             "Author the inert partner on, not this toggle.";
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
