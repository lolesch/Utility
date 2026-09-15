using UnityEngine.EventSystems;

namespace Submodules.Utility.UI.InteractiveElements
{
    public abstract class AbstractButton : InteractiveElement, IPointerClickHandler
    {
        //TODO: disable the button for x seconds to disable button spamming
        //TODO: implement audio feedback on click

        protected AbstractButton()
        {
            staySelected = true;
        }
        
        protected override void Interact(SelectionState state, bool instant)
        {
            switch (state)
            {
                case SelectionState.Highlighted:
                case SelectionState.Selected:
                    Scale(hoverScale);
                    break;
                case SelectionState.Normal:
                case SelectionState.Pressed:
                case SelectionState.Disabled:
                default:
                    ResetScale();
                    break;
            }
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            OnClick();
        }
        
        protected abstract void OnClick();
    }
}
