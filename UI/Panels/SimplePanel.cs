using Submodules.Utility.Extensions;
using Submodules.Utility.Tools.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// Panels provide appearance options such as fading in and out, scaling and movement.
    /// A panel should always stay enabled, only its canvasGroup alpha is set to 0.
    /// Therefore, use <see cref="BeforeAppear"/> instead of <see cref="OnEnable"/> to set data.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup), typeof(GraphicRaycaster))]
    public class SimplePanel : MonoBehaviour
    {
        #region COMPONENT REFERENCES
        protected CanvasGroup _canvasGroup = null;
        public CanvasGroup CanvasGroup => _canvasGroup != null ? _canvasGroup : _canvasGroup = GetComponentInParent<CanvasGroup>();

        protected RectTransform _transform = null;
        public RectTransform Transform => _transform != null ? _transform : _transform = GetComponentInParent<RectTransform>();
        #endregion COMPONENT REFERENCES

        [field: SerializeField, Range(0, 1)] public float FadeDuration { get; } = .2f;

        [SerializeField, Range(0f, 2f)] protected float scaleFrom = 1f;
        [SerializeField] protected Vector2 moveFrom = Vector2.zero;

        private Vector2 startPosition;

        protected bool IsScaling => scaleFrom != 1f;
        protected bool IsMoving => moveFrom != Vector2.zero;

        protected virtual void Awake()
        {
            startPosition = Transform.anchoredPosition;

            FadeOut(true);
        }

        private void OnDisable()
        {
            KillTweens();

            OnPanelDisable();
        }

        /// <summary>
        /// Called whenever the panel is disabled or destroyed. Override this instead of
        /// declaring <c>OnDisable</c>/<c>OnDestroy</c> directly — those are Unity magic
        /// methods, so a subclass declaring its own <c>OnDisable</c> would silently hide this
        /// class's cleanup instead of extending it (Unity dispatches to the most-derived
        /// declaration only, with no compiler error for the missing <c>override</c>).
        /// May run twice on a normal destroy of an enabled panel (once via <c>OnDisable</c>,
        /// once via <c>OnDestroy</c>) — keep overrides idempotent, the way <c>KillTweens</c> is.
        /// </summary>
        //TODO: keep this or move overrides into OnDisappear?
        protected virtual void OnPanelDisable() { }

        [ContextMenu("FadeIn")]
        public virtual void FadeIn() => FadeIn(false);

        private void FadeIn(bool instant)
        {
            KillTweens();
            BeforeAppear();

            if (instant)
            {
                OnAppear();
                return;
            }

            _ = CanvasGroup.TweenAlpha(1f, FadeDuration, Ease.InOutQuad).OnComplete(OnAppear);

            if (IsMoving)
                _ = Transform.TweenAnchoredPosition(startPosition, FadeDuration, Ease.InOutQuad);

            if (IsScaling)
            {
                Transform.localScale = new Vector3(scaleFrom, scaleFrom, 1f);
                _ = Transform.TweenScale(1f, FadeDuration, Ease.InOutQuad);
            }
        }

        /// <summary>
        /// Called right before the CanvasGroup fades in.
        /// </summary>
        protected virtual void BeforeAppear()
        {
            // refresh data -> IView? 
        }

        /// <summary>
        /// Called right before the CanvasGroup fades out.
        /// </summary>
        protected virtual void BeforeDisappear()
        {
            CanvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Called after the CanvasGroup completed fading in.
        /// </summary>
        protected virtual void OnAppear()
        {
            CanvasGroup.alpha = 1;
            CanvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Called after the CanvasGroup completed fading out.
        /// </summary>
        protected virtual void OnDisappear()
        {
            CanvasGroup.alpha = 0;
        }

        [ContextMenu("FadeOut")]
        public void FadeOut() => FadeOut( false );

        private void FadeOut(bool instant)
        {
            KillTweens();
            BeforeDisappear();

            if (instant)
            {
                OnDisappear();
                return;
            }

            _ = CanvasGroup.TweenAlpha(0f, FadeDuration, Ease.InQuad).OnComplete(OnDisappear);

            if (IsMoving)
                _ = Transform.TweenAnchoredPosition(startPosition + moveFrom, FadeDuration, Ease.InQuad);

            if (IsScaling)
                _ = Transform.TweenScale(scaleFrom, FadeDuration, Ease.InQuad);
        }

        //[ContextMenu("Toggle Visibility")]
        //private void Toggle() => Toggle(CanvasGroup.alpha < 1);

        public void Toggle(bool toggleOn)
        {
            if (toggleOn)
                FadeIn();
            else
                FadeOut();
        }

        private void KillTweens()
        {
            if (CanvasGroup)
                return;

            Tween.Kill(CanvasGroup);
            Tween.Kill(Transform);
        }
    }
}
