using NUnit.Framework;
using System.Reflection;
using Submodules.Utility.Tests.TestSupport;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// The UI Submit (Enter on a selected button) presses a button exactly as a click does, so a
    /// popup's default button can be driven from the keyboard.
    /// </summary>
    [TestFixture]
    public sealed class AbstractButtonTests
    {
        private UiTestScene scene;

        private GameObject eventSystem;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            eventSystem = new GameObject("event-system", typeof(EventSystem));

            // EventSystem is not [ExecuteAlways], so it registers itself (and becomes current) only in Play Mode.
            _ = typeof(EventSystem).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(eventSystem.GetComponent<EventSystem>(), null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(eventSystem);
            scene.Dispose();
        }

        [Test]
        public void Submit_OnAnInteractableButton_DoesWhatAClickDoes()
        {
            var button = scene.Element<SpyButton>();

            button.OnSubmit(null);

            Assert.That(button.Clicks, Is.EqualTo(1));
        }

        [Test]
        public void Submit_OnANonInteractableButton_DoesNothing()
        {
            var button = scene.Element<SpyButton>(interactable: false);

            button.OnSubmit(null);

            Assert.That(button.Clicks, Is.Zero);
        }

        /// <summary>A button left selected after a click would press again on the next stray Enter.</summary>
        [Test]
        public void AfterAClick_TheButtonIsNoLongerSelected()
        {
            var button = scene.Element<SpyButton>();
            EventSystem.current.SetSelectedGameObject(button.gameObject);

            button.OnPointerUp(UiTestScene.LeftClick());

            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
        }
    }
}
