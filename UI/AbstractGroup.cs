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
    /// allowed depends on who asks (<see cref="GroupCanBeCleared"/>): the <b>user</b> (a click or hotkey
    /// on the active member) needs <see cref="UserCanClear"/>, off by default - radio
    /// behaviour; the <b>group</b> (<see cref="Deactivate"/> with <c>byUser: false</c>, or state derived from elsewhere,
    /// such as a context closing) needs <see cref="SystemCanClear"/>, on by default, and is
    /// also allowed wherever the user is.
    ///
    /// <para>A group that can never be empty (neither flag set) also goes home: the first member
    /// to become active is remembered as <see cref="FirstMember"/>, and deactivating it from the
    /// group's side (state derived from elsewhere) switches back to it instead of
    /// emptying it.</para>
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
        /// group returns to (<see cref="Deactivate"/>) instead of being cleared.</summary>
        public TMember FirstMember { get; private set; }

        [field: SerializeField, FormerlySerializedAs("<IsClearable>k__BackingField")]
        [field: Tooltip("The user may switch the active member off by clicking it, leaving the group " +
                        "with nothing active. Off keeps the radio-button rule: one member always stays on.")]
        public bool UserCanClear { get; private set; }

        [field: SerializeField, FormerlySerializedAs("<IsClearableByGroup>k__BackingField")]
        [field: Tooltip("The group itself may switch the active member off (Deactivate by the group, or state " +
                        "derived from elsewhere such as a closing context) even where the user may not.")]
        public bool SystemCanClear { get; private set; } = true;

        /// <summary>The one statement of whether the active member may be deactivated with no
        /// replacement - asked by <see cref="Deactivate"/> and by <c>SimplePanel.Collapse</c>
        /// alike, so a panel and a toggle in the same group cannot disagree. Where the user may
        /// untoggle the group may too.</summary>
        public bool GroupCanBeCleared(bool byUser) => byUser ? UserCanClear : SystemCanClear || UserCanClear;

        /// <summary>A group that can never be empty, neither by the user nor by itself: resetting it
        /// means going back to <see cref="FirstMember"/>.</summary>
        private bool IsRequired => !GroupCanBeCleared(byUser: false);

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
            if (IsRequired && !Exists(FirstMember) && Exists(ActiveMember))
                FirstMember = ActiveMember;
        }

        internal void Activate(TMember member)
        {
            if (!Exists(member) || !IsMember(member))
                return;

            if (IsRequired && !Exists(FirstMember))
                FirstMember = member;

            SwitchTo(member);
        }

        /// <param name="byUser">Whether the un-toggle is the user's own (a click, a hotkey, a
        /// panel's Collapse) rather than the group's (a closing panel, a derived-state
        /// sync, a member leaving). Defaults to the strict, user side. The group's own un-toggle of
        /// a group that can never be empty returns to <see cref="FirstMember"/>; the user's own is
        /// refused, so clicking the active tab never switches to another.</param>
        internal void Deactivate(TMember member, bool byUser = true)
        {
            if (!Exists(member) || !Same(ActiveMember, member))
                return;

            if (!byUser && IsRequired)
            {
                if (Exists(FirstMember))
                    SwitchTo(FirstMember);

                return;
            }

            if (!GroupCanBeCleared(byUser))
            {
                Debug.Log("SetToggle(false) prevented. To allow un-toggle, enable 'UserCanClear' in the " +
                          $"RadioGroup, or re-parent {member.name} out of any RadioGroup.", this);
                return;
            }

            SwitchTo(null);
        }

        private void SwitchTo(TMember member)
        {
            if (Same(ActiveMember, member))
                return;

            PreviousMember = ActiveMember;
            ActiveMember = member;

            if (Exists(PreviousMember))
                SetMemberActive(PreviousMember, false);

            if (Exists(ActiveMember))
                SetMemberActive(ActiveMember, true);
        }
    }
}
