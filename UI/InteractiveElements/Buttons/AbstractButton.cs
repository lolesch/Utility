using UnityEngine;
using UnityEngine.EventSystems;

namespace Submodules.Utility.UI
{
    public abstract class AbstractButton : InteractiveElement, IPointerClickHandler
    {
        //TODO: disable the button for x seconds to disable button spamming
        //TODO: implement audio feedback on click
        
        protected override void Interact(SelectionState state, bool instant) => ApplyClickFeedback(state);

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable)
                return;

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            OnClick();
        }
        
        [ContextMenu("Click")]
        protected abstract void OnClick();
    }
}
