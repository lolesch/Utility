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
        private SimplePanel parentPanel;

        [field: SerializeField]
        [field: Tooltip("Reset the group once the panel it lives in has finished closing, so a selection " +
                        "never outlives the panel that showed it: back to the first member on a group that " +
                        "can never be empty (neither 'UserCanClear' nor 'SystemCanClear'), emptied " +
                        "otherwise.")]
        public bool ResetWithParentPanel { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (ResetWithParentPanel)
                parentPanel = GetComponentInParent<SimplePanel>(true);
        }

        /// <summary>Unity calls this on any <c>CanvasGroup</c> change above, so on every frame of a fade;
        /// <see cref="SimplePanel.IsCollapsed"/> says the fade-out is done.</summary>
        private void OnCanvasGroupChanged()
        {
            if (parentPanel) ResetWhenCollapsed(parentPanel);
        }

        internal void ResetWhenCollapsed(SimplePanel parent)
        {
            if (parent.IsCollapsed)
                Deactivate(ActiveMember, byUser: false);
        }

        protected override bool IsMember(AbstractToggle toggle) => toggle.RadioGroup == this;

        protected override void SetMemberActive(AbstractToggle toggle, bool active) =>
            toggle.ToggleState(active);
    }
}
