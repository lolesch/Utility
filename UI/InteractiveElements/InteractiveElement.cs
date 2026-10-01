using Submodules.Utility.Extensions;
using Submodules.Utility.Tools.Tweening;
using TMPro;
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

        [Space]
        [SerializeField] protected bool staySelected = false;
        [SerializeField, Range(.8f, 1.2f)] protected float hoverScale = 1.06f;

        [Tooltip("Optional: shows a tooltip while it is highlighted (hover or navigation) and hides it otherwise.")]
        [SerializeField] private string tooltip;
        [SerializeField] private TextMeshProUGUI tooltipLabel;

        protected override void Awake()
        {
            base.Awake();

            if (!targetGraphic)
                LogExtensions.MissingComponent(nameof(Graphic), gameObject);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            ShowTooltip(false);

            if (targetGraphic && Tween.IsTweening(targetGraphic.transform))
                Tween.Kill(targetGraphic.transform);
        }
        
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            Interact(state, instant);
            
            ShowTooltip( state is not (SelectionState.Disabled or SelectionState.Normal) );
        }

        private void ShowTooltip(bool show)
        {
            // A label on this very object would switch the element off with it.
            if (!tooltipLabel || tooltipLabel.gameObject == gameObject)
                return;

            if (string.IsNullOrWhiteSpace(tooltip))
                return;
            
            if (show)
                tooltipLabel.text = tooltip;

            if (tooltipLabel.gameObject.activeSelf != show)
                tooltipLabel.gameObject.SetActive(show);
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

        /// <summary>The click affordance of a button: grows on hover and while selected, returns to size
        /// on press, so the press reads as a push. <see cref="AbstractButton"/> and
        /// <see cref="AbstractSlider"/> both use it.</summary>
        protected void ApplyClickFeedback(SelectionState state)
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
