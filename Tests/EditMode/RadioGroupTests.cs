using System.Reflection;
using NUnit.Framework;
using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="RadioGroup"/> answers two questions and nothing else: which toggle is
    /// active, and which one a caller should restore if the group empties. Both are read
    /// by <c>MultiplePanelToggle</c> and by <c>MapPanel</c>, so both are pinned here.
    ///
    /// Driven through <see cref="RadioGroup.Activate"/> / <see cref="RadioGroup.Deactivate"/>
    /// — the whole public surface since the membership list was removed and
    /// <c>Adopt</c> retired: a toggle's group is exactly its nearest <see cref="RadioGroup"/>
    /// ancestor, so <see cref="UiTestScene.Toggle"/> parents a toggle under the group the same
    /// way a real scene would.
    /// </summary>
    [TestFixture]
    public sealed class RadioGroupTests
    {
        private UiTestScene scene;
        private RadioGroup group;

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
            group.Activate(toggle);

            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            group.Activate(toggle);

            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Activate_AnnouncesTheChange()
        {
            var toggle = scene.Toggle(group);
            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            group.Activate(toggle);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Deactivate_ClearsTheActiveToggle()
        {
            var clearable = scene.Group(isClearable: true);
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

            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            group.Deactivate(other);

            Assert.That(group.ActiveMember, Is.SameAs(active));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Deactivate_AnnouncesTheChange()
        {
            var clearable = scene.Group(isClearable: true);
            var toggle = scene.Toggle(clearable);
            clearable.Activate(toggle);

            var changes = 0;
            clearable.OnGroupChanged += _ => changes++;

            clearable.Deactivate(toggle);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Deactivate_RemembersTheToggleItCleared()
        {
            var clearable = scene.Group(isClearable: true);
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
            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            Assert.That(() => group.Deactivate(null), Throws.Nothing);
            Assert.That(changes, Is.Zero, "a null toggle must not be read as 'the (null) active toggle switched off'");
        }

        /// <summary><see cref="RadioGroup.IsRestorable"/> is the fallback checked when a group
        /// is not <see cref="RadioGroup.IsClearable"/>: instead of ending up with nothing
        /// selected, switching the active toggle off re-selects whichever one was on before it.
        /// The panel-side counterpart is <c>PanelGroupTests.Deactivate_NotClearable_Restorable_*</c>;
        /// both run the one implementation in <see cref="ExclusiveGroup{TMember}"/>, but only
        /// this side proves the toggle's own on/off primitive is what the restore drives.</summary>
        [Test]
        public void Deactivate_NotClearable_Restorable_RestoresThePreviousMember()
        {
            var restorable = scene.Group(isRestorable: true);
            var first = scene.Toggle(restorable);
            var second = scene.Toggle(restorable);
            restorable.Activate(first);
            restorable.Activate(second);

            restorable.Deactivate(second);

            Assert.That(restorable.ActiveMember, Is.SameAs(first));
        }

        [Test]
        public void Deactivate_NotClearable_Restorable_TurnsTheClearedToggleOff_AndTheRestoredToggleOn()
        {
            var restorable = scene.Group(isRestorable: true);
            var first = scene.Toggle(restorable);
            var second = scene.Toggle(restorable);
            restorable.Activate(first);
            restorable.Activate(second);

            restorable.Deactivate(second);

            Assert.That(second.IsOn, Is.False);
            Assert.That(first.IsOn, Is.True, "a restore has to re-run the toggle's own state primitive, not just re-point the group");
        }

        [Test]
        public void Deactivate_NotClearable_Restorable_AnnouncesTheChange_WithTheRestoredToggle()
        {
            var restorable = scene.Group(isRestorable: true);
            var first = scene.Toggle(restorable);
            var second = scene.Toggle(restorable);
            restorable.Activate(first);
            restorable.Activate(second);
            AbstractToggle announced = null;
            restorable.OnGroupChanged += t => announced = t;

            restorable.Deactivate(second);

            Assert.That(announced, Is.SameAs(first));
        }

        [Test]
        public void Deactivate_NotClearable_Restorable_WithNoPreviousMember_IsANoOp()
        {
            var restorable = scene.Group(isRestorable: true);
            var toggle = scene.Toggle(restorable);
            restorable.Activate(toggle);
            var changes = 0;
            restorable.OnGroupChanged += _ => changes++;

            restorable.Deactivate(toggle);

            Assert.That(restorable.ActiveMember, Is.SameAs(toggle), "nothing to restore to, so the group keeps what it has");
            Assert.That(changes, Is.Zero);
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
        public void SetToggle_False_OnTheActiveToggle_AnnouncesTheChange()
        {
            var clearable = scene.Group(isClearable: true);
            var toggle = scene.Toggle(clearable);
            toggle.SetToggle(true);

            var changes = 0;
            clearable.OnGroupChanged += _ => changes++;

            toggle.SetToggle(false);

            Assert.That(changes, Is.EqualTo(1),
                "the toggle switching itself off is a real 'no panel open' state change, "
                + "whichever of click / hotkey / script routed it through SetToggle, as long as the group is clearable");
        }

        /// <summary>The self-heal counterpart to the membership guard: a reference that was
        /// never produced by <see cref="RadioGroup.Activate"/> — e.g. a stale value left over
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

        private static void InvokeOnValidate(RadioGroup target) =>
            typeof(RadioGroup)
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(target, null);
    }
}
