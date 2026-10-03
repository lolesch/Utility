using NaughtyAttributes;
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

        private CanvasGroup canvasGroup = null;
        private CanvasGroup CanvasGroup => canvasGroup ? canvasGroup : canvasGroup = GetComponent<CanvasGroup>();

        private RectTransform Transform => transform as RectTransform;

        /// <summary>A panel's <see cref="RadioGroup"/> is automatically assigned if present on the panel's parent.</summary>
        [field: SerializeField, ReadOnly] public PanelGroup RadioGroup { get; private set; }
        
        #endregion COMPONENT REFERENCES

        [field: SerializeField, Range(0, 1)] public float FadeDuration { get; private set; } = .2f;

        [SerializeField, Range(0f, 2f)] protected float scaleFrom = 1f;
        [SerializeField] protected Vector2 moveFrom = Vector2.zero;
        
        public bool IsCollapsed => CanvasGroup.alpha <= 0f;
        public bool IsInteractive => CanvasGroup.blocksRaycasts;
        public bool IsExtended => CanvasGroup.alpha >= 1;

        private Vector2 startPosition;

        private bool IsScaling => !Mathf.Approximately(scaleFrom, 1f);
        private bool IsMoving => moveFrom != Vector2.zero;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            var resolved = transform.parent?.GetComponent<PanelGroup>();
            
            if (RadioGroup && RadioGroup != resolved)
                RadioGroup.Deactivate(this, byUser: false);
            
            RadioGroup = resolved;
        }
#endif //UNITY_EDITOR

        protected virtual void Awake()
        {
            startPosition = Transform.anchoredPosition;
            
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.alpha = 1;

            if(!RadioGroup)
               RadioGroup = transform.parent?.GetComponent<PanelGroup>();
            
            if (RadioGroup && RadioGroup.ActiveMember == this)
                RadioGroup.Deactivate(this);
            else
                Disappear(true);
        }

        protected virtual void OnDisable() => KillTweens();

        public void ToggleState(bool toggleOn)
        {
            if (toggleOn)
                Expand();
            else
                Collapse();
        }

        /// <summary>The group-aware entry point: a caller (toggle, button, key handler) calls
        /// this exactly as it always has, and — if this panel sits under a
        /// <see cref="RadioGroup"/> — the group takes over and drives <see cref="Appear"/>
        /// itself, hiding whichever sibling was up first. Ungrouped, it just appears.</summary>
        [ContextMenu("Expand")]
        public virtual void Expand()
        {
            if (RadioGroup)
                RadioGroup.Activate(this);
            else
                Appear();
        }

        /// <summary>The group-aware entry point, mirroring <see cref="Expand"/>. Prevented
        /// outright on the sole active panel of a group the user cannot untoggle
        /// (<see cref="AbstractGroup{TMember}.UserCanUntoggle"/>) — the same guard <see cref="AbstractToggle.SetToggle"/> has for
        /// un-toggling the active one.</summary>
        [ContextMenu("Collapse")]
        public void Collapse()
        {
            if (RadioGroup && RadioGroup.ActiveMember == this && !RadioGroup.CanUntoggle(byUser: true))
            {
                Debug.Log("Collapse() prevented. Enable 'UserCanUntoggle' in the " +
                          $"RadioGroup, or re-parent {name} out of any RadioGroup.", RadioGroup);
                return;
            }

            if (RadioGroup && RadioGroup.ActiveMember == this)
                RadioGroup.Deactivate(this);
            else
                Disappear();
        }

        /// <summary>The actual appear primitive, named to match <see cref="AbstractToggle.SetToggle"/>'s
        /// on/off vocabulary. Internal so <see cref="PanelGroup.Activate"/> can drive it directly
        /// without looping back through the group-aware <see cref="Expand"/> — that loop is
        /// what silently double-fired <see cref="BeforeAppear"/> before.</summary>
        internal void Appear(bool instant = false)
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

        /// <summary> Called right before the CanvasGroup fades in.
        /// </summary>
        protected virtual void BeforeAppear()
        {
            // refresh data -> IDisplay?
            CanvasGroup.blocksRaycasts = true;
        } 

        /// <summary> Called after the CanvasGroup completed fading in.
        /// </summary>
        protected virtual void OnAppear()
        {
            CanvasGroup.alpha = 1;
        }

        /// <summary>The actual disappear primitive, named to match <see cref="AbstractToggle.SetToggle"/>'s
        /// on/off vocabulary. Internal so <see cref="PanelGroup.Activate"/> (fading out the replaced
        /// panel) and <see cref="PanelGroup.Deactivate"/> can drive it directly without looping back
        /// through the group-aware <see cref="Collapse"/>.</summary>
        internal void Disappear(bool instant = false)
        {
            KillTweens();
            BeforeDisappear();

            if (instant)
            {
                if (IsMoving)
                    Transform.anchoredPosition = startPosition + moveFrom;
                if (IsScaling)
                    Transform.localScale = new Vector3(scaleFrom, scaleFrom, 1f);
                
                OnDisappear();
                return;
            }

            _ = CanvasGroup.TweenAlpha(0f, FadeDuration, Ease.InQuad).OnComplete(OnDisappear);

            if (IsMoving)
                _ = Transform.TweenAnchoredPosition(startPosition + moveFrom, FadeDuration, Ease.InQuad);

            if (IsScaling)
                _ = Transform.TweenScale(scaleFrom, FadeDuration, Ease.InQuad);
        }

        /// <summary> Called right before the CanvasGroup fades out.
        /// </summary>
        protected virtual void BeforeDisappear() => CanvasGroup.blocksRaycasts = false;

        /// <summary> Called after the CanvasGroup completed fading out.
        /// </summary>
        protected virtual void OnDisappear() => CanvasGroup.alpha = 0;

        /// <summary>Stops whatever fade is in flight so the one about to start — or the
        /// disable that is about to strand it — is not raced by the old tween's
        /// <see cref="OnAppear"/>/<see cref="OnDisappear"/> landing on state that has since
        /// moved. No null guard: <see cref="CanvasGroup"/> is <c>[RequireComponent]</c>d and
        /// resolved lazily, so it cannot be null here, and <see cref="Tween.Kill(object)"/>
        /// already no-ops on null.</summary>
        private void KillTweens()
        {
            Tween.Kill(CanvasGroup);
            Tween.Kill(Transform);
        }
    }
}
