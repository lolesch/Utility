using NaughtyAttributes;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Submodules.Utility.UI
{
    public abstract class AbstractToggle : AbstractButton
    {
        //TODO: implement audio feedback on toggle
        
        [field: SerializeField] public bool IsOn { get; private set; } = false;

        /// <summary>A toggle's <see cref="RadioGroup"/> is automatically assigned if present on the toggle's parent.</summary>
        [field: SerializeField, ReadOnly] public ToggleGroup RadioGroup { get; private set; }

        [SerializeField] private Sprite toggledOffSprite;
        [SerializeField] private Sprite toggledOnSprite;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            var resolved = transform.parent?.GetComponent<ToggleGroup>();

            if (RadioGroup && RadioGroup != resolved)
                RadioGroup.Deactivate(this, byUser: false);

            RadioGroup = resolved;

            if (IsOn && RadioGroup)
                RadioGroup.Activate(this);
        }
#endif //UNITY_EDITOR

        protected override void Awake()
        {
            base.Awake();

            if (!RadioGroup)
                RadioGroup = transform.parent?.GetComponent<ToggleGroup>();
        }

        /// <summary>Bypasses the group-aware <see cref="SetToggle"/> — mirrors
        /// <c>SimplePanel.Start</c> calling its internal primitive directly. A toggle authored
        /// as the group's selection already has <see cref="ToggleGroup.ActiveMember"/> pointing
        /// at it (via <see cref="OnValidate"/>), so routing through <see cref="ToggleGroup.Activate"/>
        /// here would see "no change" and skip this toggle's own visual setup entirely.</summary>
        protected override void Start() => ToggleState(IsOn);
        
        protected override void Interact(SelectionState state, bool instant)
        {
            switch (state)
            {
                // Hover always grows, on or off — same affordance AbstractButton gives.
                case SelectionState.Highlighted:
                    Scale(hoverScale);
                    break;
                case SelectionState.Normal:
                case SelectionState.Selected:
                    Scale(IsOn ? hoverScale : 1);
                    break;
                case SelectionState.Pressed:
                    Scale(IsOn? 1: hoverScale);
                    break;
                case SelectionState.Disabled:
                default:
                    ResetScale();
                    break;
            }
        }

        /// <summary>On, and the active member of a group whose <c>UserCanUntoggle</c> is off: a
        /// click on it is refused by <see cref="SetToggle"/>, so it must not look as if it were
        /// being switched off.</summary>
        public bool IsLockedOn => IsOn && RadioGroup && RadioGroup.ActiveMember == this && !RadioGroup.CanUntoggle(byUser: true);

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            // Normal is the resting look of an on toggle. Pressed on a locked-on one is the
            // un-toggle feedback for a click that will be refused, so it stays put instead.
            if (IsOn && state == SelectionState.Normal || IsLockedOn && state == SelectionState.Pressed)
                state = SelectionState.Selected;
            base.DoStateTransition(state, instant);
        }

        protected override void OnClick() => SetToggle(!IsOn);
        
        /// <summary>The group-aware entry point: a caller (click, hotkey, script) calls this
        /// exactly as it always has, and — if this toggle sits under a <see cref="RadioGroup"/>
        /// — the group takes over and drives <see cref="ToggleState"/> itself, deselecting
        /// whichever sibling was on. Ungrouped, it just applies.
        ///
        /// <para>This is the <b>user</b> side: switching the group's active member off is refused
        /// unless the group's <c>UserCanUntoggle</c> allows it. State derived from elsewhere
        /// goes through <see cref="SyncToggle"/> instead.</para></summary>
        public void SetToggle(bool toggleOn) => Apply(toggleOn, byUser: true);

        /// <summary>The <b>group</b> side of <see cref="SetToggle"/>, for a toggle that mirrors
        /// state owned elsewhere (a context, a run phase): the group's <c>GroupCanUntoggle</c>
        /// governs switching its active member off, not <c>UserCanUntoggle</c>, so a mirror can
        /// always follow the state it reflects without the user being allowed to click it off.</summary>
        public void SyncToggle(bool toggleOn) => Apply(toggleOn, byUser: false);

        private void Apply(bool toggleOn, bool byUser)
        {
            // Already in the requested state - except a grouped toggle that is on without being
            // the group's ActiveMember (authored on, never registered): it must still reach Activate.
            if (IsOn == toggleOn && (!toggleOn || !RadioGroup || RadioGroup.ActiveMember == this))
                return;
            
            if (RadioGroup)
            {
                if (toggleOn)
                    RadioGroup.Activate(this);
                else
                    RadioGroup.Deactivate(this, byUser);
                return;
            }

            ToggleState(toggleOn);
        }

        /// <summary>The actual state-change primitive. Internal so <see cref="ToggleGroup.Activate"/>
        /// / <see cref="ToggleGroup.Deactivate"/> can drive it directly on either side of a switch
        /// without looping back through the group-aware <see cref="SetToggle"/> — that loop is
        /// what would double-fire <see cref="OnToggle"/> on the toggle being replaced.</summary>
        internal void ToggleState(bool toggleOn)
        {
            IsOn = toggleOn;

            //Interact(SelectionState.Selected, true);
            DoStateTransition(currentSelectionState, false);
            
            if (image && toggledOffSprite && toggledOnSprite)
                image.sprite = IsOn ? toggledOnSprite : toggledOffSprite;

            OnToggle();
        }

        protected abstract void OnToggle();
    }
}
