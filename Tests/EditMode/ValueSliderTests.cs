using System.Globalization;
using NUnit.Framework;
using Submodules.Utility.Tests.TestSupport;
using Submodules.Utility.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// Pins what <see cref="AbstractSlider"/> owns, through <see cref="ValueSlider"/>: the 0..1 value
    /// and its snapping to <c>Steps</c> intervals, the change event, the pointer-to-value mapping, and how the
    /// fill mask, handle and label follow the value. Hover scale is <see cref="InteractiveElement"/>'s
    /// and is not retested here (see <see cref="InteractiveElementTests"/>).
    ///
    /// The slider sits on an overlay canvas at the origin and is 200 wide, so a pointer at screen
    /// x == local x: -100 is the left end of the root, 0 the middle, 100 the right end.
    /// </summary>
    [TestFixture]
    public sealed class ValueSliderTests
    {
        private const float HandleWidth = 20f;

        private UiTestScene scene;

        [SetUp]
        public void SetUp() => scene = new UiTestScene();

        [TearDown]
        public void TearDown() => scene.Dispose();

        private ValueSlider Build(int steps = 0, bool interactable = true, bool floatingPoint = false,
            bool withLabel = false, bool withMask = false, bool withHandle = false, bool withSlideArea = false)
        {
            var slider = scene.Element<ValueSlider>(interactable);
            ((RectTransform)slider.transform).sizeDelta = new Vector2(200f, 20f);

            var serialized = new UnityEditor.SerializedObject(slider);
            serialized.FindProperty("steps").intValue = steps;
            serialized.FindProperty("useFloatingPoint").boolValue = floatingPoint;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (withLabel)
                UiTestScene.SetObject(slider, "valueLabel", Child<TextMeshProUGUI>(slider.transform, "label"));

            // The mask spans the whole root; the slide area (when present) is stretched like it would be authored.
            if (withMask)
            {
                var mask = Child<RectMask2D>(slider.transform, "mask");
                Stretch((RectTransform)mask.transform);
                UiTestScene.SetObject(slider, "fillMask", mask);
            }

            var handleParent = slider.transform;

            if (withSlideArea)
            {
                var area = Child<RectTransform>(slider.transform, "slide-area");
                Stretch(area);
                UiTestScene.SetObject(slider, "slideArea", area);
                handleParent = area;
            }

            if (withHandle)
            {
                var handle = Child<RectTransform>(handleParent, "handle");
                handle.sizeDelta = new Vector2(HandleWidth, 20f);
                UiTestScene.SetObject(slider, "handle", handle);
            }

            slider.Refresh();

            return slider;
        }

        private static T Child<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            // A RectTransform is already on the object; AddComponent<RectTransform> would hand back null.
            return go.TryGetComponent<T>(out var existing) ? existing : go.AddComponent<T>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static PointerEventData PointerAt(float x) =>
            new(EventSystem.current) { button = PointerEventData.InputButton.Left, position = new Vector2(x, 0f) };

        [Test]
        public void TheValue_IsClampedToZeroToOne()
        {
            var slider = Build();

            slider.SetValue(5f);
            Assert.That(slider.Value, Is.EqualTo(1f));

            slider.SetValue(-5f);
            Assert.That(slider.Value, Is.Zero);
        }

        [Test]
        public void ASteppedSlider_SnapsToTheNearestPosition()
        {
            var slider = Build(steps: 3);

            slider.SetValue(.4f);

            Assert.That(slider.Value, Is.EqualTo(1f / 3f).Within(1e-5f), "3 steps: values are 0, 1/3, 2/3, 1");
            Assert.That(slider.StepIndex, Is.EqualTo(1));
        }

        [Test]
        public void ASteppedSlider_LastPositionIsExactlyFull_SoTheFillCompletes()
        {
            var slider = Build(steps: 3);

            slider.SetStepIndex(3);

            Assert.That(slider.Value, Is.EqualTo(1f));
        }

        [Test]
        public void SetStepIndex_ClampsToTheOfferedPositions()
        {
            var slider = Build(steps: 3);

            slider.SetStepIndex(99);
            Assert.That(slider.StepIndex, Is.EqualTo(3));

            slider.SetStepIndex(-1);
            Assert.That(slider.StepIndex, Is.Zero);
        }

        [Test]
        public void ZeroSteps_IsContinuous()
        {
            var slider = Build(steps: 0);

            slider.SetValue(.37f);

            Assert.That(slider.IsStepped, Is.False);
            Assert.That(slider.Value, Is.EqualTo(.37f).Within(1e-5f));
        }

        [Test]
        public void TenSteps_AreElevenValuesInTenthsIncrements()
        {
            var slider = Build(steps: 10);

            slider.SetValue(.26f);
            Assert.That(slider.Value, Is.EqualTo(.3f).Within(1e-5f));
            Assert.That(slider.StepIndex, Is.EqualTo(3));

            slider.SetStepIndex(10);
            Assert.That(slider.Value, Is.EqualTo(1f));
        }

        [Test]
        public void OneStep_IsJustEmptyOrFull()
        {
            var slider = Build(steps: 1);

            slider.SetValue(.4f);
            Assert.That(slider.Value, Is.Zero);

            slider.SetValue(.6f);
            Assert.That(slider.Value, Is.EqualTo(1f));
        }

        [Test]
        public void SetValue_RaisesTheEventOnceWithTheSnappedValue()
        {
            var slider = Build(steps: 10);
            var received = new System.Collections.Generic.List<float>();
            slider.OnValueChanged += received.Add;

            slider.SetValue(.34f);

            Assert.That(received.Count, Is.EqualTo(1));
            Assert.That(received[0], Is.EqualTo(.3f).Within(1e-5f));
        }

        [Test]
        public void SetValue_ToTheCurrentPosition_DoesNotRaiseTheEvent()
        {
            var slider = Build(steps: 10);
            slider.SetValue(.3f);
            var raised = 0;
            slider.OnValueChanged += _ => raised++;

            slider.SetValue(.32f);

            Assert.That(raised, Is.Zero, "0.32 snaps to the 0.3 it already is");
        }

        [Test]
        public void SetValueWithoutNotify_ChangesTheValueButStaysSilent()
        {
            var slider = Build();
            var raised = 0;
            slider.OnValueChanged += _ => raised++;

            slider.SetValueWithoutNotify(.4f);

            Assert.That(slider.Value, Is.EqualTo(.4f));
            Assert.That(raised, Is.Zero);
        }

        [Test]
        public void PressingTheSlider_MapsThePointerToAValue()
        {
            var slider = Build();

            slider.OnPointerDown(PointerAt(0f));
            Assert.That(slider.Value, Is.EqualTo(.5f).Within(1e-3f));

            slider.OnDrag(PointerAt(100f));
            Assert.That(slider.Value, Is.EqualTo(1f).Within(1e-3f));

            slider.OnDrag(PointerAt(-300f));
            Assert.That(slider.Value, Is.Zero.Within(1e-3f), "dragging past the end clamps");
        }

        [Test]
        public void Dragging_SnapsToThePositions()
        {
            var slider = Build(steps: 3);

            slider.OnDrag(PointerAt(20f));

            Assert.That(slider.Value, Is.EqualTo(2f / 3f).Within(1e-5f), "20 is 60% across: 1.8 of 3 snaps to 2");
        }

        [Test]
        public void ANonInteractableSlider_IgnoresThePointer()
        {
            var slider = Build(interactable: false);

            slider.OnDrag(PointerAt(100f));

            Assert.That(slider.Value, Is.Zero);
        }

        [Test]
        public void ARightClickDrag_IsIgnored()
        {
            var slider = Build();
            var rightClick = new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Right, position = new Vector2(100f, 0f) };

            slider.OnDrag(rightClick);

            Assert.That(slider.Value, Is.Zero);
        }

        [Test]
        public void TheFillMask_ClipsFromTheRightToTheCurrentValue()
        {
            var slider = Build(withMask: true);
            var mask = slider.GetComponentInChildren<RectMask2D>();

            slider.SetValue(.25f);

            Assert.That(mask.padding.z, Is.EqualTo(150f).Within(1e-3f));
            Assert.That(mask.padding.x, Is.Zero, "only the right edge moves");
        }

        [Test]
        public void TheHandle_IsAnchoredAtTheCurrentValue()
        {
            var slider = Build(withHandle: true);
            var handle = (RectTransform)slider.transform.Find("handle");

            slider.SetValue(.25f);

            Assert.That(handle.anchorMin.x, Is.EqualTo(.25f).Within(1e-5f));
            Assert.That(handle.anchorMax.x, Is.EqualTo(.25f).Within(1e-5f));
        }

        [Test]
        public void TheSlideArea_IsInsetByHalfTheHandleWidthOnEachSide()
        {
            var slider = Build(withSlideArea: true, withHandle: true);
            var area = (RectTransform)slider.transform.Find("slide-area");

            Assert.That(area.offsetMin.x, Is.EqualTo(HandleWidth / 2f).Within(1e-3f));
            Assert.That(area.offsetMax.x, Is.EqualTo(-HandleWidth / 2f).Within(1e-3f));
        }

        [Test]
        public void TheSlideArea_FollowsTheHandlesPivot()
        {
            var slider = Build(withSlideArea: true, withHandle: true);
            var area = (RectTransform)slider.transform.Find("slide-area");
            var handle = (RectTransform)area.Find("handle");

            handle.pivot = new Vector2(0f, .5f);
            slider.Refresh();

            Assert.That(area.offsetMin.x, Is.Zero.Within(1e-3f), "a left-pivoted handle overhangs only to the right");
            Assert.That(area.offsetMax.x, Is.EqualTo(-HandleWidth).Within(1e-3f));
        }

        [Test]
        public void WithAnInsetSlideArea_ThePointerMapsOntoTheInsetSpan()
        {
            var slider = Build(withSlideArea: true, withHandle: true);

            slider.OnDrag(PointerAt(-90f));
            Assert.That(slider.Value, Is.Zero.Within(1e-3f), "the inset edge is empty, not the root's edge");

            slider.OnDrag(PointerAt(90f));
            Assert.That(slider.Value, Is.EqualTo(1f).Within(1e-3f));

            slider.OnDrag(PointerAt(-100f));
            Assert.That(slider.Value, Is.Zero, "the padding outside the slide area still counts as the hit area and clamps");
        }

        [Test]
        public void TheFill_EndsUnderTheHandlesCentre_NotAtTheMasksEdge()
        {
            var slider = Build(withMask: true, withSlideArea: true, withHandle: true);
            var mask = slider.GetComponentInChildren<RectMask2D>();

            slider.SetValue(1f);
            Assert.That(mask.padding.z, Is.EqualTo(HandleWidth / 2f).Within(1e-3f), "full: the fill stops at the handle centre, 10 short of the edge");

            slider.SetValue(0f);
            Assert.That(mask.padding.z, Is.EqualTo(200f - HandleWidth / 2f).Within(1e-3f));
        }

        [Test]
        public void TheLabel_ShowsTheValueInTheAuthoredFormat()
        {
            var slider = Build(withLabel: true);
            var label = slider.GetComponentInChildren<TextMeshProUGUI>();
            Assert.That(label.text, Is.EqualTo("0 %"));

            slider.SetValue(.5f);
            Assert.That(label.text, Is.EqualTo("50 %"));

            slider.SetValueWithoutNotify(.25f);
            Assert.That(label.text, Is.EqualTo("25 %"));
        }

        [Test]
        public void TheDefaults_AreContinuousAndAPercentage()
        {
            var slider = scene.Element<ValueSlider>();

            Assert.That(slider.IsStepped, Is.False);
            Assert.That(slider.Value, Is.Zero);
            Assert.That(slider.FormattedValue, Is.EqualTo("0 %"));
        }

        [TestCase(false, .5f, "50 %")]
        [TestCase(false, .256f, "26 %")]
        [TestCase(true, .5f, "0.50")]
        [TestCase(true, .256f, "0.26")]
        public void TheReadout_IsAPercentOrAFloatingPointNumber(bool floatingPoint, float value, string expected)
        {
            var slider = Build(floatingPoint: floatingPoint);

            slider.SetValueWithoutNotify(value);

            Assert.That(slider.FormattedValue, Is.EqualTo(expected));
        }

        [Test]
        public void TheReadout_IgnoresTheCurrentCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            try
            {
                var slider = Build(floatingPoint: true);
                slider.SetValueWithoutNotify(.5f);

                Assert.That(slider.FormattedValue, Is.EqualTo("0.50"), "not 0,50");
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Test]
        public void ADerivedSlider_MapsTheValueBeforeFormatting()
        {
            var slider = scene.Element<SpyMappedValueSlider>();
            UiTestScene.SetBool(slider, "useFloatingPoint", true);

            slider.SetValueWithoutNotify(.5f);

            Assert.That(slider.FormattedValue, Is.EqualTo("5.00"));
        }

        [Test]
        public void ASliderWithNoChildren_DoesNotThrow()
        {
            var slider = Build();

            Assert.That(() => slider.SetValue(.5f), Throws.Nothing);
        }
    }
}
