using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Submodules.Utility.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="PanelGroup"/> is <see cref="RadioGroup"/>'s counterpart for content rather
    /// than input: panels occupying the same screen space, where showing one hides whichever
    /// else is up. Deliberately driven the same way — <see cref="PanelGroup.Activate"/> /
    /// <see cref="PanelGroup.Deactivate"/>, membership by hierarchy, an
    /// <see cref="PanelGroup.OnGroupChanged"/> announcement — so a caller who already knows
    /// <see cref="RadioGroup"/> reads this for free. The difference is what drives it: a
    /// toggle asking the group to show its panel is one caller, but not the only one —
    /// phase-change code can call <see cref="PanelGroup.Activate"/> directly, with no toggle
    /// involved at all.
    /// </summary>
    [TestFixture]
    public sealed class PanelGroupTests
    {
        private UiTestScene scene;
        private PanelGroup group;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            group = scene.PanelGroup();
        }

        [TearDown]
        public void TearDown() => scene.Dispose();

        [Test]
        public void ANewGroup_HasNothingActive()
        {
            Assert.That(group.ActiveMember, Is.Null);
        }

        [Test]
        public void Activate_MakesThatPanelTheActiveOne()
        {
            var panel = scene.Panel(group);

            group.Activate(panel);

            Assert.That(group.ActiveMember, Is.SameAs(panel));
        }

        [Test]
        public void Activate_FadesThePanelIn()
        {
            var panel = scene.Panel(group);

            group.Activate(panel);

            Assert.That(panel.FadeInCalls, Is.EqualTo(1));
        }

        [Test]
        public void Activate_Null_LeavesTheGroupAlone()
        {
            var panel = scene.Panel(group);
            group.Activate(panel);

            group.Activate(null);

            Assert.That(group.ActiveMember, Is.SameAs(panel));
        }

        [Test]
        public void Activate_FadesTheReplacedPanelOut()
        {
            var first = scene.Panel(group);
            var second = scene.Panel(group);

            group.Activate(first);
            group.Activate(second);

            Assert.That(first.FadeOutCalls, Is.EqualTo(1));
            Assert.That(second.FadeInCalls, Is.EqualTo(1));
            Assert.That(group.ActiveMember, Is.SameAs(second));
        }

        /// <summary>
        /// The ordering <see cref="PanelGroup.Activate"/> exists to provide — the reason a group is
        /// not merely "one panel in, one panel out": the replaced panel publishes its disappear
        /// <i>before</i> the replacement publishes its appear.
        ///
        /// <para>Game code relies on that gap. A Side Panel announces its
        /// <c>SidePanelContext</c> from these same two hooks, and the Sell Basket cancels a
        /// staged sale on the <c>None</c> published between them — so a swap that announced the
        /// two the other way round would reopen what it had just closed. Counters cannot see
        /// the difference; only a shared record can.</para>
        /// </summary>
        [Test]
        public void Activate_ReplacedPanelDisappears_BeforeTheReplacementAppears()
        {
            var log = new List<string>();
            var first = scene.Panel(group);
            var second = scene.Panel(group);
            first.Label = "first";
            second.Label = "second";
            first.Log = log;
            second.Log = log;

            group.Activate(first);
            group.Activate(second);

            Assert.That(log, Is.EqualTo(new[] { "first+", "first-", "second+" }));
        }

        [Test]
        public void Activate_TheAlreadyActiveMember_ChangesNothing()
        {
            var panel = scene.Panel(group);
            group.Activate(panel);

            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            group.Activate(panel);

            Assert.That(changes, Is.Zero);
            Assert.That(panel.FadeInCalls, Is.EqualTo(1), "re-showing the active panel must not re-fade it");
        }

        [Test]
        public void Activate_AnnouncesTheChange()
        {
            var panel = scene.Panel(group);
            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            group.Activate(panel);

            Assert.That(changes, Is.EqualTo(1));
        }

        /// <summary>The membership guard, mirroring <c>RadioGroup.Activate</c>: a group can
        /// only ever show its own child.</summary>
        [Test]
        public void Activate_APanelThatBelongsToAnotherGroup_IsIgnored()
        {
            var otherGroup = scene.PanelGroup();
            var foreign = scene.Panel(otherGroup);

            group.Activate(foreign);

            Assert.That(group.ActiveMember, Is.Null);
        }

        [Test]
        public void Clear_NotClearable_IsANoOp()
        {
            var panel = scene.Panel(group);
            group.Activate(panel);

            group.Deactivate(panel);

            Assert.That(group.ActiveMember, Is.SameAs(panel), "the group must always keep a panel shown unless it opted into being clearable");
        }

        [Test]
        public void Clear_Clearable_ClearsTheActiveMember()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var panel = scene.Panel(clearable);
            clearable.Activate(panel);

            clearable.Deactivate(panel);

            Assert.That(clearable.ActiveMember, Is.Null);
        }

        [Test]
        public void Clear_Clearable_FadesThePanelOut()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var panel = scene.Panel(clearable);
            clearable.Activate(panel);

            clearable.Deactivate(panel);

            Assert.That(panel.FadeOutCalls, Is.EqualTo(1));
        }

        [Test]
        public void Clear_APanelThatIsNotActive_LeavesTheGroupAlone()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var active = scene.Panel(clearable);
            var other = scene.Panel(clearable);
            clearable.Activate(active);

            var changes = 0;
            clearable.OnGroupChanged += _ => changes++;

            clearable.Deactivate(other);

            Assert.That(clearable.ActiveMember, Is.SameAs(active));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Clear_AnnouncesTheChange()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var panel = scene.Panel(clearable);
            clearable.Activate(panel);

            var changes = 0;
            clearable.OnGroupChanged += _ => changes++;

            clearable.Deactivate(panel);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Clear_Null_LeavesTheGroupAlone_EvenWhenNothingIsActive()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var changes = 0;
            clearable.OnGroupChanged += _ => changes++;

            Assert.That(() => clearable.Deactivate(null), Throws.Nothing);
            Assert.That(changes, Is.Zero);
        }

        /// <summary><see cref="PanelGroup.IsRestorable"/> is the fallback checked when a group
        /// is not <see cref="PanelGroup.IsClearable"/>: instead of ending up with nothing
        /// shown, hiding the active panel re-shows whichever one was up before it.</summary>
        [Test]
        public void Deactivate_NotClearable_Restorable_RestoresThePreviousMember()
        {
            var restorable = scene.PanelGroup(isRestorable: true);
            var first = scene.Panel(restorable);
            var second = scene.Panel(restorable);
            restorable.Activate(first);
            restorable.Activate(second);

            restorable.Deactivate(second);

            Assert.That(restorable.ActiveMember, Is.SameAs(first));
        }

        [Test]
        public void Deactivate_NotClearable_Restorable_FadesTheHiddenPanelOutAndTheRestoredPanelIn()
        {
            var restorable = scene.PanelGroup(isRestorable: true);
            var first = scene.Panel(restorable);
            var second = scene.Panel(restorable);
            restorable.Activate(first);
            restorable.Activate(second);

            restorable.Deactivate(second);

            Assert.That(second.FadeOutCalls, Is.EqualTo(1));
            Assert.That(first.FadeInCalls, Is.EqualTo(2), "shown once by the initial Show, once by the restore");
        }

        [Test]
        public void Deactivate_NotClearable_Restorable_AnnouncesTheChange_WithTheRestoredPanel()
        {
            var restorable = scene.PanelGroup(isRestorable: true);
            var first = scene.Panel(restorable);
            var second = scene.Panel(restorable);
            restorable.Activate(first);
            restorable.Activate(second);
            SimplePanel announced = null;
            restorable.OnGroupChanged += p => announced = p;

            restorable.Deactivate(second);

            Assert.That(announced, Is.SameAs(first));
        }

        [Test]
        public void Deactivate_NotClearable_Restorable_RemembersTheToggleItReplaced()
        {
            var restorable = scene.PanelGroup(isRestorable: true);
            var first = scene.Panel(restorable);
            var second = scene.Panel(restorable);
            restorable.Activate(first);
            restorable.Activate(second);

            restorable.Deactivate(second);

            Assert.That(restorable.PreviousMember, Is.SameAs(second),
                "the panel that just went off is the one a second Hide would need to restore back to");
        }

        [Test]
        public void Deactivate_NotClearable_Restorable_WithNoPreviousMember_IsANoOp()
        {
            var restorable = scene.PanelGroup(isRestorable: true);
            var panel = scene.Panel(restorable);
            restorable.Activate(panel);
            var changes = 0;
            restorable.OnGroupChanged += _ => changes++;

            restorable.Deactivate(panel);

            Assert.That(restorable.ActiveMember, Is.SameAs(panel), "nothing to restore to, so the group keeps what it has");
            Assert.That(changes, Is.Zero);
        }

        /// <summary><see cref="SimplePanel.FadeOut"/>'s own guard, the counterpart to
        /// <c>AbstractToggle.SetToggle</c>'s: the group-aware entry point refuses outright on
        /// the sole panel of a group that is neither Clearable nor Restorable.
        ///
        /// <para>The panel staying up is <em>not</em> enough to pin this — delete the guard and
        /// <see cref="ExclusiveGroup{TMember}.Deactivate"/> no-ops on exactly the same
        /// configuration, so the outcome is identical (verified by removing it: the suite stayed
        /// green). The guard's one unique effect is the author-facing log telling you which two
        /// settings would allow the fade-out, so that is what this expects.</para></summary>
        [Test]
        public void FadeOut_OnTheActivePanel_NeitherClearableNorRestorable_IsRefused()
        {
            var panel = scene.Panel(group);
            panel.FadeIn();
            var fadeOutsBefore = panel.FadeOutCalls;
            LogAssert.Expect(LogType.Log, new Regex(@"^FadeOut\(\) prevented\."));

            panel.FadeOut();

            Assert.That(group.ActiveMember, Is.SameAs(panel));
            Assert.That(panel.FadeOutCalls, Is.EqualTo(fadeOutsBefore), "the panel must not fade out behind the group's back");
        }

        [Test]
        public void FadeOut_OnTheActivePanel_Restorable_RestoresThePreviousPanel()
        {
            var restorable = scene.PanelGroup(isRestorable: true);
            var first = scene.Panel(restorable);
            var second = scene.Panel(restorable);
            first.FadeIn();
            second.FadeIn();

            second.FadeOut();

            Assert.That(restorable.ActiveMember, Is.SameAs(first),
                "the guard must not refuse a fade-out a restorable group can absorb");
        }

        /// <summary>The convenience a caller like Go Venture needs: close whichever panel is
        /// open without first asking the group which one that is.</summary>
        [Test]
        public void ClearActive_ClearsWhicheverPanelIsShown()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var panel = scene.Panel(clearable);
            clearable.Activate(panel);

            clearable.ClearActive();

            Assert.That(clearable.ActiveMember, Is.Null);
        }

        [Test]
        public void ClearActive_WhenNothingIsActive_IsANoOp()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var changes = 0;
            clearable.OnGroupChanged += _ => changes++;

            Assert.That(() => clearable.ClearActive(), Throws.Nothing);
            Assert.That(changes, Is.Zero);
        }

        /// <summary>The self-heal counterpart to the membership guard, mirroring
        /// <c>RadioGroup.OnValidate</c>: a reference that is no longer one of the group's own
        /// children is cleared the next time the Editor validates the group.</summary>
        [Test]
        public void OnValidate_AnActiveMemberThatIsNoLongerAMember_IsCleared()
        {
            var otherGroup = scene.PanelGroup();
            var foreign = scene.Panel(otherGroup);
            UiTestScene.SetObject(group, "<ActiveMember>k__BackingField", foreign);

            InvokeOnValidate(group);

            Assert.That(group.ActiveMember, Is.Null);
        }

        [Test]
        public void OnValidate_DoesNotClearAGenuineMember()
        {
            var panel = scene.Panel(group);
            group.Activate(panel);

            InvokeOnValidate(group);

            Assert.That(group.ActiveMember, Is.SameAs(panel));
        }

        private static void InvokeOnValidate(PanelGroup target) =>
            typeof(PanelGroup)
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(target, null);
    }
}
