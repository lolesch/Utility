using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI.InteractiveElements
{
    public abstract class AbstractToggle : AbstractButton
    {
        //TODO: implement audio feedback on toggle

        protected AbstractToggle()
        {
            staySelected = true;
        }
        
        [field: SerializeField] public bool IsOn { get; private set; } = false;

        [SerializeField, ReadOnly] protected RadioGroup radioGroup = null;
        public RadioGroup RadioGroup => radioGroup != null ? radioGroup : radioGroup = GetComponentInParent<RadioGroup>();

        [SerializeField] private Sprite toggledOffSprite;
        [SerializeField] private Sprite toggledOnSprite;


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (RadioGroup != null && RadioGroup.transform != transform.parent)
                radioGroup = null;

            if (IsOn && RadioGroup)
                RadioGroup.Activate(this);
        }
#endif //UNITY_EDITOR

        protected override void Start() => SetToggle(IsOn);
        
        protected override void Interact(SelectionState state, bool instant)
        {
            switch (state)
            {
                case SelectionState.Normal:
                case SelectionState.Highlighted:
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

        protected override void OnClick()
        {
            if (IsOn && RadioGroup && !RadioGroup.CanDeactivateAll)
                return;

            SetToggle(!IsOn);
        }
        
        public void SetToggle(bool isOn)
        {
            IsOn = isOn;

            Interact( SelectionState.Selected, true);
            
            if (toggledOffSprite != null && toggledOnSprite != null)
                image.sprite = IsOn ? toggledOnSprite : toggledOffSprite;

            if (RadioGroup)
            {
                if (IsOn)
                {
                    if (RadioGroup.ActivatedToggle != this)
                        RadioGroup.Activate(this);
                }
                else
                {
                    if (RadioGroup.ActivatedToggle == this)
                        RadioGroup.Deactivate(this);
                }
            }

            ToggleSideEffects();
        }

        protected abstract void ToggleSideEffects();
    }
}
