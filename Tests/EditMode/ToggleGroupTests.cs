using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Submodules.Utility.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="ToggleGroup"/> answers two questions and nothing else: which toggle is
    /// active, and which one a caller should restore if the group empties. Both are read
    /// by <c>SidePanelToggle</c> and by <c>MinimapController</c>, so both are pinned here.
    ///
    /// Driven through <see cref="ToggleGroup.Activate"/> / <see cref="ToggleGroup.Deactivate"/>
    /// — the whole public surface since the membership list was removed and
    /// <c>Adopt</c> retired: a toggle's group is exactly its nearest <see cref="ToggleGroup"/>
    /// ancestor, so <see cref="UiTestScene.Toggle"/> parents a toggle under the group the same
    /// way a real scene would.
    /// </summary>
    [TestFixture]
    public sealed class ToggleGroupTests
    {
        private UiTestScene scene;
        private ToggleGroup group;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            group = scene.Group();
        }

        [TearDown]
        public void TearDown() => scene.Dispose();

        [Test]
        public void ANewGroup_HasNothingActive()
        {
            Assert.That(group.ActiveMember, Is.Null);
            Assert.That(group.PreviousMember, Is.Null);
        }

        [Test]
        public void Activate_MakesThatToggleTheActiveOne()
        {
            var toggle = scene.Toggle(group);

            group.Activate(toggle);

            Assert.That(group.ActiveMember, Is.SameAs(toggle));
        }

        [Test]
        public void Activate_Null_LeavesTheGroupAlone()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);

            group.Activate(null);

            Assert.That(group.ActiveMember, Is.SameAs(toggle));
        }

        [Test]
        public void Activate_RemembersTheToggleItReplaced()
        {
            var first = scene.Toggle(group);
            var second = scene.Toggle(group);

            group.Activate(first);
            group.Activate(second);

            Assert.That(group.ActiveMember, Is.SameAs(second));
            Assert.That(group.PreviousMember, Is.SameAs(first));
        }

        [Test]
        public void Activate_TurnsTheToggleItReplacedOff()
        {
            var first = scene.Toggle(group);
            var second = scene.Toggle(group);

            first.SetToggle(true);
            second.SetToggle(true);

            Assert.That(first.IsOn, Is.False, "two toggles in one group must not both be on");
            Assert.That(second.IsOn, Is.True);
        }

        [Test]
        public void Activate_TheAlreadyActiveToggle_ChangesNothing()
        {
            var toggle = scene.Toggle(group);
            var other = scene.Toggle(group);
            group.Activate(other);
            group.Activate(toggle);

            group.Activate(toggle);

            Assert.That(group.ActiveMember, Is.SameAs(toggle));
            Assert.That(group.PreviousMember, Is.SameAs(other), "re-activating the active toggle must not make it its own predecessor");
        }

        [Test]
        public void Deactivate_ClearsTheActiveToggle()
        {
            var clearable = scene.Group(userCanUntoggle: true);
            var toggle = scene.Toggle(clearable);
            clearable.Activate(toggle);

            clearable.Deactivate(toggle);

            Assert.That(clearable.ActiveMember, Is.Null);
        }

        [Test]
        public void Deactivate_NotClearable_IsANoOp()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);

            group.Deactivate(toggle);

            Assert.That(group.ActiveMember, Is.SameAs(toggle), "a group must keep its selection unless it opted into being clearable");
        }

        [Test]
        public void Deactivate_AToggleThatIsNotActive_LeavesTheGroupAlone()
        {
            var active = scene.Toggle(group);
            var other = scene.Toggle(group);
            group.Activate(active);

            group.Deactivate(other);

            Assert.That(group.ActiveMember, Is.SameAs(active));
            Assert.That(group.PreviousMember, Is.Null);
        }

        [Test]
        public void Deactivate_RemembersTheToggleItCleared()
        {
            var clearable = scene.Group(userCanUntoggle: true);
            var first = scene.Toggle(clearable);
            var second = scene.Toggle(clearable);

            clearable.Activate(first);
            clearable.Activate(second);

            clearable.Deactivate(second);

            Assert.That(clearable.PreviousMember, Is.SameAs(second),
                "the toggle that just went off is the one a restore has to bring back");
        }

        [Test]
        public void Deactivate_Null_LeavesTheGroupAlone_EvenWhenNothingIsActive()
        {
            Assert.That(() => group.Deactivate(null), Throws.Nothing);
            Assert.That(group.PreviousMember, Is.Null, "a null toggle must not be read as 'the (null) active toggle switched off'");
        }

        /// <summary>The two initiators of an un-toggle: the user (Deactivate's default) and the
        /// group (<see cref="UI.AbstractGroup{TMember}.ResetGroup"/>). A default group refuses the
        /// first and allows the second - radio behaviour for a click, but a derived-state sync or
        /// a phase change can still empty it.</summary>
        [Test]
        public void ResetGroup_OnADefaultGroup_ClearsIt_WhereTheUserCannot()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);

            group.ResetGroup();

            Assert.That(group.ActiveMember, Is.Null);
            Assert.That(toggle.IsOn, Is.False);
        }

        [Test]
        public void ResetGroup_WhenTheGroupMayNotUntoggle_GoesBackToTheFirstToggle()
        {
            var locked = scene.Group(groupCanUntoggle: false);
            var first = scene.Toggle(locked);
            var second = scene.Toggle(locked);
            locked.Activate(first);
            locked.Activate(second);

            locked.ResetGroup();

            Assert.That(locked.ActiveMember, Is.SameAs(first), "never empty: cleared means back to the first");
            Assert.That(first.IsOn, Is.True);
            Assert.That(second.IsOn, Is.False);
        }

        [Test]
        public void ResetGroup_WhenTheFirstToggleIsAlreadyActive_ChangesNothing()
        {
            var locked = scene.Group(groupCanUntoggle: false);
            var first = scene.Toggle(locked);
            locked.Activate(first);

            locked.ResetGroup();

            Assert.That(locked.ActiveMember, Is.SameAs(first));
            Assert.That(first.IsOn, Is.True);
        }

        [Test]
        public void TheUsersUntoggleOfAGroupThatCannotBeEmpty_IsStillRefused_NotAReset()
        {
            var locked = scene.Group(groupCanUntoggle: false);
            var first = scene.Toggle(locked);
            var second = scene.Toggle(locked);
            locked.Activate(first);
            locked.Activate(second);
            LogAssert.Expect(LogType.Log, new Regex(@"^SetToggle\(false\) prevented\."));

            second.SetToggle(false);

            Assert.That(locked.ActiveMember, Is.SameAs(second), "a click on the active tab must not switch tabs");
        }

        [Test]
        public void Deactivate_WhenTheUserMayUntoggle_AllowsTheGroupToToo()
        {
            var userGroup = scene.Group(userCanUntoggle: true, groupCanUntoggle: false);
            var toggle = scene.Toggle(userGroup);
            userGroup.Activate(toggle);

            userGroup.ResetGroup();

            Assert.That(userGroup.ActiveMember, Is.Null, "where the user may untoggle, the group may as well");
        }

        [Test]
        public void FirstMember_IsTheFirstToggleThatBecameActive_AndStaysSo_OnAGroupThatCannotBeEmpty()
        {
            var locked = scene.Group(groupCanUntoggle: false);
            var first = scene.Toggle(locked);
            var second = scene.Toggle(locked);

            locked.Activate(first);
            locked.Activate(second);

            Assert.That(locked.FirstMember, Is.SameAs(first));
        }

        [Test]
        public void FirstMember_IsNotKept_OnAGroupThatMayBeEmptied()
        {
            var toggle = scene.Toggle(group);

            group.Activate(toggle);

            Assert.That(group.FirstMember, Is.Null);
        }

        [Test]
        public void ResetToFirst_OnAGroupThatCanNeverBeEmpty_SwitchesBackToTheFirstToggle()
        {
            var locked = scene.Group(groupCanUntoggle: false);
            var first = scene.Toggle(locked);
            var second = scene.Toggle(locked);
            locked.Activate(first);
            locked.Activate(second);

            locked.ResetToFirst();

            Assert.That(locked.ActiveMember, Is.SameAs(first));
            Assert.That(first.IsOn, Is.True);
            Assert.That(second.IsOn, Is.False);
        }

        [Test]
        public void ResetToFirst_OnAGroupThatMayBeEmptied_LeavesTheActiveToggleAlone()
        {
            var first = scene.Toggle(group);
            var second = scene.Toggle(group);
            group.Activate(first);
            group.Activate(second);

            group.ResetToFirst();

            Assert.That(group.ActiveMember, Is.SameAs(second), "with no obligation to keep one on there is no home");
        }

        [Test]
        public void ResetToFirst_WhereTheFirstToggleIsAlreadyActive_ChangesNothing()
        {
            var locked = scene.Group(groupCanUntoggle: false);
            var first = scene.Toggle(locked);
            locked.Activate(first);

            locked.ResetToFirst();

            Assert.That(locked.ActiveMember, Is.SameAs(first));
            Assert.That(locked.PreviousMember, Is.Null, "no switch happened");
        }

        /// <summary><see cref="UI.ToggleGroup.ResetWithParentPanel"/>'s reset, driven directly:
        /// the Unity message and the group's <c>Awake</c> do not run in EditMode. A group that may be
        /// emptied is emptied once its parent panel has finished closing.</summary>
        [Test]
        public void ResetWhenCollapsed_AGroupThatMayBeEmptied_IsEmptied()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);
            var parent = scene.Panel();
            parent.Disappear(true);

            group.ResetWhenCollapsed(parent);

            Assert.That(group.ActiveMember, Is.Null, "a selection must not outlive the panel that showed it");
        }

        [Test]
        public void ResetWhenCollapsed_WhileTheParentIsStillUp_ChangesNothing()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);
            var parent = scene.Panel();
            parent.Appear(true);

            group.ResetWhenCollapsed(parent);

            Assert.That(group.ActiveMember, Is.SameAs(toggle));
        }

        /// <summary>A group that can never be empty goes home even with nothing active - the state a
        /// destroyed or cleared active member leaves behind, which <c>ResetGroup</c> alone would
        /// treat as "nothing to deactivate".</summary>
        [Test]
        public void ResetWhenCollapsed_AGroupThatCanNeverBeEmpty_GoesBackToTheFirstToggle_EvenWithNothingActive()
        {
            var locked = scene.Group(groupCanUntoggle: false);
            var first = scene.Toggle(locked);
            var second = scene.Toggle(locked);
            locked.Activate(first);
            locked.Activate(second);
            UiTestScene.SetObject(locked, "<ActiveMember>k__BackingField", null);
            var parent = scene.Panel();
            parent.Disappear(true);

            locked.ResetWhenCollapsed(parent);

            Assert.That(locked.ActiveMember, Is.SameAs(first));
        }

        /// <summary>The membership guard (issue: a hand-edited/reparented toggle left a
        /// foreign group's <c>ActivatedToggle</c> pointing at it). A group can only ever
        /// activate its own child.</summary>
        [Test]
        public void Activate_AToggleThatBelongsToAnotherGroup_IsIgnored()
        {
            var otherGroup = scene.Group();
            var foreign = scene.Toggle(otherGroup);

            group.Activate(foreign);

            Assert.That(group.ActiveMember, Is.Null, "a group can only activate its own members");
        }

        [Test]
        public void SetToggle_False_OnTheActiveToggle_ClearsTheGroup()
        {
            var clearable = scene.Group(userCanUntoggle: true);
            var toggle = scene.Toggle(clearable);
            toggle.SetToggle(true);

            toggle.SetToggle(false);

            Assert.That(clearable.ActiveMember, Is.Null,
                "the toggle switching itself off is a real 'no panel open' state change, "
                + "whichever of click / hotkey / script routed it through SetToggle, as long as the group is clearable");
        }

        /// <summary>The self-heal counterpart to the membership guard: a reference that was
        /// never produced by <see cref="ToggleGroup.Activate"/> — e.g. a stale value left over
        /// from a reparent, or hand-edited directly in the Inspector — is cleared the next
        /// time the Editor validates the group, rather than persisting indefinitely.</summary>
        [Test]
        public void OnValidate_AnActivatedToggleThatIsNoLongerAMember_IsCleared()
        {
            var otherGroup = scene.Group();
            var foreign = scene.Toggle(otherGroup);
            UiTestScene.SetObject(group, "<ActiveMember>k__BackingField", foreign);

            InvokeOnValidate(group);

            Assert.That(group.ActiveMember, Is.Null);
        }

        [Test]
        public void OnValidate_APreviouslyActivatedToggleThatIsNoLongerAMember_IsCleared()
        {
            var otherGroup = scene.Group();
            var foreign = scene.Toggle(otherGroup);
            UiTestScene.SetObject(group, "<PreviousMember>k__BackingField", foreign);

            InvokeOnValidate(group);

            Assert.That(group.PreviousMember, Is.Null);
        }

        [Test]
        public void OnValidate_DoesNotClearAGenuineMember()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);

            InvokeOnValidate(group);

            Assert.That(group.ActiveMember, Is.SameAs(toggle));
        }

        private static void InvokeOnValidate(ToggleGroup target) =>
            typeof(ToggleGroup)
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(target, null);
    }
}
