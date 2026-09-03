using Submodules.Utility.Tools.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Submodules.Utility.Extensions
{
    /// <summary>
    /// The property tweens the UI components actually use, one call each — the
    /// <c>canvasGroup.DOFade(...)</c> replacements. Each captures the current value as the
    /// start, tags the tween with the component (so <see cref="Tween.Kill(object)"/> /
    /// <see cref="Tween.IsTweening(object)"/> find it) and links its lifetime to that
    /// component. This is also the seam a richer library (PrimeTween, LitMotion) would
    /// slot in behind.
    /// </summary>
    public static class TweenExtensions
    {
        public static Tween TweenAlpha( this CanvasGroup canvasGroup, float to, float duration, Ease ease = Ease.Linear )
        {
            var from = canvasGroup.alpha;
            return Tween.Play( duration, ease, t => canvasGroup.alpha = Mathf.LerpUnclamped( from, to, t ) )
                .SetTarget( canvasGroup )
                .LinkTo( canvasGroup );
        }

        public static Tween TweenAnchoredPosition( this RectTransform rectTransform, Vector2 to, float duration, Ease ease = Ease.Linear )
        {
            var from = rectTransform.anchoredPosition;
            return Tween.Play( duration, ease, t => rectTransform.anchoredPosition = Vector2.LerpUnclamped( from, to, t ) )
                .SetTarget( rectTransform )
                .LinkTo( rectTransform );
        }

        public static Tween TweenScale( this Transform transform, Vector3 to, float duration, Ease ease = Ease.Linear )
        {
            var from = transform.localScale;
            return Tween.Play( duration, ease, t => transform.localScale = Vector3.LerpUnclamped( from, to, t ) )
                .SetTarget( transform )
                .LinkTo( transform );
        }

        public static Tween TweenScale( this Transform transform, float uniformTo, float duration, Ease ease = Ease.Linear ) =>
            transform.TweenScale( new Vector3( uniformTo, uniformTo, uniformTo ), duration, ease );

        public static Tween TweenColor( this Graphic graphic, Color to, float duration, Ease ease = Ease.Linear )
        {
            var from = graphic.color;
            return Tween.Play( duration, ease, t => graphic.color = Color.LerpUnclamped( from, to, t ) )
                .SetTarget( graphic )
                .LinkTo( graphic );
        }

        public static Tween TweenFillAmount( this Image image, float to, float duration, Ease ease = Ease.Linear )
        {
            var from = image.fillAmount;
            return Tween.Play( duration, ease, t => image.fillAmount = Mathf.LerpUnclamped( from, to, t ) )
                .SetTarget( image )
                .LinkTo( image );
        }
    }
}
