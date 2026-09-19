namespace Submodules.Utility.UI
{
    /// <summary><see cref="ExclusiveGroup{TMember}"/> over input: a collection of mutually
    /// exclusive <see cref="AbstractToggle"/>s, of which at most one is on at a time. Membership
    /// is <see cref="AbstractToggle.RadioGroup"/>, and switching a member is
    /// <see cref="AbstractToggle.ToggleState"/> — the state-change primitive, not the
    /// group-aware <see cref="AbstractToggle.SetToggle"/> that would loop back here.
    /// </summary>
    public sealed class RadioGroup : ExclusiveGroup<AbstractToggle>
    {
        protected override bool IsMember(AbstractToggle toggle) => toggle.RadioGroup == this;

        protected override void SetMemberActive(AbstractToggle toggle, bool active) =>
            toggle.ToggleState(active);
    }
}
