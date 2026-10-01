using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// The slider counterpart of <see cref="AbstractButton"/> / <see cref="AbstractToggle"/>: an
    /// <see cref="InteractiveElement"/> (so hover scale and press feedback come from it) that turns
    /// a horizontal drag across <see cref="slideArea"/> into a <see cref="Value"/>. It does not wrap
    /// <c>UnityEngine.UI.Slider</c>, same as the button and toggle do not wrap <c>Button</c> /
    /// <c>Toggle</c>.
    ///
    /// <see cref="Value"/> is always 0..1, empty to full. What it means is a derived class's job:
    /// <see cref="Format"/> is the one required override, and a concrete slider maps the value onto
    /// whatever it controls (a percentage reads it directly, sim speed runs it through a curve, an
    /// enum slider turns it into an index). <see cref="Steps"/> only limits where a drag can land.
    ///
    /// Feedback is the button's (<see cref="InteractiveElement.ApplyClickFeedback"/>): the handle
    /// grows on hover and returns to size while pressed, so a press reads as a push.
    ///
    /// Hierarchy, all children optional:
    /// <code>
    /// Slider            this component, plus a transparent raycast-target Image: the hit area.
    ///                   targetGraphic is the handle, so hover scales the handle, not the whole bar.
    ///  ├ Background
    ///  ├ FillMask       RectMask2D, stretched over the bar
    ///  │  └ Fill        Image, stretched; sliced or tiled sprites keep their look
    ///  ├ SlideArea      stretched horizontally; its left/right inset is set from the handle's width
    ///  │  └ Handle      anchored at the value
    ///  └ Label          TextMeshProUGUI
    /// </code>
    /// The slider's own rect is the interaction area, so make it larger than the visuals by sizing
    /// the root and insetting Background, FillMask and SlideArea.
    /// </summary>
    public abstract class AbstractSlider : InteractiveElement, IDragHandler, IInitializePotentialDragHandler
    {
        /// <summary>Empty to full, 0..1, already snapped to <see cref="Steps"/>.</summary>
        [field: SerializeField, Range(0f, 1f)] public float Value { get; private set; } = 0f;

        [Header("Bar")]
        [Tooltip("The span the value maps onto: pointer position, handle and fill end all use it. " +
                 "Child of this slider, stretched horizontally; its left/right inset is driven from the handle's width.")]
        [SerializeField] private RectTransform slideArea;
        [Tooltip("RectMask2D that clips its child to the current value. Cheaper than a stencil Mask and keeps sliced or tiled fills intact.")]
        [SerializeField] private RectMask2D fillMask;
        [Tooltip("Child of the slide area; anchored horizontally at the current value.")]
        [SerializeField] private RectTransform handle;

        [Header("Value readout")]
        [SerializeField] private TextMeshProUGUI valueLabel;

        private DrivenRectTransformTracker tracker;

        /// <summary>Raised with the snapped value whenever it changes. Code-only: subscribe in <c>OnEnable</c>, unsubscribe in <c>OnDisable</c>.</summary>
        public event Action<float> OnValueChanged;

        /// <summary>How many equal intervals the slider is cut into: <c>Steps + 1</c> values from empty to
        /// full, so 10 steps land on 0, 0.1 ... 1. 0 is continuous. Each slider decides where it comes
        /// from: authored (<see cref="ValueSlider"/>) or derived (<see cref="EnumSlider{TEnum}"/>).</summary>
        public abstract int Steps { get; }

        public bool IsStepped => 1 <= Steps;

        /// <summary>Which value <see cref="Value"/> is on, <c>0..Steps</c>. Always 0 on a continuous slider.</summary>
        public int StepIndex => ToStepIndex(Value);

        /// <summary>The text the label shows for the current value.</summary>
        public string FormattedValue => Format(Value);

        private RectTransform SlideRect => slideArea ? slideArea : (RectTransform)transform;

        /// <summary>Renders <paramref name="value"/> (0..1) for <see cref="valueLabel"/>.</summary>
        protected abstract string Format(float value);

        protected int ToStepIndex(float value) => IsStepped ? Mathf.RoundToInt(Mathf.Clamp01(value) * Steps) : 0;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            Value = Snap(Value);
            UpdateDrivers();

            // Refresh moves the slide area, and resizing a RectTransform sends a message that is not allowed inside OnValidate.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this)
                    Refresh();
            };
        }
