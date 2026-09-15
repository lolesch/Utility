using NUnit.Framework;
using Submodules.Utility.UI;
using Submodules.Utility.Tests.TestSupport;
using UnityEngine.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="Submodules.Utility.UI.AbstractToggle"/> from the
    /// outside: <c>SetToggle</c> (what code calls) and <c>OnPointerClick</c> (what a player
    /// does). What is pinned is the state that leaves the toggle — <c>IsOn</c>, the group it
    /// reports to, and that its one extension point <c>OnToggle</c> runs exactly once per
    /// change. Scale and colour feedback are not observable in EditMode and are left alone.
    /// </summary>
    [TestFixture]
    public sealed class AbstractToggleTests
    {
        private UiTestScene scene;

        [SetUp]
        public void SetUp() => scene = new UiTestScene();

        [TearDown]
        public void TearDown() => scene.Dispose();

        [Test]
        public void SetToggle_True_TurnsItOn_AndRunsOnToggleOnce()
        {
            var toggle = scene.Toggle();
            var before = toggle.OnToggleCalls;

            toggle.SetToggle(true);

            Assert.That(toggle.IsOn, Is.True);
            Assert.That(toggle.OnToggleCalls - before, Is.EqualTo(1));
            Assert.That(toggle.ObservedStates, Does.Contain(true));
        }

        [Test]
        public void SetToggle_False_TurnsItOff_AndRunsOnToggleOnce()
        {
            var toggle = scene.Toggle();
            toggle.SetToggle(true);
            var before = toggle.OnToggleCalls;

            toggle.SetToggle(false);

            Assert.That(toggle.IsOn, Is.False);
            Assert.That(toggle.OnToggleCalls - before, Is.EqualTo(1));
        }

        [Test]
        public void SetToggle_True_ReportsItselfToItsGroup()
        {
            var group = scene.Group();
            var toggle = scene.Toggle(group);

            toggle.SetToggle(true);

            Assert.That(group.ActivatedToggle, Is.SameAs(toggle));
        }

        [Test]
        public void SetToggle_False_WhenItWasTheActiveOne_EmptiesTheGroup()
        {
            var group = scene.Group();
            var toggle = scene.Toggle(group);
            toggle.SetToggle(true);

            toggle.SetToggle(false);

            Assert.That(group.ActivatedToggle, Is.Null);
        }

        [Test]
        public void ClickingAToggleThatIsOff_TurnsItOn()
        {
            var toggle = scene.Toggle();

            toggle.OnPointerClick(UiTestScene.LeftClick());

            Assert.That(toggle.IsOn, Is.True);
        }

        [Test]
        public void ClickingANonInteractableToggle_DoesNothing()
        {
            var toggle = scene.Toggle(interactable: false);

            toggle.OnPointerClick(UiTestScene.LeftClick());

            Assert.That(toggle.IsOn, Is.False);
        }

        [Test]
        public void ClickingTheActiveToggle_WhenTheGroupCannotBeEmptied_IsRefused()
        {
            var group = scene.Group(canDeactivateAll: false);
            var toggle = scene.Toggle(group);
            toggle.SetToggle(true);

            toggle.OnPointerClick(UiTestScene.LeftClick());

            Assert.That(toggle.IsOn, Is.True, "the group must keep exactly one selection");
            Assert.That(group.ActivatedToggle, Is.SameAs(toggle));
        }

        [Test]
        public void ClickingTheActiveToggle_WhenTheGroupCanBeEmptied_TurnsItOff()
        {
            var group = scene.Group(canDeactivateAll: true);
            var toggle = scene.Toggle(group);
            toggle.SetToggle(true);

            toggle.OnPointerClick(UiTestScene.LeftClick());

            Assert.That(toggle.IsOn, Is.False);
            Assert.That(group.ActivatedToggle, Is.Null);
        }

        [Test]
        public void SetToggle_SwapsTheSprite_WhenTheGraphicIsAnImage()
        {
            var toggle = scene.Toggle();
            var off = scene.Sprite();
            var on = scene.Sprite();
            UiTestScene.SetObject(toggle, "toggledOffSprite", off);
            UiTestScene.SetObject(toggle, "toggledOnSprite", on);

            toggle.SetToggle(true);

            Assert.That(((Image)toggle.targetGraphic).sprite, Is.SameAs(on));
        }

        [Test]
        public void SetToggle_WithSpritesButANonImageGraphic_DoesNotThrow()
        {
            var toggle = scene.Toggle(graphicType: typeof(RawImage));
            UiTestScene.SetObject(toggle, "toggledOffSprite", scene.Sprite());
            UiTestScene.SetObject(toggle, "toggledOnSprite", scene.Sprite());

            Assert.That(() => toggle.SetToggle(true), Throws.Nothing,
                "nothing in the inspector forbids sprites on a non-Image graphic, and "
                + "Start calls SetToggle before anything else gets a chance to run");
        }
    }
}
