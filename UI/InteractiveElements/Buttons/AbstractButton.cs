using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Submodules.Utility.UI
{
    public abstract class AbstractButton : InteractiveElement, IPointerClickHandler, ISubmitHandler
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

        /// <summary>The UI Submit (Enter on the selected button) presses it as a click does. A click shows the
        /// press through the pointer's state changes; a key press has none, so it is played here.</summary>
        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (!interactable)
                return;

            DoStateTransition(SelectionState.Pressed, false);
            _ = StartCoroutine(ReleaseAfterFade());

            OnClick();
        }

        private IEnumerator ReleaseAfterFade()
        {
            yield return new WaitForSecondsRealtime(colors.fadeDuration);
            DoStateTransition(currentSelectionState, false);
        }

        [ContextMenu("Click")]
        protected abstract void OnClick();
    }
}
