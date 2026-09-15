using Submodules.Utility.Extensions;
using Submodules.Utility.Tools.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Submodules.Utility.UI
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

            if (!targetGraphic)
                LogExtensions.MissingComponent(nameof(Graphic), gameObject);

            SyncRaycastTarget();
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

            SyncRaycastTarget();

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
                    if (targetGraphic)
                        targetGraphic.CrossFadeColor( colors.highlightedColor, instant ? 0f : colors.fadeDuration, true, true);
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

        /// <summary>
        /// Keeps raycast reception in step with <see cref="Selectable.interactable"/> for the
        /// life of the component. Setting <c>interactable</c> routes through
        /// <see cref="DoStateTransition"/>, so this is the one place that sees every change —
        /// syncing only in <c>Awake</c> freezes the flag at its authored value and leaves an
        /// element that is re-enabled at runtime permanently unclickable.
        /// </summary>
        private void SyncRaycastTarget()
        {
            if (targetGraphic)
                targetGraphic.raycastTarget = interactable;
        }

        protected void ResetScale() => Scale(1f);

        protected void Scale( float factor )
        {
            // The tween pump is installed by TimerBootstrapper's [RuntimeInitializeOnLoadMethod],
            // so it only ticks in play mode. Selectable is [ExecuteAlways] and OnValidate drives
            // DoStateTransition in the editor — starting a tween there leaks a handle that never
            // advances, never completes, and leaves IsTweening true forever.
            if (!Application.isPlaying)
                return;

            if (targetGraphic)
                _ = targetGraphic.transform.TweenScale( factor, colors.fadeDuration, Ease.InOutSine);
        }
    }
}
