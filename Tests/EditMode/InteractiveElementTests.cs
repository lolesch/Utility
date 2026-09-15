using NUnit.Framework;
using Submodules.Utility.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// Pins the one thing <see cref="InteractiveElement"/> promises beyond stock
    /// <c>Selectable</c>: an element that is <c>interactable</c> receives pointer events,
    /// and one that is not, does not. The refactor bound
    /// <see cref="UnityEngine.UI.Graphic.raycastTarget"/> to <c>interactable</c>, so the two
    /// have to stay in step for the lifetime of the component — `MapPanel`,
    /// `MinimapPanel` and `StoreStashPhaseBinding` all flip <c>interactable</c> at runtime.
    ///
    /// Scale and colour feedback is deliberately not tested: <c>Scale</c> discards its tween
    /// handle and the tween pump is installed by <c>[RuntimeInitializeOnLoadMethod]</c>, so
    /// there is nothing an EditMode test can observe.
    /// </summary>
    [TestFixture]
    public sealed class InteractiveElementTests
    {
        private UiTestScene scene;

        [SetUp]
        public void SetUp() => scene = new UiTestScene();

        [TearDown]
        public void TearDown() => scene.Dispose();

        [Test]
        public void AnInteractableElement_IsARaycastTarget()
        {
            var element = scene.Element<InteractiveElement>(interactable: true);

            Assert.That(element.targetGraphic.raycastTarget, Is.True);
        }

        [Test]
        public void AnElementThatStartsNonInteractable_IsNotARaycastTarget()
        {
            var element = scene.Element<InteractiveElement>(interactable: false);

            Assert.That(element.targetGraphic.raycastTarget, Is.False);
        }

        [Test]
        public void TurningInteractableOnAfterAwake_MakesTheGraphicARaycastTargetAgain()
        {
            var element = scene.Element<InteractiveElement>(interactable: false);

            element.interactable = true;

            Assert.That(element.targetGraphic.raycastTarget, Is.True,
                "an element re-enabled at runtime has to receive pointer events again");
        }

        [Test]
        public void TurningInteractableOff_StopsTheGraphicBeingARaycastTarget()
        {
            var element = scene.Element<InteractiveElement>(interactable: true);

            element.interactable = false;

            Assert.That(element.targetGraphic.raycastTarget, Is.False,
                "a disabled element must not swallow pointer events meant for what is behind it");
        }

        [Test]
        public void PressingAnElementWithNoGraphic_DoesNotThrow()
        {
            // Awake logs MissingComponent as an error for this configuration and keeps
            // going — the element is expected to survive without a Graphic, not to be
            // considered broken.
            LogAssert.ignoreFailingMessages = true;

            var element = scene.Element<InteractiveElement>(withGraphic: false);

            Assert.That(() => element.OnPointerDown(UiTestScene.LeftClick()), Throws.Nothing);
        }
    }
}
