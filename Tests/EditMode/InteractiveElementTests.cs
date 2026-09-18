using NUnit.Framework;
using Submodules.Utility.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// Pins the one thing <see cref="InteractiveElement"/> promises beyond stock
    /// <c>Selectable</c>: <see cref="UnityEngine.UI.Graphic.raycastTarget"/> is always on,
    /// regardless of <c>interactable</c> — a disabled element still blocks the raycast so
    /// clicks meant for it don't fall through to whatever sits behind it. It's on the
    /// individual button/toggle to no-op in its handler when <c>!interactable</c> (see
    /// <see cref="AbstractButton.OnPointerClick"/>).
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
        public void AnElementThatStartsNonInteractable_IsStillARaycastTarget()
        {
            var element = scene.Element<InteractiveElement>(interactable: false);

            Assert.That(element.targetGraphic.raycastTarget, Is.True,
                "a disabled element must still block the raycast so clicks don't fall through to what's behind it");
        }

        [Test]
        public void TurningInteractableOnAfterAwake_LeavesTheGraphicARaycastTarget()
        {
            var element = scene.Element<InteractiveElement>(interactable: false);

            element.interactable = true;

            Assert.That(element.targetGraphic.raycastTarget, Is.True);
        }

        [Test]
        public void TurningInteractableOff_LeavesTheGraphicARaycastTarget()
        {
            var element = scene.Element<InteractiveElement>(interactable: true);

            element.interactable = false;

            Assert.That(element.targetGraphic.raycastTarget, Is.True,
                "a disabled element must still block the raycast so clicks don't fall through to what's behind it");
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
