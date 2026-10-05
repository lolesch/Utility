using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Submodules.Utility.UI
{
    /// <summary>The exclusivity rule shared by <see cref="ToggleGroup"/>
    /// (input) and <see cref="PanelGroup"/> (content): a collection of mutually exclusive
    /// <typeparamref name="TMember"/>s of which at most one is active at a time.
    /// <see cref="Activate"/> deactivates whichever sibling held the slot. Deactivating the
    /// active member with no replacement leaves the group with nothing active. Whether that is
    /// allowed depends on who asks (<see cref="CanUntoggle"/>): the <b>user</b> (a click or hotkey
    /// on the active member) needs <see cref="UserCanUntoggle"/>, off by default - radio
    /// behaviour; the <b>group</b> (<see cref="ResetGroup"/>, or state derived from elsewhere,
    /// such as a context closing) needs <see cref="GroupCanUntoggle"/>, on by default, and is
    /// also allowed wherever the user is.
    ///
    /// <para>A group that can never be empty (neither flag set) also goes home: the first member
    /// to become active is remembered as <see cref="FirstMember"/>, and resetting the group
    /// (<see cref="ResetGroup"/>, state derived from elsewhere) switches back to it instead of
    /// emptying it. With <see cref="ResetWithParentPanel"/> on, the group resets itself the same way once
    /// the <see cref="SimplePanel"/> it lives in has finished closing - back to its first tab where it
    /// can never be empty, emptied where it may be - so a selection never outlives the panel that
    /// showed it, without the panel knowing its members exist.</para>
    ///
    /// A subclass supplies only the two things that differ per member kind: what counts as
    /// membership (<see cref="IsMember"/>) and how a member is switched on or off
    /// (<see cref="SetMemberActive"/>).
    /// </summary>
    public abstract class AbstractGroup<TMember> : MonoBehaviour where TMember : Component
    {
        [field: SerializeField, ReadOnly] public TMember ActiveMember { get; private set; }
        [field: SerializeField, ReadOnly] internal TMember PreviousMember { get; private set; }

        /// <summary>The first member that became active - the authored selection, or the first one
        /// activated at runtime. Only a group that can never be empty keeps one: it is what that
        /// group returns to (<see cref="ResetToFirst"/>) instead of being cleared.</summary>
        public TMember FirstMember { get; private set; }

        /// <summary>The panel this group lives in, found once on the way up like a toggle finds its
        /// group. Only looked for where <see cref="ResetWithParentPanel"/> is on.</summary>
        private SimplePanel parentPanel;

        [field: SerializeField, FormerlySerializedAs("<IsClearable>k__BackingField")]
        [field: Tooltip("The user may switch the active member off by clicking it, leaving the group " +
                        "with nothing active. Off keeps the radio-button rule: one member always stays on.")]
        public bool UserCanUntoggle { get; private set; }

        [field: SerializeField, FormerlySerializedAs("<IsClearableByGroup>k__BackingField")]
        [field: Tooltip("The group itself may switch the active member off (ResetGroup, or state " +
                        "derived from elsewhere such as a closing context) even where the user may not.")]
        public bool GroupCanUntoggle { get; private set; } = true;

        [field: SerializeField]
        [field: Tooltip("Reset the group once the panel it lives in has finished closing, so a selection " +
                        "never outlives the panel that showed it: back to the first member on a group that " +
                        "can never be empty (neither 'UserCanUntoggle' nor 'GroupCanUntoggle'), emptied " +
                        "otherwise.")]
        public bool ResetWithParentPanel { get; private set; }

        /// <summary>The one statement of whether the active member may be deactivated with no
        /// replacement - asked by <see cref="Deactivate"/> and by <c>SimplePanel.Collapse</c>
        /// alike, so a panel and a toggle in the same group cannot disagree. Where the user may
        /// untoggle the group may too.</summary>
        public bool CanUntoggle(bool byUser) => byUser ? UserCanUntoggle : GroupCanUntoggle || UserCanUntoggle;

        /// <summary>A group that can never be empty, neither by the user nor by itself: resetting it
        /// means going back to <see cref="FirstMember"/>.</summary>
        private bool ReturnsToFirst => !CanUntoggle(byUser: false);

        /// <summary>Whether <paramref name="member"/> belongs to this group — the back-reference
        /// the member kind keeps to its own group.</summary>
        protected abstract bool IsMember(TMember member);

        /// <summary>Switch a member on or off. The state-change primitive, not the member's own
        /// group-aware entry point: routing back through that would loop into this group again.</summary>
        protected abstract void SetMemberActive(TMember member, bool active);

        // Unity's fake-null and its == overload are not picked up for a type parameter, so both
        // route through the Component the constraint gives us to keep destroyed-object semantics.
        private static bool Exists(TMember member) => (Component)member;
        private static bool Same(TMember a, TMember b) => (Component)a == (Component)b;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (Exists(ActiveMember) && !IsMember(ActiveMember))
                ActiveMember = null;

            if (Exists(PreviousMember) && !IsMember(PreviousMember))
                PreviousMember = null;
        }
#endif

        protected virtual void Awake()
        {
            if (ResetWithParentPanel)
                parentPanel = GetComponentInParent<SimplePanel>(true);

            if (ReturnsToFirst && !Exists(FirstMember) && Exists(ActiveMember))
                FirstMember = ActiveMember;
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
        /// has finished closing: back to <see cref="FirstMember"/> on a group that can never be empty
        /// (even with nothing active - <see cref="ResetGroup"/> would find no member to deactivate and
        /// stop), emptied otherwise. Split out of <see cref="OnCanvasGroupChanged"/> so it can be
        /// driven without the Unity message or an <c>Awake</c>.</summary>
        internal void ResetWhenCollapsed(SimplePanel parent)
        {
            if (!parent.IsCollapsed)
                return;

            if (ReturnsToFirst)
                ResetToFirst();
            else
                ResetGroup();
        }

        /// <summary>Switches back to <see cref="FirstMember"/>. Only a group that can never be empty
        /// has one, so any other group is left as it is: there is no home to return to.</summary>
        internal void ResetToFirst()
        {
            if (!Exists(FirstMember) || Same(ActiveMember, FirstMember))
                return;

            Activate(FirstMember);
        }

        /// <summary>Puts the group back to its resting state from the group's side: empty where it may
        /// be emptied, on <see cref="FirstMember"/> where it can never be empty
        /// (<see cref="Deactivate"/>). The one call for "the state this group is derived from is
        /// gone", whichever kind of group it is.</summary>
        public void ResetGroup() => Deactivate(ActiveMember, byUser: false);

        internal void Activate(TMember member)
        {
            if (!Exists(member) || !IsMember(member) || Same(ActiveMember, member))
                return;

            PreviousMember = ActiveMember;
            ActiveMember = member;

            if (ReturnsToFirst && !Exists(FirstMember))
                FirstMember = member;

            if (Exists(PreviousMember))
                SetMemberActive(PreviousMember, false);

            SetMemberActive(ActiveMember, true);
        }

        /// <param name="byUser">Whether the un-toggle is the user's own (a click, a hotkey, a
        /// panel's Collapse) rather than the group's (<see cref="ResetGroup"/>, a derived-state
        /// sync, a member leaving). Defaults to the strict, user side. The group's own un-toggle of
        /// a group that can never be empty is a <see cref="ResetToFirst"/>; the user's own is
        /// refused, so clicking the active tab never switches to another.</param>
        internal void Deactivate(TMember member, bool byUser = true)
        {
            if (!Exists(member) || !IsMember(member) || !Same(ActiveMember, member))
                return;

            if (!byUser && ReturnsToFirst)
            {
                ResetToFirst();
                return;
            }

            if (!CanUntoggle(byUser))
            {
                Debug.Log("SetToggle(false) prevented. To allow un-toggle, enable 'UserCanUntoggle' in the " +
                          $"RadioGroup, or re-parent {member.name} out of any RadioGroup.", this);
                return;
            }

            PreviousMember = ActiveMember;
            ActiveMember = null;
            SetMemberActive(PreviousMember, false);
        }
    }
}
