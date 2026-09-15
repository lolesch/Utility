using NUnit.Framework;
using Submodules.Utility.UI;
using UnityEngine;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="MultiplePanelToggle"/> fans one toggle out to N panels — <c>FadeIn</c> on
    /// one list, <c>FadeOut</c> on the other, flipped when the toggle turns off — plus, per
    /// issue #30, up to N sibling <see cref="RadioGroup"/>s it clears while it is on and
    /// restores when it turns back off. Driven through <see cref="AbstractToggle.SetToggle"/>
    /// (what code calls), observed through <see cref="Submodules.Utility.Tests.TestSupport.SpyPanel"/>'s
    /// fade counters and the groups' own <see cref="RadioGroup.ActivatedToggle"/> /
    /// <see cref="RadioGroup.PreviouslyActivatedToggle"/> state.
    /// </summary>
    [TestFixture]
    public sealed class MultiplePanelToggleTests
    {
        private UiTestScene scene;
        private MultiplePanelToggle toggle;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            toggle = scene.Element<MultiplePanelToggle>();
        }

        [TearDown]
        public void TearDown() => scene.Dispose();

        [Test]
        public void SetToggle_True_FadesInEveryPanelInPanelsToTurnOn()
        {
            var a = scene.Panel();
            var b = scene.Panel();
            UiTestScene.SetObjectList(toggle, "panelsToTurnOn", new Object[] { a, b });

            toggle.SetToggle(true);

            Assert.That(a.FadeInCalls, Is.EqualTo(1));
            Assert.That(b.FadeInCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetToggle_True_FadesOutEveryPanelInPanelsToTurnOff()
        {
            var a = scene.Panel();
            UiTestScene.SetObjectList(toggle, "panelsToTurnOff", new Object[] { a });

            toggle.SetToggle(true);

            Assert.That(a.FadeOutCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetToggle_False_FadesOutEveryPanelInPanelsToTurnOn()
        {
            var a = scene.Panel();
            UiTestScene.SetObjectList(toggle, "panelsToTurnOn", new Object[] { a });

            toggle.SetToggle(false);

            Assert.That(a.FadeOutCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetToggle_False_FadesInEveryPanelInPanelsToTurnOff()
        {
            var a = scene.Panel();
            UiTestScene.SetObjectList(toggle, "panelsToTurnOff", new Object[] { a });

            toggle.SetToggle(false);

            Assert.That(a.FadeInCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetToggle_True_TurnsOffTheActiveToggleInEveryGroupToTurnOff()
        {
            var group = scene.Group();
            var active = scene.Toggle(group);
            active.SetToggle(true);
            UiTestScene.SetObjectList(toggle, "groupsToTurnOff", new Object[] { group });

            toggle.SetToggle(true);

            Assert.That(active.IsOn, Is.False);
            Assert.That(group.ActivatedToggle, Is.Null);
        }

        [Test]
        public void SetToggle_True_WithNoActiveToggleInAGroupToTurnOff_DoesNotThrow()
        {
            var group = scene.Group();
            UiTestScene.SetObjectList(toggle, "groupsToTurnOff", new Object[] { group });

            Assert.That(() => toggle.SetToggle(true), Throws.Nothing);
        }

        [Test]
        public void SetToggle_False_RestoresThePreviouslyActivatedToggleInEveryGroupToTurnOff()
        {
            var group = scene.Group();
            var previous = scene.Toggle(group);
            previous.SetToggle(true);
            previous.SetToggle(false); // group now remembers `previous` as PreviouslyActivatedToggle
            UiTestScene.SetObjectList(toggle, "groupsToTurnOff", new Object[] { group });

            toggle.SetToggle(false);

            Assert.That(previous.IsOn, Is.True);
            Assert.That(group.ActivatedToggle, Is.SameAs(previous));
        }

        [Test]
        public void SetToggle_False_WithNoPreviouslyActivatedToggleInAGroupToTurnOff_DoesNotThrow()
        {
            var group = scene.Group();
            UiTestScene.SetObjectList(toggle, "groupsToTurnOff", new Object[] { group });

            Assert.That(() => toggle.SetToggle(false), Throws.Nothing);
        }

        [Test]
        public void NullEntriesInAnyList_AreSkippedWithoutThrowing()
        {
            UiTestScene.SetObjectList(toggle, "panelsToTurnOn", new Object[] { null });
            UiTestScene.SetObjectList(toggle, "panelsToTurnOff", new Object[] { null });
            UiTestScene.SetObjectList(toggle, "groupsToTurnOff", new Object[] { null });

            Assert.That(() => toggle.SetToggle(true), Throws.Nothing);
        }
    }
}
