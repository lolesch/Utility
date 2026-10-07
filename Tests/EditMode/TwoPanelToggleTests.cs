using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Submodules.Utility.Tests.TestSupport;
using Submodules.Utility.UI;
using ToggleGroup = Submodules.Utility.UI.ToggleGroup;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// A two-panel switch is a driver toggle owning one bool and exactly two panels following it
    /// (off shows A, on shows B), beside an inert partner in the same group that can never be
    /// empty: a click on either tab button flips both. The group is the one mirror of the bool.
    ///
    /// Which panel is showing is read from the order record the panels share: the last thing a
    /// panel was told is what it is doing. A stray <c>Start</c> (the toggle is <c>[ExecuteAlways]</c>)
    /// only re-states the current bool, so it cannot change that answer.
    /// </summary>
    [TestFixture]
    public sealed class TwoPanelToggleTests
    {
        private UiTestScene scene;
        private ToggleGroup group;
        private SpyToggle partner;
        private TwoPanelToggle driver;
        private SpyPanel panelA;
        private SpyPanel panelB;
        private List<string> log;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            log = new List<string>();

            // The pair as authored: a group that can never be empty, the inert partner (the off-state
            // toggle) first and on, the driver beside it.
            group = scene.Group(groupCanUntoggle: false);
            partner = scene.Toggle(group);
            driver = scene.Element<TwoPanelToggle>(parent: group.transform);
            group.Activate(partner);

            panelA = scene.Panel();
            panelB = scene.Panel();
            panelA.Label = "A";
            panelB.Label = "B";
            panelA.Log = log;
            panelB.Log = log;
            UiTestScene.SetObject(driver, "panelWhenOff", panelA);
            UiTestScene.SetObject(driver, "panelWhenOn", panelB);
        }

        [TearDown]
        public void TearDown() => scene.Dispose();

        /// <summary>A second pair in its own group, wired like the fixture's (inert partner on and
        /// first), for a test to break one rule of.</summary>
        private (ToggleGroup group, SpyToggle inert, TwoPanelToggle other) AnotherPair(bool userCanUntoggle = false,
            bool groupCanUntoggle = false)
        {
            var other = scene.Group(userCanUntoggle, groupCanUntoggle);
            var inert = scene.Toggle(other);
            var twin = scene.Element<TwoPanelToggle>(parent: other.transform);
            UiTestScene.SetObject(twin, "panelWhenOff", panelA);
            UiTestScene.SetObject(twin, "panelWhenOn", panelB);
            other.Activate(inert);

            return (other, inert, twin);
        }

        private bool Showing(string label) => log.LastOrDefault(entry => entry.StartsWith(label)) == label + "+";

        private bool Hiding(string label) => log.LastOrDefault(entry => entry.StartsWith(label)) == label + "-";

        [Test]
        public void Off_ShowsPanelA_AndHidesPanelB()
        {
            driver.SetToggle(true);

            partner.SetToggle(true);

            Assert.That(driver.IsOn, Is.False);
            Assert.That(Showing("A"), Is.True);
            Assert.That(Hiding("B"), Is.True);
        }

        [Test]
        public void On_ShowsPanelB_AndHidesPanelA()
        {
            driver.SetToggle(true);

            Assert.That(driver.IsOn, Is.True);
            Assert.That(Showing("B"), Is.True);
            Assert.That(Hiding("A"), Is.True);
        }

        [Test]
        public void TheDriverSwitchedOn_TurnsTheInertPartnerOff()
        {
            driver.SetToggle(true);

            Assert.That(partner.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.SameAs(driver));
        }

        [Test]
        public void AClickOnTheInertToggle_SwitchesTheDriverOff()
        {
            driver.SetToggle(true);

            partner.SetToggle(true);

            Assert.That(driver.IsOn, Is.False);
            Assert.That(partner.IsOn, Is.True);
            Assert.That(group.ActiveMember, Is.SameAs(partner));
        }

        [Test]
        public void AClickOnTheDriverWhileItIsOn_IsRefused()
        {
            driver.SetToggle(true);
            var writes = driver.Writes;

            driver.SetToggle(false);

            Assert.That(driver.IsOn, Is.True, "the group forbids switch-off");
            Assert.That(group.ActiveMember, Is.SameAs(driver));
            Assert.That(driver.Writes, Is.EqualTo(writes), "a refused click is not a write");
            Assert.That(Showing("B"), Is.True);
        }

        /// <summary>A peek switches the bool off from the group's side: the group goes home to the
        /// partner instead of emptying, even though the user's own click on the driver is refused.</summary>
        [Test]
        public void SyncToggleOff_FromTheGroupsSide_GoesHomeToThePartner()
        {
            driver.SetToggle(true);
            var writes = driver.Writes;

            driver.SyncToggle(false);

            Assert.That(driver.IsOn, Is.False);
            Assert.That(partner.IsOn, Is.True);
            Assert.That(driver.Writes, Is.EqualTo(writes + 1));
            Assert.That(Showing("A"), Is.True);
        }

        [Test]
        public void TheTwoPanelsAreNeverBothShowing_OnAnySwitch()
        {
            for (var i = 0; i < 4; i++)
            {
                driver.SetToggle(true);
                Assert.That(Showing("B") && Hiding("A"), Is.True, $"B alone after switching on, round {i}");

                partner.SetToggle(true);
                Assert.That(Showing("A") && Hiding("B"), Is.True, $"A alone after switching off, round {i}");
            }
        }

        /// <summary>Sibling panels in a <see cref="PanelGroup"/> that can never be empty switch like any
        /// two panels: the group hands the slot over, whichever panel the switch addresses first.</summary>
        [Test]
        public void PanelsInAGroupThatCanNeverBeEmpty_StillSwitch()
        {
            var panels = scene.PanelGroup(groupCanUntoggle: false);
            var inA = scene.Panel(panels);
            var inB = scene.Panel(panels);
            UiTestScene.SetObject(driver, "panelWhenOff", inA);
            UiTestScene.SetObject(driver, "panelWhenOn", inB);
            inA.Expand();

            driver.SetToggle(true);
            Assert.That(panels.ActiveMember, Is.SameAs(inB));

            partner.SetToggle(true);
            Assert.That(panels.ActiveMember, Is.SameAs(inA));
        }

        [Test]
        public void Writes_CountsEveryChangeOfTheBool_AClickAndTheGroupsOwn()
        {
            var writes = driver.Writes;

            driver.SetToggle(true);
            partner.SetToggle(true);

            Assert.That(driver.Writes, Is.EqualTo(writes + 2));
        }

        /// <summary>The group's reset on its panel closing writes the bool through the driver exactly as a
        /// click does: it reaches the driver's own toggle callback, so it is counted and it moves the
        /// panels. Driven directly - the Unity message does not run in EditMode.</summary>
        [Test]
        public void TheGroupsResetOnClosing_ReturnsThePairToOff_ThroughTheDriver()
        {
            driver.SetToggle(true);
            var writes = driver.Writes;
            var parent = scene.Panel();
            parent.Disappear(true);

            group.ResetWhenCollapsed(parent);

            Assert.That(driver.IsOn, Is.False);
            Assert.That(partner.IsOn, Is.True);
            Assert.That(driver.Writes, Is.EqualTo(writes + 1), "the reset is a write like a click");
            Assert.That(Showing("A"), Is.True);
            Assert.That(Hiding("B"), Is.True);
        }

        [Test]
        public void TheGroupsResetWhileThePairIsAlreadyOff_WritesNothing()
        {
            var writes = driver.Writes;
            var parent = scene.Panel();
            parent.Disappear(true);

            group.ResetWhenCollapsed(parent);

            Assert.That(driver.Writes, Is.EqualTo(writes));
        }

        [Test]
        public void WithUnsetPanels_ASwitchDoesNotThrow()
        {
            UiTestScene.SetObject(driver, "panelWhenOff", null);
            UiTestScene.SetObject(driver, "panelWhenOn", null);

            Assert.That(() => driver.SetToggle(true), Throws.Nothing);
            Assert.That(() => partner.SetToggle(true), Throws.Nothing);
        }

        [Test]
        public void AWellAuthoredPair_HasNoAuthoringProblem()
        {
            Assert.That(driver.AuthoringProblems(), Is.Empty);
        }

        [Test]
        public void ADriverWithNoGroup_IsWarned()
        {
            var alone = scene.Element<TwoPanelToggle>();

            Assert.That(alone.AuthoringProblems(), Has.Some.Contain("no ToggleGroup"));
        }

        [Test]
        public void AGroupThatAllowsSwitchOff_IsWarned()
        {
            var pair = AnotherPair(userCanUntoggle: true);

            Assert.That(pair.other.AuthoringProblems().Single(), Does.Contain("allows switch-off"));
        }

        [Test]
        public void AGroupWhoseGroupMayEmptyItself_IsWarned()
        {
            var pair = AnotherPair(groupCanUntoggle: true);

            Assert.That(pair.other.AuthoringProblems().Single(), Does.Contain("allows switch-off"));
        }

        [Test]
        public void AFirstMemberThatIsTheDriver_IsWarned()
        {
            var backwards = scene.Group(groupCanUntoggle: false);
            var other = scene.Element<TwoPanelToggle>(parent: backwards.transform);
            var inert = scene.Toggle(backwards);
            UiTestScene.SetObject(other, "panelWhenOff", panelA);
            UiTestScene.SetObject(other, "panelWhenOn", panelB);
            backwards.Activate(other);
            backwards.Activate(inert);

            Assert.That(other.AuthoringProblems().Single(), Does.Contain("first member"));
        }

        [Test]
        public void AGroupWithNoFirstMember_IsWarned()
        {
            var empty = scene.Group(groupCanUntoggle: false);
            var other = scene.Element<TwoPanelToggle>(parent: empty.transform);
            scene.Toggle(empty);
            UiTestScene.SetObject(other, "panelWhenOff", panelA);
            UiTestScene.SetObject(other, "panelWhenOn", panelB);

            Assert.That(other.AuthoringProblems().Single(), Does.Contain("first member"));
        }

        [Test]
        public void AThirdToggleInTheGroup_IsWarned()
        {
            var pair = AnotherPair();
            scene.Toggle(pair.group);

            Assert.That(pair.other.AuthoringProblems().Single(), Does.Contain("3 toggles"));
        }

        [Test]
        public void AMissingPartner_IsWarned()
        {
            var alone = scene.Group(groupCanUntoggle: false);
            var other = scene.Element<TwoPanelToggle>(parent: alone.transform);
            UiTestScene.SetObject(other, "panelWhenOff", panelA);
            UiTestScene.SetObject(other, "panelWhenOn", panelB);

            Assert.That(other.AuthoringProblems(), Has.Some.Contain("1 toggles"));
        }

        [Test]
        public void AnUnsetPanel_IsWarned_EvenWithNoGroup()
        {
            var alone = scene.Element<TwoPanelToggle>();

            Assert.That(alone.AuthoringProblems(), Has.Some.Contain("panel is unset"));
        }

        [Test]
        public void AnUnsetPanel_IsWarned()
        {
            UiTestScene.SetObject(driver, "panelWhenOn", null);

            Assert.That(driver.AuthoringProblems().Single(), Does.Contain("panel is unset"));
        }
    }
}
