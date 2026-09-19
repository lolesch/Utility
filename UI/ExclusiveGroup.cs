using System;
using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>The exclusivity rule shared by <see cref="RadioGroup"/>
    /// (input) and <see cref="PanelGroup"/> (content): a collection of mutually exclusive
    /// <typeparamref name="TMember"/>s of which at most one is active at a time.
    /// <see cref="Activate"/> deactivates whichever sibling held the slot. Deactivating the
    /// active member with no replacement is governed by two mutually exclusive settings,
    /// checked in this order: <see cref="IsClearable"/> lets the group end up with nothing
    /// active; otherwise <see cref="IsRestorable"/> falls back to <see cref="PreviousMember"/>
    /// instead. With both off (the default), deactivating the active member is prevented.
    ///
    /// A subclass supplies only the two things that differ per member kind: what counts as
    /// membership (<see cref="IsMember"/>) and how a member is switched on or off
    /// (<see cref="SetMemberActive"/>).
    /// </summary>
    public abstract class ExclusiveGroup<TMember> : MonoBehaviour where TMember : Component
    {
        [field: SerializeField, ReadOnly] public TMember ActiveMember { get; private set; }
        [field: SerializeField, ReadOnly] public TMember PreviousMember { get; private set; }
        [field: SerializeField] public bool IsClearable { get; private set; }

        [field: SerializeField, HideIf(nameof(IsClearable)), Tooltip(
            "Deactivating the active member with no replacement falls back to whichever one " +
            "was active before it, instead of being prevented. Only consulted while the " +
            "group is not Clearable.")]
        public bool IsRestorable { get; private set; }

        public event Action<TMember> OnGroupChanged;

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

        public void ClearActive() => Deactivate(ActiveMember);

        internal void Activate(TMember member)
        {
            if (!Exists(member) || !IsMember(member) || Same(ActiveMember, member))
                return;

            PreviousMember = ActiveMember;
            ActiveMember = member;

            if (Exists(PreviousMember))
                SetMemberActive(PreviousMember, false);

            SetMemberActive(member, true);

            OnGroupChanged?.Invoke(ActiveMember);
        }

        internal void Deactivate(TMember member)
        {
            if (!Exists(member) || !IsMember(member) || !Same(ActiveMember, member))
                return;

            if (!IsClearable)
            {
                // Restoring re-activates PreviousMember directly rather than routing through
                // Activate(), which would try to switch `member` off a second time — it is
                // already going off, mid-way through the call that got us here.
                if (!IsRestorable || !Exists(PreviousMember) || Same(PreviousMember, member))
                    return;

                var restored = PreviousMember;
                PreviousMember = member;
                ActiveMember = restored;

                SetMemberActive(member, false);
                SetMemberActive(restored, true);

                OnGroupChanged?.Invoke(restored);

                return;
            }

            PreviousMember = member;
            ActiveMember = null;
            SetMemberActive(member, false);

            OnGroupChanged?.Invoke(null);
        }
    }
}
