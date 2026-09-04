using Submodules.Utility.Extensions;
using Submodules.Utility.Tools.Tweening;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// The hover-hint mechanism, content left to the caller: a show-delay, cursor-follow
    /// each frame, a screen-edge clamp via pivot-flip by quadrant (mirrors
    /// <c>PreviewProvider.RefreshPreviewDisplay</c>'s trick), fade in / out via
    /// <see cref="Tween"/>, and the pending-show / visible / pending-hide state machine
    /// from <see cref="TooltipTiming{T}"/>. One instance is the ambient host for its scene -
    /// <see cref="InteractiveElement"/> finds it through <see cref="Current"/>, the same way
    /// runtime code finds <c>ItemView.Catalog</c>. Rendering <typeparamref name="T"/> is
    /// <see cref="Content"/>'s job; this class only ever calls <see cref="IView{T}.Refresh"/>
    /// on it.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public abstract class TooltipHost<T> : MonoBehaviour
    {
        [SerializeField] private float showDelay = 0.5f;
        [SerializeField] private float hideDebounce = 0.1f;
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private Vector2 cursorOffset = new(16f, -16f);

        private RectTransform rect;
        private RectTransform Rect => rect != null ? rect : rect = transform as RectTransform;

        private CanvasGroup canvasGroup;
        private CanvasGroup CanvasGroup => canvasGroup != null ? canvasGroup : canvasGroup = GetComponent<CanvasGroup>();

        private Canvas parentCanvas;
        private Canvas ParentCanvas => parentCanvas != null ? parentCanvas : parentCanvas = GetComponentInParent<Canvas>();

        private TooltipTiming<T> timing;

        /// <summary>
        /// The ambient host for whichever scene has one enabled - set in <see cref="OnEnable"/>,
        /// mirroring <c>ItemView.Catalog</c>. Null when no host is in the scene; callers use
        /// the null-conditional operator rather than assume one exists.
        /// </summary>
        public static TooltipHost<T> Current { get; private set; }

        /// <summary>The content renderer - the caller's adapter, never named by this class.</summary>
        protected abstract IView<T> Content { get; }

        protected virtual void Awake()
        {
            Rect.anchorMin = Vector2.zero;
            Rect.anchorMax = Vector2.zero;

            timing = new TooltipTiming<T>(showDelay, hideDebounce);
            timing.OnShow += HandleShow;
            timing.OnHide += HandleHide;

            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
        }

        protected virtual void OnEnable() => Current = this;

        protected virtual void OnDisable()
        {
            if (Current == this)
                Current = null;

            Tween.Kill(CanvasGroup);
        }

        protected virtual void Update()
        {
            timing.Tick(Time.unscaledDeltaTime);

            if (timing.State is TooltipVisibility.Visible or TooltipVisibility.PendingHide)
                FollowCursor();
        }

        /// <summary>Requests a hint for <paramref name="value"/> - a hover / select.</summary>
        public void Show(T value) => timing.Request(value);

        /// <summary>Requests the hint hide - an exit / deselect. Inert if nothing is showing.</summary>
        public void Hide() => timing.Cancel();

        private void HandleShow(T value)
        {
            Content.Refresh(value);

            FollowCursor();

            Tween.Kill(CanvasGroup);
            _ = CanvasGroup.TweenAlpha(1f, fadeDuration, Ease.OutQuad);
        }

        private void HandleHide()
        {
            Tween.Kill(CanvasGroup);
            _ = CanvasGroup.TweenAlpha(0f, fadeDuration, Ease.InQuad);
        }

        private void FollowCursor()
        {
            var mousePosition = Input.mousePosition;

            var showLeft = mousePosition.x < Screen.width * 0.5f;
            var showBottom = mousePosition.y < Screen.height * 0.5f;

            Rect.pivot = new Vector2(showLeft ? 0f : 1f, showBottom ? 0f : 1f);

            var scale = ParentCanvas != null ? ParentCanvas.scaleFactor : 1f;
            var mouse = (Vector2)mousePosition / scale;
            var offset = new Vector2(showLeft ? cursorOffset.x : -cursorOffset.x, showBottom ? cursorOffset.y : -cursorOffset.y);

            Rect.anchoredPosition = mouse + offset;
        }
    }

    /// <summary>
    /// The default <see cref="TooltipHost{T}"/> - one TMP line. The ready-to-drop hover-hint
    /// host: place one per scene, wire <see cref="content"/> to a
    /// <see cref="StringTooltipContent"/> child, and any <see cref="InteractiveElement"/> with
    /// a non-empty tooltip finds it through <see cref="TooltipHost{T}.Current"/>.
    /// </summary>
    public sealed class TooltipHost : TooltipHost<string>
    {
        [SerializeField] private StringTooltipContent content;

        protected override IView<string> Content => content;
    }
}
