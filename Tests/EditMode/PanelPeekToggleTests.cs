using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Submodules.Utility.Tests.TestSupport;
using Submodules.Utility.UI;
using UnityEngine;
using ToggleGroup = Submodules.Utility.UI.ToggleGroup;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// A peek toggle is a tab that owns one bool and two panels (off shows A, on shows B), sitting beside
    /// another tab in a <see cref="ToggleGroup"/> that can never be empty. While its key is held the pair
    /// shows its other tab; letting go brings it back. The group does every switch, so a click, the group's
    /// reset and the peek are all one road, and the tests drive the key through <see cref="SpyPeekToggle"/>.
    ///
    /// Which panel is showing is read from the order record the panels share: the last thing a panel was
    /// told is what it is doing.
    /// </summary>
    [TestFixture]
    public sealed class PanelPeekToggleTests
    {
        private UiTestScene scene;
        private ToggleGroup group;
        private SpyPeekToggle driver;
        private SpyToggle other;
        private SpyPanel panelA;
        private SpyPanel panelB;
        private List<string> log;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            log = new List<string>();

            // The pair as authored: a group that can never be empty, the driver and the other tab
            // button beside it, the other tab first and on (the pair rests off, on the Supply).
            group = scene.Group(groupCanUntoggle: false);
            driver = scene.Element<SpyPeekToggle>(parent: group.transform);
            other = scene.Toggle(group);
            group.Activate(other);

            panelA = scene.Panel();
            panelB = scene.Panel();
            panelA.Label = "A";
            panelB.Label = "B";
            panelA.Log = log;
            panelB.Log = log;
            UiTestScene.SetObject(driver, "panelWhenOff", panelA);
            UiTestScene.SetObject(driver, "panelWhenOn", panelB);
            UiTestScene.SetObject(driver, "peekTarget", other);
        }

        [TearDown]
        public void TearDown() => scene.Dispose();

        private bool Showing(string label) => log.LastOrDefault(entry => entry.StartsWith(label)) == label + "+";

        private bool Hiding(string label) => log.LastOrDefault(entry => entry.StartsWith(label)) == label + "-";

        private void Hold()
        {
            driver.KeyHeld = true;
            driver.Tick();
        }

        private void Release()
        {
            driver.KeyHeld = false;
            driver.Tick();
        }

        /// <summary>The pair the other way round, as the Vendor and the Healer author it: the driver is the
        /// group's first member (the tab a panel opens on), the other button is the one it peeks to.</summary>
        private (ToggleGroup group, SpyPeekToggle driver, SpyToggle other) DriverFirstPair()
        {
            var swapped = scene.Group(groupCanUntoggle: false);
            var first = scene.Element<SpyPeekToggle>(parent: swapped.transform);
            var second = scene.Toggle(swapped);
            UiTestScene.SetObject(first, "panelWhenOff", panelA);
            UiTestScene.SetObject(first, "panelWhenOn", panelB);
            UiTestScene.SetObject(first, "peekTarget", second);
            swapped.Activate(first);

            return (swapped, first, second);
        }

        #region THE SWITCH

        [Test]
        public void Off_ShowsPanelA_AndHidesPanelB()
        {
            driver.SetToggle(true);

            other.SetToggle(true);

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
        public void TheDriverSwitchedOn_TurnsTheOtherTabOff()
        {
            driver.SetToggle(true);

            Assert.That(other.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.SameAs(driver));
        }

        [Test]
        public void AClickOnTheOtherTab_SwitchesTheDriverOff()
        {
            driver.SetToggle(true);

            other.SetToggle(true);

            Assert.That(driver.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.SameAs(other));
        }

        [Test]
        public void AClickOnTheDriverWhileItIsOn_IsRefused()
        {
            driver.SetToggle(true);

            driver.SetToggle(false);

            Assert.That(driver.IsOn, Is.True, "the group forbids switch-off");
            Assert.That(Showing("B"), Is.True);
        }

        [Test]
        public void TheTwoPanelsAreNeverBothShowing_OnAnySwitch()
        {
            for (var i = 0; i < 4; i++)
            {
                driver.SetToggle(true);
                Assert.That(Showing("B") && Hiding("A"), Is.True, $"B alone after switching on, round {i}");

                other.SetToggle(true);
                Assert.That(Showing("A") && Hiding("B"), Is.True, $"A alone after switching off, round {i}");
            }
        }

        [Test]
        public void AnotherToggleInTheGroupSwitchedOn_SwitchesTheDriverOff_AndShowsPanelA()
        {
            var third = scene.Toggle(group);
            driver.SetToggle(true);

            third.SetToggle(true);

            Assert.That(driver.IsOn, Is.False);
            Assert.That(group.ActiveMember, Is.SameAs(third));
            Assert.That(Showing("A"), Is.True);
        }

        /// <summary>Sibling panels in a <see cref="PanelGroup"/> that can never be empty switch like any
        /// two panels, in either direction: the group hands the slot over, whichever panel is addressed first.</summary>
        [Test]
        public void PanelsInAGroupThatCanNeverBeEmpty_StillSwitch_BothWays()
        {
            var panels = scene.PanelGroup(groupCanUntoggle: false);
            var inA = scene.Panel(panels);
            var inB = scene.Panel(panels);
            UiTestScene.SetObject(driver, "panelWhenOff", inA);
            UiTestScene.SetObject(driver, "panelWhenOn", inB);
            inA.Expand();

            driver.SetToggle(true);
            Assert.That(panels.ActiveMember, Is.SameAs(inB));

            other.SetToggle(true);
            Assert.That(panels.ActiveMember, Is.SameAs(inA));
        }

        /// <summary>The incoming panel is switched on first: a PanelGroup refuses to collapse its sole
        /// active panel, so collapsing the outgoing one first is a refused call in the log.</summary>
        [Test]
        public void SwitchingOff_WithPanelsInAGroup_NeverAsksTheGroupToCollapseItsActivePanel()
        {
            var panels = scene.PanelGroup(groupCanUntoggle: false);
            var inA = scene.Panel(panels);
            var inB = scene.Panel(panels);
            UiTestScene.SetObject(driver, "panelWhenOff", inA);
            UiTestScene.SetObject(driver, "panelWhenOn", inB);
            inA.Expand();
            driver.SetToggle(true);
            var refused = 0;

            void Count(string message, string stack, LogType type)
            {
                if (message.Contains("Collapse() prevented"))
                    refused++;
            }

            Application.logMessageReceived += Count;
            try
            {
                other.SetToggle(true);
            }
            finally
            {
                Application.logMessageReceived -= Count;
            }

            Assert.That(refused, Is.Zero);
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
        public void WithUnsetPanels_ASwitchDoesNotThrow()
        {
            UiTestScene.SetObject(driver, "panelWhenOff", null);
            UiTestScene.SetObject(driver, "panelWhenOn", null);

            Assert.That(() => driver.SetToggle(true), Throws.Nothing);
            Assert.That(() => other.SetToggle(true), Throws.Nothing);
        }

        /// <summary>The group's reset on its panel closing reaches the driver's own toggle callback exactly
        /// as a click does, so the driver needs no case for it. Driven directly - the Unity message does not
        /// run in EditMode.</summary>
        [Test]
        public void TheGroupsResetOnClosing_ReturnsThePairToItsFirstMember_ThroughTheDriver()
        {
            driver.SetToggle(true);
            var parent = scene.Panel();
            parent.Disappear(true);

            group.ResetWhenCollapsed(parent);

            Assert.That(driver.IsOn, Is.False);
            Assert.That(other.IsOn, Is.True);
            Assert.That(Showing("A"), Is.True);
            Assert.That(Hiding("B"), Is.True);
        }

        [Test]
        public void TheGroupsResetOnClosing_WithTheDriverFirst_ReturnsThePairToOn_ThroughTheDriver()
        {
            var pair = DriverFirstPair();
            pair.other.SetToggle(true);
            var parent = scene.Panel();
            parent.Disappear(true);

            pair.group.ResetWhenCollapsed(parent);

            Assert.That(pair.driver.IsOn, Is.True);
            Assert.That(pair.other.IsOn, Is.False);
            Assert.That(Showing("B"), Is.True);
            Assert.That(Hiding("A"), Is.True);
        }

        #endregion THE SWITCH

        #region THE PEEK

        [Test]
        public void Holding_FromTheOffState_ShowsTheDriversPanel_AndReleasingBringsTheSupplyBack()
        {
            Hold();

            Assert.That(driver.IsOn, Is.True);
            Assert.That(other.IsOn, Is.False, "both tab buttons follow the peek");
            Assert.That(Showing("B"), Is.True);

            Release();

            Assert.That(driver.IsOn, Is.False);
            Assert.That(other.IsOn, Is.True);
            Assert.That(Showing("A"), Is.True);
        }

        [Test]
        public void Holding_FromTheOnState_ShowsTheOtherTab_AndReleasingBringsTheDriverBack()
        {
            driver.SetToggle(true);

            Hold();

            Assert.That(driver.IsOn, Is.False);
            Assert.That(other.IsOn, Is.True, "both tab buttons follow the peek");
            Assert.That(Showing("A"), Is.True);

            Release();

            Assert.That(driver.IsOn, Is.True);
            Assert.That(Showing("B"), Is.True);
        }

        [Test]
        public void ADriverThatIsTheFirstMember_PeeksAndReturns_LikeAnyOther()
        {
            var pair = DriverFirstPair();

            pair.driver.KeyHeld = true;
            pair.driver.Tick();
            Assert.That(pair.other.IsOn, Is.True);

            pair.driver.KeyHeld = false;
            pair.driver.Tick();
            Assert.That(pair.driver.IsOn, Is.True);
            Assert.That(pair.other.IsOn, Is.False);
        }

        [Test]
        public void KeepingTheKeyDown_PeeksOnce_NotOnEveryFrame()
        {
            Hold();
            var shown = log.Count;

            driver.Tick();
            driver.Tick();

            Assert.That(log.Count, Is.EqualTo(shown));
            Assert.That(driver.IsOn, Is.True);
        }

        [Test]
        public void ReleasingWithNoPeekRunning_ChangesNothing()
        {
            Release();

            Assert.That(driver.IsOn, Is.False);
            Assert.That(other.IsOn, Is.True);
        }

        /// <summary>A click on the home tab moves the group on and so cancels the restore: the player stays
        /// where they clicked, and holding on does not start a second peek.</summary>
        [Test]
        public void HoldThenAClickOnTheHomeTab_ThenRelease_LeavesThePlayerOnTheHomeTab()
        {
            Hold();

            other.SetToggle(true);
            driver.Tick();
            Release();

            Assert.That(other.IsOn, Is.True);
            Assert.That(driver.IsOn, Is.False);
            Assert.That(Showing("A"), Is.True);
        }

        [Test]
        public void HoldThenAClickOnTheHomeTab_FromTheOnState_StaysOnTheDriver_AfterRelease()
        {
            driver.SetToggle(true);
            Hold();

            driver.SetToggle(true);
            Release();

            Assert.That(driver.IsOn, Is.True);
            Assert.That(other.IsOn, Is.False);
        }

        /// <summary>The tab being peeked at is already on, so a click on it is refused and changes nothing:
        /// on release the player is home.</summary>
        [Test]
        public void HoldThenAClickOnThePeekedTab_IsRefused_AndReleaseReturnsHome()
        {
            Hold();

            driver.SetToggle(false);
            driver.SetToggle(true);
            Release();

            Assert.That(other.IsOn, Is.True);
            Assert.That(driver.IsOn, Is.False);
            Assert.That(Showing("A"), Is.True);
        }

        [Test]
        public void HoldThenAThirdToggleSwitchedOn_ThenRelease_LeavesTheThirdToggle()
        {
            var third = scene.Toggle(group);
            Hold();

            third.SetToggle(true);
            Release();

            Assert.That(third.IsOn, Is.True);
            Assert.That(driver.IsOn, Is.False);
        }

        [Test]
        public void APeekFromAThirdToggle_ReturnsToIt()
        {
            var third = scene.Toggle(group);
            third.SetToggle(true);

            Hold();
            Assert.That(driver.IsOn, Is.True);

            Release();
            Assert.That(third.IsOn, Is.True);
            Assert.That(driver.IsOn, Is.False);
        }

        [Test]
        public void AHoldWithTheToggleNotInteractive_DoesNothing()
        {
            driver.interactable = false;

            Hold();

            Assert.That(driver.IsOn, Is.False);
            Assert.That(other.IsOn, Is.True);
        }

        /// <summary>The panel a toggle lives in takes its contents out of reach through its CanvasGroup, and
        /// the toggle reads that as <c>IsInteractable</c> with nothing of its own to check.</summary>
        [Test]
        public void AHoldWhileTheCanvasGroupAboveIsNotInteractable_DoesNothing()
        {
            var canvasGroup = group.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            Assert.That(driver.IsInteractable(), Is.False, "the CanvasGroup took the toggle out of reach");

            Hold();

            Assert.That(driver.IsOn, Is.False);
        }

        [Test]
        public void AHoldBegunBeforeThePanelOpens_PeeksAsSoonAsItIsReachable()
        {
            driver.interactable = false;
            Hold();
            Assert.That(driver.IsOn, Is.False);

            driver.interactable = true;
            driver.Tick();

            Assert.That(driver.IsOn, Is.True);
            Assert.That(other.IsOn, Is.False);
        }

        /// <summary>A begin that was refused (nothing to switch to) is not retried for the rest of the hold:
        /// a later click on the driver does not turn into a peek.</summary>
        [Test]
        public void ARefusedBegin_IsNotRetried_ForTheRestOfTheHold()
        {
            UiTestScene.SetObject(driver, "peekTarget", null);
            driver.SetToggle(true);
            Hold();

            UiTestScene.SetObject(driver, "peekTarget", other);
            driver.Tick();

            Assert.That(driver.IsOn, Is.True);
            Assert.That(other.IsOn, Is.False);
        }

        [Test]
        public void DisablingTheToggleDuringAHold_GivesThePeekBack()
        {
            Hold();

            driver.enabled = false;

            Assert.That(driver.IsOn, Is.False);
            Assert.That(other.IsOn, Is.True);
        }

        [Test]
        public void ThePanelClosingDuringAHold_GivesThePeekBackAtOnce()
        {
            Hold();

            driver.interactable = false;
            driver.Tick();

            Assert.That(driver.IsOn, Is.False);
            Assert.That(other.IsOn, Is.True);
        }

        /// <summary>A pair resting on the driver, hold, close the panel, the group resets to its first
        /// member, reopen and let go: the panel opens on the first member and stays there.</summary>
        [Test]
        public void HoldCloseTheGroupResetsReopenRelease_LeavesTheFirstMember()
        {
            driver.SetToggle(true);
            Hold();
            driver.interactable = false;
            driver.Tick();
            var parent = scene.Panel();
            parent.Disappear(true);
            group.ResetWhenCollapsed(parent);

            driver.interactable = true;
            driver.KeyHeld = false;
            driver.Tick();

            Assert.That(other.IsOn, Is.True);
            Assert.That(driver.IsOn, Is.False);
            Assert.That(Showing("A"), Is.True);
        }

        [Test]
        public void ReopeningWithTheKeyStillHeld_PeeksAgain_AndReleaseReturnsToTheFirstMember()
        {
            Hold();
            driver.interactable = false;
            driver.Tick();
            driver.interactable = true;

            driver.Tick();
            Assert.That(driver.IsOn, Is.True);

            Release();
            Assert.That(other.IsOn, Is.True);
        }

        [Test]
        public void WithNoPeekTarget_APeekFromTheOnStateIsNotPossible()
        {
            UiTestScene.SetObject(driver, "peekTarget", null);
            driver.SetToggle(true);

            Hold();

            Assert.That(driver.IsOn, Is.True);
            Assert.That(other.IsOn, Is.False);
        }

        [Test]
        public void WithAPeekTargetInAnotherGroup_APeekFromTheOnStateIsNotPossible()
        {
            var elsewhere = scene.Group(groupCanUntoggle: false);
            var stray = scene.Toggle(elsewhere);
            UiTestScene.SetObject(driver, "peekTarget", stray);
            driver.SetToggle(true);

            Hold();

            Assert.That(driver.IsOn, Is.True);
            Assert.That(stray.IsOn, Is.False);
        }

        #endregion THE PEEK

        #region AUTHORING

        [Test]
        public void AWellAuthoredPair_HasNoAuthoringProblem()
        {
            Assert.That(driver.AuthoringProblems(), Is.Empty);
        }

        [Test]
        public void ADriverWithNoGroup_IsWarned()
        {
            var alone = scene.Element<SpyPeekToggle>();

            Assert.That(alone.AuthoringProblems(), Has.Some.Contain("no ToggleGroup"));
        }

        /// <summary>A second pair in a group of its own, wired like the fixture's, for a test to break one rule of.</summary>
        private SpyPeekToggle PairInGroup(ToggleGroup loose)
        {
            var strayDriver = scene.Element<SpyPeekToggle>(parent: loose.transform);
            var strayOther = scene.Toggle(loose);
            UiTestScene.SetObject(strayDriver, "panelWhenOff", panelA);
            UiTestScene.SetObject(strayDriver, "panelWhenOn", panelB);
            UiTestScene.SetObject(strayDriver, "peekTarget", strayOther);

            return strayDriver;
        }

        [Test]
        public void AGroupThatAllowsSwitchOff_IsWarned()
        {
            var pair = PairInGroup(scene.Group(userCanUntoggle: true));

            Assert.That(pair.AuthoringProblems().Single(), Does.Contain("allows switch-off"));
        }

        [Test]
        public void AGroupWhoseGroupMayEmptyItself_IsWarned()
        {
            var pair = PairInGroup(scene.Group(groupCanUntoggle: true));

            Assert.That(pair.AuthoringProblems().Single(), Does.Contain("allows switch-off"));
        }

        [Test]
        public void AnUnsetPanel_IsWarned()
        {
            UiTestScene.SetObject(driver, "panelWhenOn", null);

            Assert.That(driver.AuthoringProblems().Single(), Does.Contain("panel is unset"));
        }

        [Test]
        public void TheSamePanelInBothSlots_IsWarned()
        {
            UiTestScene.SetObject(driver, "panelWhenOn", panelA);

            Assert.That(driver.AuthoringProblems().Single(), Does.Contain("same panel"));
        }

        [Test]
        public void AnUnsetPeekTarget_IsWarned()
        {
            UiTestScene.SetObject(driver, "peekTarget", null);

            Assert.That(driver.AuthoringProblems().Single(), Does.Contain("'peekTarget' is unset"));
        }

        [Test]
        public void APeekTargetInAnotherGroup_IsWarned()
        {
            var elsewhere = scene.Group(groupCanUntoggle: false);
            UiTestScene.SetObject(driver, "peekTarget", scene.Toggle(elsewhere));

            Assert.That(driver.AuthoringProblems().Single(), Does.Contain("not in the same ToggleGroup"));
        }

        [Test]
        public void ADriverAmongOtherToggles_IsNotWarned()
        {
            scene.Toggle(group);

            Assert.That(driver.AuthoringProblems(), Is.Empty);
        }

        #endregion AUTHORING
    }
}
