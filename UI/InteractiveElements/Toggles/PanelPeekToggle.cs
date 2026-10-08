using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A tab that owns one bool and two panels - <see cref="panelWhenOff"/> while it is off,
    /// <see cref="panelWhenOn"/> while it is on - and can be <b>peeked past</b>: while
    /// <see cref="PeekKeyPressed"/> the pair shows its other tab, and letting go brings it back.
    ///
    /// <para><b>The pair is scene layout.</b> This toggle and <see cref="companionToggle"/> sit in one
    /// <see cref="ToggleGroup"/> that can never be empty, and their panels in a <see cref="PanelGroup"/>
    /// of their own. The group is the one mirror of the bool and does all the switching; the peek
    /// only ever calls <see cref="AbstractToggle.SetToggle"/> on a member of it, so a click on either
    /// button, the group's reset on its panel closing and the peek all go the same road.</para>
    ///
    /// <para><b>One restore rule.</b> A press remembers whether this toggle was on. On release the peek is
    /// given back only if that has changed, that is, the pair is still where the peek left it. A click on
    /// either tab during the hold has already put the pair where the player wants it, and is left there. A
    /// click on the tab being peeked at is refused (it is on), so release brings the player home.</para>
    ///
    /// <para><b>A pair, and only a pair.</b> This toggle and its companion are the whole group. Nothing checks
    /// that: with a third member, a click on it during a peek is undone on release, because this toggle is
    /// then off where the press found it on.</para>
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
        [SerializeField] private SimplePanel panelWhenOn;
        [SerializeField] private SimplePanel panelWhenOff;

        [SerializeField, Tooltip("The other tab button of the pair, in the same ToggleGroup.")]
        private AbstractToggle companionToggle;

        /// <summary>Whether the peek key is held down right now.</summary>
        protected abstract bool PeekKeyPressed { get; }

        /// <summary>Whether the last tick read the key as held and reachable. A peek begins and ends on the
        /// edges of this, so a begin that was refused is not retried for the rest of the hold.</summary>
        private bool wasPressed;

        /// <summary>Whether this toggle was on when the current peek began; null when not peeking.</summary>
        private bool? snapshot;

        private void Update()
        {
            if (Application.isPlaying)
                EvaluateKey();
        }

        /// <summary>A <c>CanvasGroup</c> above changed: a closing panel takes this toggle out of reach here,
        /// before its group can reset on the panel having closed, so the peek is given back first and the
        /// reset has the last word.</summary>
        protected override void OnCanvasGroupChanged()
        {
            base.OnCanvasGroupChanged();

            if (Application.isPlaying)
                EvaluateKey();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (wasPressed)
                Cancel();
            
            wasPressed = false;
        }

        /// <summary>Evaluates the peek for this frame: begins it, ends it or leaves it be.</summary>
        internal void EvaluateKey()
        {
            var pressed = PeekKeyPressed && IsInteractable();
            if (pressed == wasPressed)
                return;

            wasPressed = pressed;

            if (pressed) 
                SwitchPanels();
            else
                Cancel();
        }

        /// <summary>Gives the peek back, unless the player has already moved the pair themselves.</summary>
        private void Cancel()
        {
            if (snapshot.HasValue && snapshot != IsOn)
                SwitchPanels();
            snapshot = null;
        }

        /// <summary>Shows the other tab of the pair: the companion when this toggle is on, otherwise this toggle.
        /// With no companion to switch to it flips itself, which the group refuses unless it allows switch-off.</summary>
        private void SwitchPanels()
        {
            snapshot = IsOn;

            if (IsOn && HasCompanion)
                companionToggle.SetToggle(true);
            else
                SetToggle(!IsOn);
        }

        private bool HasCompanion => companionToggle && companionToggle != this && RadioGroup && companionToggle.RadioGroup == RadioGroup;

        protected override void OnToggle()
        {
            var shown = IsOn ? panelWhenOn : panelWhenOff;
            var hidden = IsOn ? panelWhenOff : panelWhenOn;

            // Each slot is optional on its own. The incoming panel first: a PanelGroup hides its sibling
            // itself, and refuses to collapse its sole active panel. Collapsing a sibling it already hid would
            // restart its fade-out.
            if (shown)
                shown.ToggleState(true);

            var sharesGroup = shown && hidden && shown.RadioGroup && shown.RadioGroup == hidden.RadioGroup;
            if (hidden && !sharesGroup)
                hidden.ToggleState(false);
        }
    }
}