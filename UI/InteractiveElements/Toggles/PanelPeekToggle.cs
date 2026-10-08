using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A tab that owns one bool and two panels - <see cref="panelWhenOff"/> while it is off,
    /// <see cref="panelWhenOn"/> while it is on - and can be <b>peeked past</b>: while
    /// <see cref="PeekKeyHeld"/> the pair shows its other tab, and letting go brings it back.
    ///
    /// <para><b>The pair is scene layout.</b> This toggle and <see cref="peekTarget"/> sit in one
    /// <see cref="ToggleGroup"/> that can never be empty, and their panels in a <see cref="PanelGroup"/>
    /// of their own. The group is the one mirror of the bool and does all the switching; the peek
    /// only ever calls <see cref="AbstractToggle.SetToggle"/> on a member of it, so a click on either
    /// button, the group's reset on its panel closing and the peek all go the same road.</para>
    ///
    /// <para><b>One restore rule.</b> A peek remembers the member it left (<c>home</c>) and the one it
    /// switched on (<c>away</c>). On release it switches home back on only if away is still on - a click on
    /// the home tab, or on a third toggle, has moved the group on, and that is left where the player put
    /// it. A click on the tab being peeked at is refused (it is on), so release brings the player home.</para>
    ///
    /// <para><b>Held, not pressed.</b> The key is read every frame as "held and reachable", and a peek begins
    /// and ends on the edges of that, once per hold. Reachable is <see cref="Selectable.IsInteractable"/>,
    /// which a closed or hidden <see cref="SimplePanel"/> takes away through its <c>CanvasGroup</c>: Alt
    /// moves no tab nobody can see, a hold begun before the panel opened peeks on open, and a panel
    /// closing during a hold gives the peek back at once. Alt+Tab needs no case: the next frame
    /// back reads the key as up.</para>
    ///
    /// <para>The key is the subclass's to name, so this stays generic UI and the game decides what
    /// "peek" is bound to.</para>
    /// </summary>
    public abstract class PanelPeekToggle : AbstractToggle
    {
        [Space]
        [SerializeField] private SimplePanel panelWhenOff;
        [SerializeField] private SimplePanel panelWhenOn;

        [SerializeField, Tooltip("The other tab button of the pair, in the same ToggleGroup. Switched on to " +
                                 "peek while this toggle is on. Unset, a peek from the on state is not possible.")]
        private AbstractToggle peekTarget;

        /// <summary>Whether the peek key is held down right now.</summary>
        protected abstract bool PeekKeyHeld { get; }

        /// <summary>The member the running peek left, and the one it switched on. Null when not peeking.</summary>
        private AbstractToggle home;
        private AbstractToggle away;

        /// <summary>Whether the last tick read the key as held and reachable. A peek begins and ends on the
        /// edges of this, so a begin that was refused is not retried for the rest of the hold.</summary>
        private bool wasHeld;

        private void Update()
        {
            if (Application.isPlaying)
                Tick();
        }

        /// <summary>A <c>CanvasGroup</c> above changed: a closing panel takes this toggle out of reach here,
        /// before its group can reset on the panel having closed, so the peek is given back first and the
        /// reset has the last word.</summary>
        protected override void OnCanvasGroupChanged()
        {
            base.OnCanvasGroupChanged();

            if (Application.isPlaying)
                Tick();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            End();
            wasHeld = false;
        }

        /// <summary>Evaluates the peek for this frame: begins it, ends it or leaves it be.</summary>
        internal void Tick()
        {
            var held = PeekKeyHeld && IsInteractable();

            if (held == wasHeld)
                return;

            wasHeld = held;

            if (held)
                Begin();
            else
                End();
        }

        private void Begin()
        {
            if (!RadioGroup)
                return;

            if (IsOn)
            {
                if (!PeekTargetIsInGroup)
                    return;

                (home, away) = (this, peekTarget);
            }
            else
            {
                // Whoever the group is on - the other tab, or a third toggle sharing the group.
                if (!RadioGroup.ActiveMember)
                    return;

                (home, away) = (RadioGroup.ActiveMember, this);
            }

            away.SetToggle(true);
        }

        private void End()
        {
            if (home == null)
                return;

            var back = home;
            var peeked = away;
            home = away = null;

            if (back && peeked && peeked.IsOn)
                back.SetToggle(true);
        }

        private bool PeekTargetIsInGroup => peekTarget && peekTarget != this && peekTarget.RadioGroup == RadioGroup;

        protected override void OnToggle()
        {
            var shown = IsOn ? panelWhenOn : panelWhenOff;
            var hidden = IsOn ? panelWhenOff : panelWhenOn;

            // The incoming panel first: a PanelGroup hides its sibling itself, and refuses to collapse the
            // sole active panel of a group that can never be empty. A sibling it has already hidden is left
            // alone: collapsing it again would restart its fade-out.
            if (shown)
                shown.ToggleState(true);

            if (hidden && !SharesPanelGroup(shown, hidden))
                hidden.ToggleState(false);
        }

        private static bool SharesPanelGroup(SimplePanel a, SimplePanel b) =>
            a && b && a.RadioGroup && a.RadioGroup == b.RadioGroup;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            foreach (var problem in AuthoringProblems())
                Debug.LogWarning($"{name}: {problem}", this);
        }
#endif

        /// <summary>What is wrong with how this pair is authored, one line each; empty when nothing is.</summary>
        internal IEnumerable<string> AuthoringProblems()
        {
            if (!panelWhenOff || !panelWhenOn)
                yield return "a panel is unset: a peek toggle needs both 'panelWhenOff' and 'panelWhenOn'.";
            else if (panelWhenOff == panelWhenOn)
                yield return "'panelWhenOff' and 'panelWhenOn' are the same panel, which would be shown and " +
                             "hidden by the same switch.";

            if (!RadioGroup)
            {
                yield return "it has no ToggleGroup on its parent: the group is what keeps the pair exclusive.";
                yield break;
            }

            if (RadioGroup.CanUntoggle(byUser: false))
                yield return $"its ToggleGroup '{RadioGroup.name}' allows switch-off, so both tab buttons can be " +
                             "off at once: turn off 'UserCanUntoggle' and 'GroupCanUntoggle'.";

            if (!peekTarget)
                yield return "'peekTarget' is unset: a peek from the on state has no tab to switch on.";
            else if (!PeekTargetIsInGroup)
                yield return $"'peekTarget' {peekTarget.name} is not in the same ToggleGroup, so switching it on " +
                             "would not switch this toggle off.";
        }
    }
}
