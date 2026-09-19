namespace Submodules.Utility.UI
{
    /// <summary><see cref="ExclusiveGroup{TMember}"/> over content rather than input: a
    /// collection of mutually exclusive <see cref="SimplePanel"/>s, of which at most one is up
    /// at a time. Membership is <see cref="SimplePanel.PanelGroup"/>, and switching a member is
    /// <see cref="SimplePanel.Appear"/> / <see cref="SimplePanel.Disappear"/> — the primitives,
    /// not the group-aware <see cref="SimplePanel.FadeIn"/> / <see cref="SimplePanel.FadeOut"/>
    /// that would loop back here.
    /// </summary>
    public sealed class PanelGroup : ExclusiveGroup<SimplePanel>
    {
        protected override bool IsMember(SimplePanel panel) => panel.PanelGroup == this;

        protected override void SetMemberActive(SimplePanel panel, bool active)
        {
            if (active)
                panel.Appear();
            else
                panel.Disappear();
        }
    }
}
