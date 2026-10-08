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
    /// (off shows A, on shows B), beside a mirror toggle in the same group that can never be
    /// empty: a click on either tab button flips both. The group is the one mirror of the bool, and
    /// does the switching off of whichever member was on - so other toggles may share it. The fixture
    /// authors the mirror as the group's first member (the pair rests off); <c>DriverFirstPair</c> authors
    /// it the other way, driver first (the pair rests on), which is just as valid.
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
        private TwoPanelMirrorToggle partner;
        private TwoPanelToggle driver;
        private SpyPanel panelA;
        private SpyPanel panelB;
        private List<string> log;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            log = new List<string>();

            // The pair as authored: a group that can never be empty, the mirror (the off-state
            // toggle) first and on, the driver beside it.
            group = scene.Group(groupCanUntoggle: false);
            driver = scene.Element<TwoPanelToggle>(parent: group.transform);
            partner = scene.Element<TwoPanelMirrorToggle>(parent: group.transform);
            UiTestScene.SetObject(partner, "driver", driver);
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

        /// <summary>A second pair in its own group, wired like the fixture's (mirror on and first),
        /// for a test to break one rule of.</summary>
        private (ToggleGroup group, TwoPanelMirrorToggle mirror, TwoPanelToggle other) AnotherPair(
            bool userCanUntoggle = false, bool groupCanUntoggle = false)
        {
            var other = scene.Group(userCanUntoggle, groupCanUntoggle);
            var twin = scene.Element<TwoPanelToggle>(parent: other.transform);
            var mirror = scene.Element<TwoPanelMirrorToggle>(parent: other.transform);
            UiTestScene.SetObject(twin, "panelWhenOff", panelA);
            UiTestScene.SetObject(twin, "panelWhenOn", panelB);
            UiTestScene.SetObject(mirror, "driver", twin);
            other.Activate(mirror);

            return (other, mirror, twin);
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
        public void TheDriverSwitchedOn_TurnsTheMirrorOff()
        {
            driver.SetToggle(true);

            Assert.That(partner.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.SameAs(driver));
        }

        [Test]
        public void AClickOnTheMirror_SwitchesTheDriverOff()
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

        [Test]
        public void AClickOnTheMirrorWhileItIsOn_IsRefused()
        {
            var writes = driver.Writes;

            partner.SetToggle(false);

            Assert.That(partner.IsOn, Is.True, "the group forbids switch-off");
            Assert.That(group.ActiveMember, Is.SameAs(partner));
            Assert.That(driver.Writes, Is.EqualTo(writes), "a refused click is not a write");
        }

        [Test]
        public void AnotherToggleInTheGroupSwitchedOn_SwitchesTheDriverOff_AndShowsPanelA()
        {
            var other = scene.Toggle(group);
            driver.SetToggle(true);

            other.SetToggle(true);

            Assert.That(driver.IsOn, Is.False);
            Assert.That(partner.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.SameAs(other));
            Assert.That(Showing("A"), Is.True);
            Assert.That(Hiding("B"), Is.True);
        }

        [Test]
        public void TheDriverSwitchedOn_TurnsAnotherToggleInTheGroupOff()
        {
            var other = scene.Toggle(group);
            other.SetToggle(true);

            driver.SetToggle(true);

            Assert.That(other.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.SameAs(driver));
            Assert.That(Showing("B"), Is.True);
        }

        /// <summary>A peek switches the bool off from the group's side: the group goes home to the
        /// mirror instead of emptying, even though the user's own click on the driver is refused.</summary>
        [Test]
        public void SyncToggleOff_FromTheGroupsSide_GoesHomeToTheMirror()
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

        /// <summary>The group hands the slot over by fading the sibling out itself; the switch must not
        /// fade it a second time, which would restart its fade-out.</summary>
        [Test]
        public void SiblingPanelsInAGroup_AreEachFadedOutOnce_OnASwitch()
        {
            var panels = scene.PanelGroup(groupCanUntoggle: false);
            var inA = scene.Panel(panels);
            var inB = scene.Panel(panels);
            UiTestScene.SetObject(driver, "panelWhenOff", inA);
            UiTestScene.SetObject(driver, "panelWhenOn", inB);
            inA.Expand();
            var fadedOut = inA.FadeOutCalls;

            driver.SetToggle(true);

            Assert.That(inA.FadeOutCalls, Is.EqualTo(fadedOut + 1));
        }

        [Test]
        public void PanelsInDifferentGroups_AreStillBothSwitched()
        {
            var groupA = scene.PanelGroup(userCanUntoggle: true);
            var groupB = scene.PanelGroup();
            var inA = scene.Panel(groupA);
            var inB = scene.Panel(groupB);
            UiTestScene.SetObject(driver, "panelWhenOff", inA);
            UiTestScene.SetObject(driver, "panelWhenOn", inB);
            inA.Expand();

            driver.SetToggle(true);

            Assert.That(groupB.ActiveMember, Is.SameAs(inB));
            Assert.That(groupA.ActiveMember, Is.Null);
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

        /// <summary>The group's home is whatever member it authored first, so the pair need not be
        /// alone in it: any member but the driver is a home that switches the driver off.</summary>
        [Test]
        public void TheGroupsResetToAnotherFirstMember_StillSwitchesTheDriverOff()
        {
            var home = scene.Group(groupCanUntoggle: false);
            var tab = scene.Toggle(home);
            var other = scene.Element<TwoPanelToggle>(parent: home.transform);
            UiTestScene.SetObject(other, "panelWhenOff", panelA);
            UiTestScene.SetObject(other, "panelWhenOn", panelB);
            home.Activate(tab);
            other.SetToggle(true);
            var parent = scene.Panel();
            parent.Disappear(true);

            home.ResetWhenCollapsed(parent);

            Assert.That(other.IsOn, Is.False);
            Assert.That(tab.IsOn, Is.True);
            Assert.That(Showing("A"), Is.True);
            Assert.That(other.AuthoringProblems(), Is.Empty);
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

        /// <summary>The pair the other way round, as the Vendor and the Healer author it: the driver is
        /// the group's first member (the tab a panel opens on) and the mirror is the other button.</summary>
        private (ToggleGroup group, TwoPanelToggle driver, TwoPanelMirrorToggle mirror) DriverFirstPair()
        {
            var swapped = scene.Group(groupCanUntoggle: false);
            var first = scene.Element<TwoPanelToggle>(parent: swapped.transform);
            var second = scene.Element<TwoPanelMirrorToggle>(parent: swapped.transform);
            UiTestScene.SetObject(first, "panelWhenOff", panelA);
            UiTestScene.SetObject(first, "panelWhenOn", panelB);
            UiTestScene.SetObject(second, "driver", first);
            swapped.Activate(first);

            return (swapped, first, second);
        }

        [Test]
        public void ADriverThatIsTheFirstMember_WithItsMirror_IsNotWarned()
        {
            var pair = DriverFirstPair();

            Assert.That(pair.driver.AuthoringProblems(), Is.Empty);
            Assert.That(pair.mirror.AuthoringProblems(), Is.Empty);
        }

        /// <summary>The driver does not look for its mirror, so a missing or misplaced one is the mirror's
        /// to warn about (an unset driver, another group), not the driver's.</summary>
        [Test]
        public void ADriverThatIsTheFirstMember_ChecksNothingAboutItsMirror()
        {
            var pair = DriverFirstPair();
            UiTestScene.SetObject(pair.mirror, "driver", null);

            Assert.That(pair.driver.AuthoringProblems(), Is.Empty);
            Assert.That(pair.mirror.AuthoringProblems().Single(), Does.Contain("driver is unset"));
        }

        [Test]
        public void ADriverThatIsNotTheFirstMember_NeedsNoMirror()
        {
            var home = scene.Group(groupCanUntoggle: false);
            var tab = scene.Toggle(home);
            var other = scene.Element<TwoPanelToggle>(parent: home.transform);
            UiTestScene.SetObject(other, "panelWhenOff", panelA);
            UiTestScene.SetObject(other, "panelWhenOn", panelB);
            home.Activate(tab);

            Assert.That(other.AuthoringProblems(), Is.Empty);
        }

        /// <summary>The group's home is the driver, so the reset switches it on - a write like a click -
        /// and a click on the mirror, the only way off, switches it off again.</summary>
        [Test]
        public void TheGroupsResetOnClosing_WithTheDriverFirst_ReturnsThePairToOn_ThroughTheDriver()
        {
            var pair = DriverFirstPair();
            pair.mirror.SetToggle(true);
            var writes = pair.driver.Writes;
            var parent = scene.Panel();
            parent.Disappear(true);

            pair.group.ResetWhenCollapsed(parent);

            Assert.That(pair.driver.IsOn, Is.True);
            Assert.That(pair.mirror.IsOn, Is.False);
            Assert.That(pair.driver.Writes, Is.EqualTo(writes + 1), "the reset is a write like a click");
            Assert.That(Showing("B"), Is.True);
            Assert.That(Hiding("A"), Is.True);
        }

        [Test]
        public void TheGroupsSideWrite_OffOnTheFirstMemberDriver_GoesToTheMirror()
        {
            var pair = DriverFirstPair();
            var writes = pair.driver.Writes;

            ((ITwoPanelDriver)pair.driver).SetFromGroup(false);

            Assert.That(pair.driver.IsOn, Is.False);
            Assert.That(pair.mirror.IsOn, Is.True);
            Assert.That(pair.driver.Writes, Is.EqualTo(writes + 1));
            Assert.That(Showing("A"), Is.True);
        }

        /// <summary>The switch-off goes through the group, which knows its own members: nothing looks for
        /// the mirror, so it does not matter whether the other button names the driver.</summary>
        [Test]
        public void TheGroupsSideWrite_OffOnTheFirstMemberDriver_DoesNotNeedTheOtherButtonToNameIt()
        {
            var pair = DriverFirstPair();
            UiTestScene.SetObject(pair.mirror, "driver", null);

            ((ITwoPanelDriver)pair.driver).SetFromGroup(false);

            Assert.That(pair.driver.IsOn, Is.False);
            Assert.That(pair.mirror.IsOn, Is.True);
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

        /// <summary>A pair whose group is authored on with its driver and nothing else: no first member
        /// has been recorded (that happens in the group's <c>Awake</c> or on a first
        /// <c>Activate</c>), only the active one. The group's own rule is that the active member is
        /// home until told otherwise, and the driver reads it the same way everywhere.</summary>
        private (ToggleGroup group, TwoPanelToggle driver, TwoPanelMirrorToggle mirror) UnrecordedFirstMemberPair(
            bool withMirror)
        {
            var fresh = scene.Group(groupCanUntoggle: false);
            var first = scene.Element<TwoPanelToggle>(parent: fresh.transform);
            var second = scene.Element<TwoPanelMirrorToggle>(parent: fresh.transform);
            UiTestScene.SetObject(first, "panelWhenOff", panelA);
            UiTestScene.SetObject(first, "panelWhenOn", panelB);

            if (withMirror)
                UiTestScene.SetObject(second, "driver", first);

            UiTestScene.SetObject(fresh, "<ActiveMember>k__BackingField", first);
            UiTestScene.SetBool(first, "<IsOn>k__BackingField", true);

            return (fresh, first, second);
        }

        [Test]
        public void WithTheFirstMemberUnrecorded_TheActiveDriverIsTakenForIt_ByTheSideWrite()
        {
            var pair = UnrecordedFirstMemberPair(withMirror: true);
            var writes = pair.driver.Writes;

            Assert.That((bool)pair.group.FirstMember, Is.False, "the edge under test: nothing recorded");

            ((ITwoPanelDriver)pair.driver).SetFromGroup(false);

            Assert.That(pair.driver.IsOn, Is.False);
            Assert.That(pair.mirror.IsOn, Is.True, "the pair went off through its mirror");
            Assert.That(pair.driver.Writes, Is.EqualTo(writes + 1));
        }

        [Test]
        public void WithTheFirstMemberUnrecorded_TheActiveDriverIsTakenForIt_ByTheWarning()
        {
            var pair = UnrecordedFirstMemberPair(withMirror: true);

            Assert.That(pair.driver.AuthoringProblems(), Is.Empty, "a first member is taken to exist");
        }

        /// <summary>Returning home on closing is the group's opt-in (<c>ResetWithParentPanel</c>); a group
        /// without it simply keeps the player's tab, which is a choice and not a mistake.</summary>
        [Test]
        public void AGroupWithoutResetWithParentPanel_IsNotWarned_WhicheverButtonIsFirst()
        {
            var driverFirst = DriverFirstPair();

            Assert.That(group.ResetWithParentPanel, Is.False);
            Assert.That(driver.AuthoringProblems(), Is.Empty);
            Assert.That(driverFirst.group.ResetWithParentPanel, Is.False);
            Assert.That(driverFirst.driver.AuthoringProblems(), Is.Empty);
            Assert.That(driverFirst.mirror.AuthoringProblems(), Is.Empty);
        }

        /// <summary>The group's reset is "back to the first member", and a pair resting on its driver that
        /// is already there is not moved: a no-op, not a write that would restart a fade.</summary>
        [Test]
        public void TheGroupsResetWithTheDriverFirstAndAlreadyOn_WritesNothing()
        {
            var pair = DriverFirstPair();
            pair.driver.SetToggle(true);
            var writes = pair.driver.Writes;
            var parent = scene.Panel();
            parent.Disappear(true);

            pair.group.ResetWhenCollapsed(parent);

            Assert.That(pair.driver.IsOn, Is.True);
            Assert.That(pair.mirror.IsOn, Is.False);
            Assert.That(pair.driver.Writes, Is.EqualTo(writes));
        }

        [Test]
        public void ADriverAmongOtherToggles_IsNotWarned()
        {
            var pair = AnotherPair();
            scene.Toggle(pair.group);

            Assert.That(pair.other.AuthoringProblems(), Is.Empty);
        }

        [Test]
        public void AMirrorWithItsDriverInTheSameGroup_HasNoAuthoringProblem()
        {
            Assert.That(partner.AuthoringProblems(), Is.Empty);
        }

        [Test]
        public void AMirrorWithNoDriver_IsWarned()
        {
            var loose = scene.Element<TwoPanelMirrorToggle>(parent: group.transform);

            Assert.That(loose.AuthoringProblems().Single(), Does.Contain("driver is unset"));
        }

        [Test]
        public void AMirrorInAnotherGroupThanItsDriver_IsWarned()
        {
            var elsewhere = scene.Group(groupCanUntoggle: false);
            var stray = scene.Element<TwoPanelMirrorToggle>(parent: elsewhere.transform);
            UiTestScene.SetObject(stray, "driver", driver);

            Assert.That(stray.AuthoringProblems().Single(), Does.Contain("not in the same ToggleGroup"));
        }

        [Test]
        public void AMirrorWithNoGroup_IsWarned()
        {
            var alone = scene.Element<TwoPanelMirrorToggle>();
            UiTestScene.SetObject(alone, "driver", driver);

            Assert.That(alone.AuthoringProblems().Single(), Does.Contain("not in the same ToggleGroup"));
        }

        [Test]
        public void TheSamePanelInBothSlots_IsWarned()
        {
            UiTestScene.SetObject(driver, "panelWhenOn", panelA);

            Assert.That(driver.AuthoringProblems().Single(), Does.Contain("same panel"));
        }

        /// <summary>Authored in one group, a mirror and its driver cannot both be on (the group keeps one);
        /// across two groups they can, and the pair would have both tab buttons selected.</summary>
        [Test]
        public void AMirrorAndADriverBothAuthoredOn_AreWarned()
        {
            var elsewhere = scene.Group(groupCanUntoggle: false);
            var stray = scene.Element<TwoPanelMirrorToggle>(parent: elsewhere.transform);
            UiTestScene.SetObject(stray, "driver", driver);
            UiTestScene.SetBool(stray, "<IsOn>k__BackingField", true);
            UiTestScene.SetBool(driver, "<IsOn>k__BackingField", true);

            Assert.That(stray.AuthoringProblems(), Has.Some.Contain("both authored on"));
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
