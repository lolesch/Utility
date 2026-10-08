using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary><see cref="AbstractGroup{TMember}"/> over input: a collection of mutually
    /// exclusive <see cref="AbstractToggle"/>s, of which at most one is on at a time. Membership
    /// is <see cref="AbstractToggle.RadioGroup"/>, and switching a member is
    /// <see cref="AbstractToggle.ToggleState"/> — the state-change primitive, not the
    /// group-aware <see cref="AbstractToggle.SetToggle"/> that would loop back here.
    ///
    /// <para>With <see cref="ResetWithParentPanel"/> on, the group resets itself once the
    /// <see cref="SimplePanel"/> it lives in has finished closing - back to its first tab where it
    /// can never be empty, emptied where it may be - so a selection never outlives the panel that
    /// showed it, without the panel knowing its members exist.</para>
    /// </summary>
    public sealed class ToggleGroup : AbstractGroup<AbstractToggle>
    {
        /// <summary>The panel this group lives in, found once on the way up like a toggle finds its
        /// group. Only looked for where <see cref="ResetWithParentPanel"/> is on.</summary>
        private SimplePanel parentPanel;

        [field: SerializeField]
        [field: Tooltip("Reset the group once the panel it lives in has finished closing, so a selection " +
                        "never outlives the panel that showed it: back to the first member on a group that " +
                        "can never be empty (neither 'UserCanUntoggle' nor 'GroupCanUntoggle'), emptied " +
                        "otherwise.")]
        public bool ResetWithParentPanel { get; private set; }

        protected override void Awake()
        {
            if (ResetWithParentPanel)
                parentPanel = GetComponentInParent<SimplePanel>(true);

            base.Awake();
        }

        /// <summary>Unity's message for a change on a <c>CanvasGroup</c> above this object - alpha,
        /// interactable or blocksRaycasts alike - so it also fires on every frame of a fade. The
        /// panel's <see cref="SimplePanel.IsCollapsed"/> is what says the fade-out has finished,
        /// with nothing left to see swap.</summary>
        private void OnCanvasGroupChanged()
        {
            if (parentPanel)
                ResetWhenCollapsed(parentPanel);
        }

        /// <summary>The reset <see cref="ResetWithParentPanel"/> asks for, once <paramref name="parent"/>
        /// has finished closing: back to <see cref="AbstractGroup{TMember}.FirstMember"/> on a group that
        /// can never be empty (even with nothing active - <see cref="AbstractGroup{TMember}.ResetGroup"/>
        /// would find no member to deactivate and stop), emptied otherwise. Split out of
        /// <see cref="OnCanvasGroupChanged"/> so it can be driven without the Unity message or an
        /// <c>Awake</c>.</summary>
        internal void ResetWhenCollapsed(SimplePanel parent)
        {
            if (!parent.IsCollapsed)
                return;

            if (ReturnsToFirst)
                ResetToFirst();
            else
                ResetGroup();
        }

        protected override bool IsMember(AbstractToggle toggle) => toggle.RadioGroup == this;

        protected override void SetMemberActive(AbstractToggle toggle, bool active) =>
            toggle.ToggleState(active);
    }
}