#endif //UNITY_EDITOR

        protected override void OnEnable()
        {
            base.OnEnable();

            UpdateDrivers();
            Refresh();
        }

        protected override void OnDisable()
        {
            tracker.Clear();

            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            if (IsActive())
                Refresh();
        }

        /// <summary>Sets the value (clamped to 0..1, snapped to <see cref="Steps"/>) and raises <see cref="OnValueChanged"/> if it changed.</summary>
        public void SetValue(float value)
        {
            if (!Apply(value))
                return;

            OnValueChanged?.Invoke(Value);
        }

        public void SetValueWithoutNotify(float value) => Apply(value);

        /// <summary>Moves to a value on a stepped slider (<c>0..Steps</c>); the index is clamped.</summary>
        public void SetStepIndex(int index) => SetValue(IndexToValue(index));

        public void SetStepIndexWithoutNotify(int index) => SetValueWithoutNotify(IndexToValue(index));

        private float IndexToValue(int index) => IsStepped ? Mathf.Clamp(index, 0, Steps) / (float)Steps : 0f;

        private bool Apply(float value)
        {
            var snapped = Snap(value);

            if (Mathf.Approximately(snapped, Value))
            {
                // Still refresh: the label, mask and handle may not have been drawn yet (first enable).
                Refresh();
                return false;
            }

            Value = snapped;
            Refresh();
            return true;
        }

        private float Snap(float value)
        {
            value = Mathf.Clamp01(value);

            // Values sit on k / Steps, so the last one is exactly 1 and the fill always completes.
            return IsStepped ? Mathf.Round(value * Steps) / Steps : value;
        }

        public void Refresh()
        {
            FitSlideArea();
            UpdateHandle();
            UpdateFill();

            if (!valueLabel)
                return;

            var text = FormattedValue;

            // Skip identical text so an OnValidate pass does not dirty the scene for nothing.
            if (valueLabel.text != text)
                valueLabel.text = text;
        }

        private void UpdateDrivers()
        {
            tracker.Clear();

            if (handle)
                tracker.Add(this, handle, DrivenTransformProperties.AnchorMinX | DrivenTransformProperties.AnchorMaxX);

            if (CanFitSlideArea())
                tracker.Add(this, slideArea, DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX);
        }

        private bool CanFitSlideArea() =>
            handle && slideArea && slideArea != transform && slideArea.anchorMin.x == 0f && slideArea.anchorMax.x == 1f;

        /// <summary>Insets the slide area by the handle's overhang on each side, so the handle stays
        /// inside the bar at both ends — what a stock slider gets from its Handle Slide Area padding.</summary>
        private void FitSlideArea()
        {
            if (!CanFitSlideArea())
                return;

            var width = handle.rect.width;
            var left = width * handle.pivot.x;
            var right = width * (1f - handle.pivot.x);

            var offsetMin = slideArea.offsetMin;
            var offsetMax = slideArea.offsetMax;

            if (Mathf.Approximately(offsetMin.x, left) && Mathf.Approximately(offsetMax.x, -right))
                return;

            slideArea.offsetMin = new Vector2(left, offsetMin.y);
            slideArea.offsetMax = new Vector2(-right, offsetMax.y);
        }

        private void UpdateHandle()
        {
            if (!handle)
                return;

            var anchorMin = handle.anchorMin;
            var anchorMax = handle.anchorMax;

            if (Mathf.Approximately(anchorMin.x, Value) && Mathf.Approximately(anchorMax.x, Value))
                return;

            handle.anchorMin = new Vector2(Value, anchorMin.y);
            handle.anchorMax = new Vector2(Value, anchorMax.y);
        }

        /// <summary>Clips the fill at the same point the handle sits, measured in the mask's own space,
        /// so the fill ends under the handle's centre however the two rects are inset.</summary>
        private void UpdateFill()
        {
            if (!fillMask)
                return;

            var slide = SlideRect;
            var maskRect = fillMask.rectTransform;

            var end = slide.TransformPoint(new Vector3(Mathf.Lerp(slide.rect.xMin, slide.rect.xMax, Value), 0f, 0f));
            var localEnd = maskRect.InverseTransformPoint(end).x;

            var padding = fillMask.padding;
            padding.z = Mathf.Max(0f, maskRect.rect.xMax - localEnd);

            if (fillMask.padding != padding)
                fillMask.padding = padding;
        }

        protected override void Interact(SelectionState state, bool instant) => ApplyClickFeedback(state);

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            SetFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData) => SetFromPointer(eventData);

        /// <summary>Stops a parent ScrollRect from claiming the drag before this slider sees it.</summary>
        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        private void SetFromPointer(PointerEventData eventData)
        {
            if (!IsActive() || !interactable || eventData.button != PointerEventData.InputButton.Left)
                return;

            var rect = SlideRect;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var local))
                return;

            SetValue(Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, local.x));
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !interactable)
            {
                base.OnMove(eventData);
                return;
            }

            var increment = IsStepped ? 1f / Steps : .1f;

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    SetValue(Value - increment);
                    break;
                case MoveDirection.Right:
                    SetValue(Value + increment);
                    break;
                default:
                    base.OnMove(eventData);
                    break;
            }
        }
    }
}
