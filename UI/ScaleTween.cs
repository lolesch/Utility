using Submodules.Utility.Extensions;
using Submodules.Utility.Tools.Tweening;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A brief scale punch — up to <see cref="punchScale"/> and back to 1x — for a one-shot
    /// visual acknowledgement (the Ability hotbar's flash-on-attack, issue #62). Reuses the
    /// shared <see cref="TweenExtensions.TweenScale(Transform,float,float,Ease)"/> helper, which
    /// already tags each leg with the <see cref="Transform"/> it runs on, so <see cref="Punch"/>
    /// killing that tag before starting a fresh punch is what keeps a rapid burst from stacking
    /// two legs against each other and leaving the icon stuck off 1x.
    /// </summary>
    public sealed class ScaleTween : MonoBehaviour
    {
        [SerializeField] private float punchScale = 1.2f;
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private Ease ease = Ease.OutQuad;

        public void Punch()
        {
            Tween.Kill(transform);
            transform.localScale = Vector3.one;
            transform.TweenScale(punchScale, duration, ease)
                .OnComplete(() => transform.TweenScale(1f, duration, ease));
        }

        private void OnDisable() => Tween.Kill(transform);
    }
}
