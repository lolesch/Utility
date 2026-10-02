using System.Text.RegularExpressions;
using NUnit.Framework;
using Submodules.Utility.UI;
using Submodules.Utility.Tests.TestSupport;
using UnityEngine;
using UnityEngine.TestTools;
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

            Assert.That(group.ActiveMember, Is.SameAs(toggle));
        }

        [Test]
        public void SetToggle_False_WhenItWasTheActiveOne_EmptiesTheGroup()
        {
            var group = scene.Group(userCanUntoggle: true);
            var toggle = scene.Toggle(group);
            toggle.SetToggle(true);

            toggle.SetToggle(false);

            Assert.That(group.ActiveMember, Is.Null);
        }

        /// <summary><see cref="AbstractToggle.SetToggle"/>'s refusal guard.
        ///
        /// <para>The toggle staying on is <em>not</em> enough to pin this — delete the guard and
        /// <see cref="AbstractGroup{TMember}.Deactivate"/> no-ops on exactly the same
        /// configuration, so the outcome is identical (verified by removing it: the suite stayed
        /// green). The guard's one unique effect is the author-facing log naming the two settings
        /// that would allow the un-toggle, so that is what this expects — same reasoning as
        /// <c>PanelGroupTests.FadeOut_OnTheActivePanel_NotClearable_IsRefused</c>.</para></summary>
        [Test]
        public void SetToggle_False_WhenItWasTheActiveOne_NotClearable_IsRefused()
        {
            var group = scene.Group();
            var toggle = scene.Toggle(group);
            toggle.SetToggle(true);
            LogAssert.Expect(LogType.Log, new Regex(@"^SetToggle\(false\) prevented\."));

            toggle.SetToggle(false);

            Assert.That(toggle.IsOn, Is.True);
            Assert.That(group.ActiveMember, Is.SameAs(toggle));
        }

        /// <summary><see cref="AbstractToggle.SyncToggle"/> is the group side of
        /// <see cref="AbstractToggle.SetToggle"/>: a toggle mirroring state owned elsewhere can
        /// always follow it off, in a group where the user may not click it off.</summary>
        [Test]
        public void SyncToggle_False_WhenItWasTheActiveOne_FollowsTheStateOff_WhereSetToggleIsRefused()
        {
            var group = scene.Group();
            var toggle = scene.Toggle(group);
            toggle.SetToggle(true);

            toggle.SyncToggle(false);

            Assert.That(toggle.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.Null);
        }

        /// <summary>What <c>DoStateTransition</c> keys on to keep a refused un-toggle from
        /// playing its pressed feedback: on, active in its group, and the user may not untoggle.</summary>
        [Test]
        public void IsLockedOn_OnlyForTheActiveToggleOfAGroupTheUserCannotUntoggle()
        {
            var locked = scene.Group();
            var free = scene.Group(userCanUntoggle: true);
            var inLocked = scene.Toggle(locked);
            var inFree = scene.Toggle(free);
            var loose = scene.Toggle();
            inLocked.SetToggle(true);
            inFree.SetToggle(true);
            loose.SetToggle(true);

            Assert.That(inLocked.IsLockedOn, Is.True);
            Assert.That(inFree.IsLockedOn, Is.False, "the user may click it off");
            Assert.That(loose.IsLockedOn, Is.False, "no group, nothing refuses it");
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

        /// <summary>The same refusal reached the way a player reaches it. Pinned on the log for
        /// the same reason as the <c>SetToggle</c> case above: without it, the guard could be
        /// deleted and this test would not notice.</summary>
        [Test]
        public void ClickingTheActiveToggle_WhenTheGroupCannotBeEmptied_IsRefused()
        {
            var group = scene.Group();
            var toggle = scene.Toggle(group);
            toggle.SetToggle(true);
            LogAssert.Expect(LogType.Log, new Regex(@"^SetToggle\(false\) prevented\."));

            toggle.OnPointerClick(UiTestScene.LeftClick());

            Assert.That(toggle.IsOn, Is.True, "the group must keep exactly one selection");
            Assert.That(group.ActiveMember, Is.SameAs(toggle));
        }

        [Test]
        public void ClickingTheActiveToggle_WhenTheGroupCanBeEmptied_TurnsItOff()
        {
            var group = scene.Group(userCanUntoggle: true);
            var toggle = scene.Toggle(group);
            toggle.SetToggle(true);

            toggle.OnPointerClick(UiTestScene.LeftClick());

            Assert.That(toggle.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.Null);
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
