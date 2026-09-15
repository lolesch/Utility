using Submodules.Utility.Extensions;
using Submodules.Utility.Tools.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Submodules.Utility.UI.InteractiveElements
{
    [RequireComponent(typeof(GraphicRaycaster), typeof(CanvasRenderer))]
    public class InteractiveElement : Selectable
    {
        //TODO: show tooltip on hover -> import and update tooltip from LOCA CSV 
        //TODO: implement audio feedback on hover

        [SerializeField] protected bool staySelected = false;
        [SerializeField, Range(.8f, 1.2f)] protected float hoverScale = 1.06f;
        
        protected override void Awake()
        {
            base.Awake();

            if (targetGraphic)
                targetGraphic.raycastTarget = interactable;
            else
                LogExtensions.MissingComponent(nameof(Graphic), gameObject);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (targetGraphic && Tween.IsTweening(targetGraphic.transform))
                Tween.Kill(targetGraphic.transform);
        }
        
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            Interact(state, instant);
        }

        protected virtual void Interact(SelectionState state, bool instant)
        {
            switch (state)
            {
                case SelectionState.Highlighted:
                    Scale(hoverScale);
                    break;
                // ensures that clicks have no visual feedback
                case SelectionState.Pressed:
                case SelectionState.Selected:
                    targetGraphic.CrossFadeColor( colors.highlightedColor,  instant ? 0f : colors.fadeDuration, true, true);
                    break;
                case SelectionState.Normal:
                case SelectionState.Disabled:
                default:
                    ResetScale();
                    break;
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            
            if (EventSystem.current.currentSelectedGameObject == gameObject && !staySelected)
                EventSystem.current.SetSelectedGameObject(null);
        }

        protected void ResetScale() => Scale(1f);

        protected void Scale( float factor )
        {
            if (targetGraphic)
                _ = targetGraphic.transform.TweenScale( factor, colors.fadeDuration, Ease.InOutSine);
        }
    }
}
