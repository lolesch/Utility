using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Submodules.Utility.Tests.TestSupport;
using Submodules.Utility.UI;
using ToggleGroup = Submodules.Utility.UI.ToggleGroup;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// A peek switches a two-panel pair to its other button while a key is held and puts it back on
    /// release, unless something else moved the pair in between. Run for both orientations of the pair: the driver as the
    /// Supply tab and the group's first member (how the Vendor and Healer are authored), and the driver as
    /// the Sold tab beside a mirror that is first. The peek must not care which button is the driver.
    ///
    /// Tabs are named by what the player sees, not by which toggle is the driver; which panel is showing
    /// is read from the order record the panels share, as in <see cref="TwoPanelToggleTests"/>.
    /// </summary>
    [TestFixture(true)]
    [TestFixture(false)]
    public sealed class TwoPanelPeekTests
    {
        private readonly bool driverIsSupply;

        private UiTestScene scene;
        private ToggleGroup group;
        private TwoPanelToggle driver;
        private TwoPanelMirrorToggle mirror;
        private AbstractToggle supplyTab;
        private AbstractToggle soldTab;
        private List<string> log;
        private ITwoPanelPeek peek;
        private bool keyHeld;
        private bool panelOpen;

        public TwoPanelPeekTests(bool driverIsSupply) => this.driverIsSupply = driverIsSupply;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            log = new List<string>();

            group = scene.Group(groupCanUntoggle: false);
            driver = scene.Element<TwoPanelToggle>(parent: group.transform);
            mirror = scene.Element<TwoPanelMirrorToggle>(parent: group.transform);
            UiTestScene.SetObject(mirror, "driver", driver);

            var supply = scene.Panel();
            var sold = scene.Panel();
            supply.Label = "Supply";
            sold.Label = "Sold";
            supply.Log = log;
            sold.Log = log;

            supplyTab = driverIsSupply ? driver : mirror;
            soldTab = driverIsSupply ? mirror : driver;
            UiTestScene.SetObject(driver, "panelWhenOn", driverIsSupply ? supply : sold);
            UiTestScene.SetObject(driver, "panelWhenOff", driverIsSupply ? sold : supply);

            // A panel opens on the Supply: the first member of its tab group.
            group.Activate(supplyTab);

            // The authored state is only told to the panels by the toggle's Start, which does not run
            // here: switch both ways once so the panels have heard where the pair is.
            soldTab.SetToggle(true);
            supplyTab.SetToggle(true);

            keyHeld = false;
            panelOpen = true;
            peek = new TwoPanelPeek(driver, mirror, () => keyHeld, () => panelOpen);
        }

        [TearDown]
        public void TearDown() => scene.Dispose();

        private bool ShowingSupply => group.ActiveMember == supplyTab && Showing("Supply") && Hiding("Sold");

        private bool ShowingSold => group.ActiveMember == soldTab && Showing("Sold") && Hiding("Supply");

        private bool Showing(string label) => log.LastOrDefault(entry => entry.StartsWith(label)) == label + "+";

        private bool Hiding(string label) => log.LastOrDefault(entry => entry.StartsWith(label)) == label + "-";

        private void Hold()
        {
            keyHeld = true;
            peek.Tick();
        }

        private void Release()
        {
            keyHeld = false;
            peek.Tick();
        }

        /// <summary>The group's reset once the panel it lives in has finished closing, driven directly:
        /// the Unity message does not run in EditMode.</summary>
        private void PanelFinishesClosing()
        {
            var parent = scene.Panel();
            parent.Disappear(true);
            group.ResetWhenCollapsed(parent);
        }

        [Test]
        public void Holding_ShowsTheSoldTab_FromTheSupply_AndReleasingReturns()
        {
            Hold();

            Assert.That(ShowingSold, Is.True);

            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void Holding_ShowsTheSupply_FromTheSoldTab_AndReleasingReturns()
        {
            soldTab.SetToggle(true);

            Hold();

            Assert.That(ShowingSupply, Is.True);

            Release();

            Assert.That(ShowingSold, Is.True);
        }

        [Test]
        public void ARepeatedHoldFrame_DoesNothingMore()
        {
            Hold();

            peek.Tick();
            peek.Tick();

            Assert.That(ShowingSold, Is.True);
        }

        [Test]
        public void ReleasingWithNoPeek_DoesNothing()
        {
            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void HoldingThenClickingTheHomeTab_StaysHome_OnReleaseAndWhileStillHeld()
        {
            Hold();

            supplyTab.SetToggle(true);
            peek.Tick();

            Assert.That(ShowingSupply, Is.True, "the click is a choice, the held key does not flip it back");

            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void HoldingThenClickingTheHomeTab_FromTheSoldTab_StaysOnTheSoldTab()
        {
            soldTab.SetToggle(true);
            Hold();

            soldTab.SetToggle(true);
            Release();

            Assert.That(ShowingSold, Is.True);
        }

        [Test]
        public void HoldingThenClickingThePeekedTab_IsRefused_AndReleasingReturnsHome()
        {
            Hold();

            soldTab.SetToggle(true);

            Assert.That(ShowingSold, Is.True, "a click on the tab that is on changes nothing");

            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        /// <summary>The group's reset goes home to its first member, which here is the Supply tab - the
        /// driver itself in one orientation - and the peek stands down, so the release gives nothing back.</summary>
        [Test]
        public void TheGroupsResetOnClosing_GoesHome_AndTheReleaseLeavesItThere()
        {
            Hold();

            PanelFinishesClosing();

            Assert.That(ShowingSupply, Is.True);

            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void HoldingClosingReopeningAndReleasing_StaysOnTheSupply()
        {
            Hold();

            panelOpen = false;
            peek.Tick();
            PanelFinishesClosing();
            panelOpen = true;
            peek.Tick();

            Assert.That(ShowingSupply, Is.True, "reopened on the Supply, the held key does not peek again");

            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        /// <summary>The peek lands on the Supply, the group's first member, so the reset on closing finds
        /// the pair already home and moves nothing. The reset is still raised, and that ends the peek: a
        /// release after the reopen would otherwise put the player back on the Sold tab they had left by
        /// closing.</summary>
        [Test]
        public void PeekingFromTheSoldTab_ThenClosingAndReopening_ReleasingStaysOnTheSupply()
        {
            soldTab.SetToggle(true);
            Hold();
            Assert.That(ShowingSupply, Is.True);

            panelOpen = false;
            peek.Tick();
            PanelFinishesClosing();
            panelOpen = true;
            peek.Tick();

            Assert.That(ShowingSupply, Is.True);

            Release();

            Assert.That(ShowingSupply, Is.True, "the reset abandoned the restore");
        }

        /// <summary>An ancestor fading, another screen briefly over the panel: the panel is not open for a
        /// frame, but its group never reset. The peek has not been abandoned, so the release gives it back.</summary>
        [Test]
        public void AFrameWithThePanelNotOpen_WithoutTheGroupsReset_DoesNotStrandThePlayerOnThePeekedTab()
        {
            Hold();
            Assert.That(ShowingSold, Is.True);

            panelOpen = false;
            peek.Tick();
            panelOpen = true;
            peek.Tick();
            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void AFrameWithThePanelNotOpen_WithoutTheGroupsReset_FromTheSoldTab_ReleaseReturnsToTheSoldTab()
        {
            soldTab.SetToggle(true);
            Hold();
            Assert.That(ShowingSupply, Is.True);

            panelOpen = false;
            peek.Tick();
            panelOpen = true;
            peek.Tick();
            Release();

            Assert.That(ShowingSold, Is.True);
        }

        [Test]
        public void TheKeyLetGoWhileThePanelIsNotOpen_WithoutTheGroupsReset_StillGivesTheTabBack()
        {
            soldTab.SetToggle(true);
            Hold();

            panelOpen = false;
            Release();

            Assert.That(ShowingSold, Is.True);
        }

        [Test]
        public void PeekingFromTheSoldTab_ThenTheKeyIsLetGoWhileClosed_StaysOnTheSupply()
        {
            soldTab.SetToggle(true);
            Hold();

            panelOpen = false;
            peek.Tick();
            PanelFinishesClosing();
            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void AfterTheCloseAbandonedAPeek_ANewPressPeeksAgain()
        {
            soldTab.SetToggle(true);
            Hold();
            panelOpen = false;
            peek.Tick();
            PanelFinishesClosing();
            panelOpen = true;
            Release();

            Hold();

            Assert.That(ShowingSold, Is.True);

            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void HoldingBeforeThePanelOpens_PeeksOnceItOpens()
        {
            panelOpen = false;
            Hold();

            Assert.That(ShowingSupply, Is.True, "a closed panel is not moved");

            panelOpen = true;
            peek.Tick();

            Assert.That(ShowingSold, Is.True);

            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void HoldingWithThePanelClosed_DoesNothing()
        {
            panelOpen = false;

            Hold();
            peek.Tick();
            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        [Test]
        public void LosingFocus_ReleasesThePeek_AndTheKeyMustBeLetGoBeforeItPeeksAgain()
        {
            Hold();

            peek.Release();

            Assert.That(ShowingSupply, Is.True);

            peek.Tick();

            Assert.That(ShowingSupply, Is.True, "the same hold does not peek twice");

            Release();
            Hold();

            Assert.That(ShowingSold, Is.True, "a new press peeks again");
        }

        /// <summary>Two clicks in separate frames - home, then the peeked tab - leave the player where the
        /// last click put them, and the release does not move them again.</summary>
        [Test]
        public void HoldingThenClickingHomeAndThenThePeekedTab_StaysOnThePeekedTab()
        {
            Hold();

            supplyTab.SetToggle(true);
            peek.Tick();
            soldTab.SetToggle(true);
            peek.Tick();
            Release();

            Assert.That(ShowingSold, Is.True, "the last click was a choice");
        }
    }
}
