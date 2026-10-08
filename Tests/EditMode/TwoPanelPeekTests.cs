using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Submodules.Utility.Tests.TestSupport;
using Submodules.Utility.UI;
using ToggleGroup = Submodules.Utility.UI.ToggleGroup;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// A peek flips a two-panel switch's bool while a key is held and puts it back on release, unless
    /// something else wrote the bool in between. Run for both orientations of the pair: the driver as the
    /// Supply tab and the group's first member (how the Vendor and Healer are authored), and the driver as
    /// the Sold tab beside a mirror that is first. The peek must not care which button the bool belongs to.
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
        private TwoPanelPeek peek;
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
            peek = new TwoPanelPeek(driver, () => keyHeld, () => panelOpen);
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
        public void ARepeatedHoldFrame_WritesNothingMore()
        {
            Hold();
            var writes = driver.Writes;

            peek.Tick();
            peek.Tick();

            Assert.That(driver.Writes, Is.EqualTo(writes));
            Assert.That(ShowingSold, Is.True);
        }

        [Test]
        public void ReleasingWithNoPeek_WritesNothing()
        {
            var writes = driver.Writes;

            Release();

            Assert.That(driver.Writes, Is.EqualTo(writes));
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
            var writes = driver.Writes;

            soldTab.SetToggle(true);

            Assert.That(driver.Writes, Is.EqualTo(writes), "a click on the tab that is on writes nothing");

            Release();

            Assert.That(ShowingSupply, Is.True);
        }

        /// <summary>The group's reset goes home to its first member, which here is the Supply tab - the
        /// driver itself in one orientation. Switching the driver on is a write however it is reached.</summary>
        [Test]
        public void TheGroupsResetOnClosing_IsAWrite_EvenWhenTheSupplyIsTheFirstMember()
        {
            Hold();
            var writes = driver.Writes;

            PanelFinishesClosing();

            Assert.That(driver.Writes, Is.EqualTo(writes + 1));
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
        /// the pair already home and writes nothing. The close itself must then end the peek: a release
        /// after the reopen would otherwise put the player back on the Sold tab they had left by closing.</summary>
        [Test]
        public void PeekingFromTheSoldTab_ThenClosingAndReopening_ReleasingStaysOnTheSupply()
        {
            soldTab.SetToggle(true);
            Hold();
            Assert.That(ShowingSupply, Is.True);
            var writes = driver.Writes;

            panelOpen = false;
            peek.Tick();
            PanelFinishesClosing();
            panelOpen = true;
            peek.Tick();

            Assert.That(driver.Writes, Is.EqualTo(writes), "the reset found the pair home: not a write");
            Assert.That(ShowingSupply, Is.True);

            Release();

            Assert.That(ShowingSupply, Is.True, "the close abandoned the restore");
            Assert.That(driver.Writes, Is.EqualTo(writes), "the release wrote nothing");
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
            var writes = driver.Writes;

            Hold();
            peek.Tick();
            Release();

            Assert.That(driver.Writes, Is.EqualTo(writes));
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

        /// <summary>The peek names its driver by <see cref="ITwoPanelDriver"/> only, so a test double
        /// stands in for the toggle.</summary>
        [Test]
        public void ThePeekDependsOnTheInterface_NotOnTheToggle()
        {
            var fake = new FakeDriver { IsOn = true };
            var onFake = new TwoPanelPeek(fake, () => keyHeld, () => panelOpen);

            keyHeld = true;
            onFake.Tick();

            Assert.That(fake.IsOn, Is.False);

            keyHeld = false;
            onFake.Tick();

            Assert.That(fake.IsOn, Is.True);
            Assert.That(fake.Sets, Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void TheDriversGroupSideWrite_SwitchesTheBoolBothWays_FromEitherOrientation()
        {
            ITwoPanelDriver asDriver = driver;
            var start = driver.IsOn;

            asDriver.SetFromGroup(!start);

            Assert.That(driver.IsOn, Is.EqualTo(!start));
            Assert.That(group.ActiveMember, Is.SameAs(driver.IsOn ? driver : mirror));

            asDriver.SetFromGroup(start);

            Assert.That(driver.IsOn, Is.EqualTo(start));
            Assert.That(group.ActiveMember, Is.SameAs(start ? driver : mirror));
        }

        private sealed class FakeDriver : ITwoPanelDriver
        {
            public bool IsOn { get; set; }
            public int Writes { get; private set; }
            public List<bool> Sets { get; } = new();

            public void SetFromGroup(bool on)
            {
                IsOn = on;
                Writes++;
                Sets.Add(on);
            }
        }
    }
}
