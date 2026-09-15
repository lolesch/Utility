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
        /// elsewhere: that is exactly how a group used to end up with an
        /// <see cref="RadioGroup.ActivatedToggle"/> that was not one of its own children —
        /// the group's own editor validation now self-heals that, but the fix is to not
        /// produce it in the first place.</summary>
        [SerializeField, ReadOnly] protected RadioGroup radioGroup = null;
        public RadioGroup RadioGroup => radioGroup != null ? radioGroup : radioGroup = GetComponentInParent<RadioGroup>();

        [SerializeField] private Sprite toggledOffSprite;
        [SerializeField] private Sprite toggledOnSprite;


#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (RadioGroup != null && RadioGroup.transform != transform.parent)
            {
                RadioGroup.Deactivate(this);
                radioGroup = null;
            }

            if (IsOn && RadioGroup)
                RadioGroup.Activate(this);
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

        protected override void OnClick()
        {
            if (IsOn && RadioGroup && !RadioGroup.CanDeactivateAll)
                return;

            SetToggle(!IsOn);
        }
        
        [ContextMenu("Toggle")]
        private void Toggle() => SetToggle(!IsOn);
        
        public void SetToggle(bool toggleOn)
        {
            IsOn = toggleOn;

            Interact( SelectionState.Selected, true);
            
            // `image` is the target graphic cast to Image — null whenever it is any other
            // Graphic, which nothing in the inspector forbids while the sprites are set.
            if (image != null && toggledOffSprite != null && toggledOnSprite != null)
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

            OnToggle();
        }

        protected abstract void OnToggle();
    }
}
