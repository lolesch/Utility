using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public abstract class AbstractToggle : AbstractButton
    {
        //TODO: implement audio feedback on toggle
        
        [field: SerializeField] public bool IsOn { get; private set; } = false;

        /// <summary>A toggle's <see cref="RadioGroup"/> is automatically assigned if present on the toggle's parent.</summary>
        [field: SerializeField, ReadOnly] public RadioGroup RadioGroup { get; private set; }

        [SerializeField] private Sprite toggledOffSprite;
        [SerializeField] private Sprite toggledOnSprite;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            var resolved = transform.parent.GetComponent<RadioGroup>();

            if (RadioGroup && RadioGroup != resolved)
                RadioGroup.Deactivate(this);

            RadioGroup = resolved;

            if (IsOn && RadioGroup)
                RadioGroup.Activate(this);
        }
#endif //UNITY_EDITOR

        protected override void Awake()
        {
            base.Awake();

            if (!RadioGroup)
                RadioGroup = transform.parent.GetComponent<RadioGroup>();
        }

        /// <summary>Bypasses the group-aware <see cref="SetToggle"/> — mirrors
        /// <c>SimplePanel.Start</c> calling its internal primitive directly. A toggle authored
        /// as the group's selection already has <see cref="RadioGroup.ActiveMember"/> pointing
        /// at it (via <see cref="OnValidate"/>), so routing through <see cref="RadioGroup.Activate"/>
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

        [ContextMenu("Toggle")]
        protected override void OnClick() => SetToggle(!IsOn);
        
        /// <summary>The group-aware entry point: a caller (click, hotkey, script) calls this
        /// exactly as it always has, and — if this toggle sits under a <see cref="RadioGroup"/>
        /// — the group takes over and drives <see cref="ToggleState"/> itself, deselecting
        /// whichever sibling was on. Ungrouped, it just applies.</summary>
        public void SetToggle(bool toggleOn)
        {
            if (!toggleOn && IsOn && RadioGroup && RadioGroup.ActiveMember == this
                && !RadioGroup.IsClearable && !RadioGroup.IsRestorable)
            {
                Debug.Log("SetToggle(false) prevented. To allow un-toggle, enable 'IsClearable' or " +
                          $"'IsRestorable' in the RadioGroup, or re-parent {name} out of any RadioGroup.", RadioGroup);
                return;
            }

            if (RadioGroup)
            {
                if (toggleOn)
                    RadioGroup.Activate(this);
                else
                    RadioGroup.Deactivate(this);
            }
            else
            {
                ToggleState(toggleOn);
            }
        }

        /// <summary>The actual state-change primitive. Internal so <see cref="RadioGroup.Activate"/>
        /// / <see cref="RadioGroup.Deactivate"/> can drive it directly on either side of a switch
        /// without looping back through the group-aware <see cref="SetToggle"/> — that loop is
        /// what would double-fire <see cref="OnToggle"/> on the toggle being replaced.</summary>
        internal void ToggleState(bool toggleOn)
        {
            IsOn = toggleOn;

            Interact(SelectionState.Selected, true);

            if (image && toggledOffSprite && toggledOnSprite)
                image.sprite = IsOn ? toggledOnSprite : toggledOffSprite;

            OnToggle();
        }

        protected abstract void OnToggle();
    }
}
