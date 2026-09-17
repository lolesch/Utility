using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public abstract class AbstractToggle : AbstractButton
    {
        //TODO: implement audio feedback on toggle
        
        [field: SerializeField] public bool IsOn { get; private set; } = false;

        /// <summary>A toggle's group is wherever it sits in the hierarchy — the nearest
        /// <see cref="RadioGroup"/> ancestor — never assigned directly. A toggle that needs a
        /// different group belongs under a different parent, not pointed at a group that sits
        /// elsewhere.</summary>
        [SerializeField, ReadOnly] protected RadioGroup radioGroup = null;
        public RadioGroup RadioGroup => radioGroup != null ? radioGroup : radioGroup = GetComponentInParent<RadioGroup>();

        [SerializeField] private Sprite toggledOffSprite;
        [SerializeField] private Sprite toggledOnSprite;


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (RadioGroup && RadioGroup.transform != transform.parent)
            {
                RadioGroup.Deselect(this);
                radioGroup = null;
            }

            if (IsOn && RadioGroup)
                RadioGroup.Select(this);
        }
#endif //UNITY_EDITOR

        protected override void Start() => SetToggle(IsOn);
        
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
        
        public void SetToggle(bool toggleOn)
        {
            if (!toggleOn && IsOn && RadioGroup && RadioGroup.SelectedToggle == this && !RadioGroup.IsDeselectable)
            {
                Debug.Log("SetToggle(false) prevented. To allow un-toggle, enable 'IsDeselectable' in the RadioGroup," +
                          $" or re-parent {name} out of any RadioGroup.", RadioGroup);
                return;
            }
            
            IsOn = toggleOn;

            Interact( SelectionState.Selected, true);
            
            if (image && toggledOffSprite && toggledOnSprite)
                image.sprite = IsOn ? toggledOnSprite : toggledOffSprite;

            OnToggle();

            if (RadioGroup)
            {
                if (RadioGroup.SelectedToggle == this)
                {
                    if (!IsOn)
                        RadioGroup.Deselect(this);
                }
                else
                {
                    if (IsOn)
                        RadioGroup.Select(this);
                }
            }
        }

        protected abstract void OnToggle();
    }
}
