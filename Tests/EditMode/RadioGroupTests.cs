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
    /// Driven through <see cref="RadioGroup.Select"/> / <see cref="RadioGroup.Deselect"/>
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
            Assert.That(group.SelectedToggle, Is.Null);
            Assert.That(group.PreviouslySelectedToggle, Is.Null);
        }

        [Test]
        public void Activate_MakesThatToggleTheActiveOne()
        {
            var toggle = scene.Toggle(group);

            group.Select(toggle);

            Assert.That(group.SelectedToggle, Is.SameAs(toggle));
        }

        [Test]
        public void Activate_Null_LeavesTheGroupAlone()
        {
            var toggle = scene.Toggle(group);
            group.Select(toggle);

            group.Select(null);

            Assert.That(group.SelectedToggle, Is.SameAs(toggle));
        }

        [Test]
        public void Activate_RemembersTheToggleItReplaced()
        {
            var first = scene.Toggle(group);
            var second = scene.Toggle(group);

            group.Select(first);
            group.Select(second);

            Assert.That(group.SelectedToggle, Is.SameAs(second));
            Assert.That(group.PreviouslySelectedToggle, Is.SameAs(first));
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
            group.Select(toggle);

            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            group.Select(toggle);

            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Activate_AnnouncesTheChange()
        {
            var toggle = scene.Toggle(group);
            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            group.Select(toggle);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Deactivate_ClearsTheActiveToggle()
        {
            var deselectable = scene.Group(isDeselectable: true);
            var toggle = scene.Toggle(deselectable);
            deselectable.Select(toggle);

            deselectable.Deselect(toggle);

            Assert.That(deselectable.SelectedToggle, Is.Null);
        }

        [Test]
        public void Deactivate_NotDeselectable_IsANoOp()
        {
            var toggle = scene.Toggle(group);
            group.Select(toggle);

            group.Deselect(toggle);

            Assert.That(group.SelectedToggle, Is.SameAs(toggle), "a group must keep its selection unless it opted into being deselectable");
        }

        [Test]
        public void Deactivate_AToggleThatIsNotActive_LeavesTheGroupAlone()
        {
            var active = scene.Toggle(group);
            var other = scene.Toggle(group);
            group.Select(active);

            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            group.Deselect(other);

            Assert.That(group.SelectedToggle, Is.SameAs(active));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Deactivate_AnnouncesTheChange()
        {
            var deselectable = scene.Group(isDeselectable: true);
            var toggle = scene.Toggle(deselectable);
            deselectable.Select(toggle);

            var changes = 0;
            deselectable.OnGroupChanged += _ => changes++;

            deselectable.Deselect(toggle);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Deactivate_RemembersTheToggleItCleared()
        {
            var deselectable = scene.Group(isDeselectable: true);
            var first = scene.Toggle(deselectable);
            var second = scene.Toggle(deselectable);

            deselectable.Select(first);
            deselectable.Select(second);

            deselectable.Deselect(second);

            Assert.That(deselectable.PreviouslySelectedToggle, Is.SameAs(second),
                "the toggle that just went off is the one a restore has to bring back");
        }

        [Test]
        public void Deactivate_Null_LeavesTheGroupAlone_EvenWhenNothingIsActive()
        {
            var changes = 0;
            group.OnGroupChanged += _ => changes++;

            Assert.That(() => group.Deselect(null), Throws.Nothing);
            Assert.That(changes, Is.Zero, "a null toggle must not be read as 'the (null) active toggle switched off'");
        }

        /// <summary>The membership guard (issue: a hand-edited/reparented toggle left a
        /// foreign group's <c>ActivatedToggle</c> pointing at it). A group can only ever
        /// activate its own child.</summary>
        [Test]
        public void Activate_AToggleThatBelongsToAnotherGroup_IsIgnored()
        {
            var otherGroup = scene.Group();
            var foreign = scene.Toggle(otherGroup);

            group.Select(foreign);

            Assert.That(group.SelectedToggle, Is.Null, "a group can only activate its own members");
        }

        [Test]
        public void SetToggle_False_OnTheActiveToggle_AnnouncesTheChange()
        {
            var deselectable = scene.Group(isDeselectable: true);
            var toggle = scene.Toggle(deselectable);
            toggle.SetToggle(true);

            var changes = 0;
            deselectable.OnGroupChanged += _ => changes++;

            toggle.SetToggle(false);

            Assert.That(changes, Is.EqualTo(1),
                "the toggle switching itself off is a real 'no panel open' state change, "
                + "whichever of click / hotkey / script routed it through SetToggle, as long as the group is deselectable");
        }

        /// <summary>The self-heal counterpart to the membership guard: a reference that was
        /// never produced by <see cref="RadioGroup.Select"/> — e.g. a stale value left over
        /// from a reparent, or hand-edited directly in the Inspector — is cleared the next
        /// time the Editor validates the group, rather than persisting indefinitely.</summary>
        [Test]
        public void OnValidate_AnActivatedToggleThatIsNoLongerAMember_IsCleared()
        {
            var otherGroup = scene.Group();
            var foreign = scene.Toggle(otherGroup);
            UiTestScene.SetObject(group, "<SelectedToggle>k__BackingField", foreign);

            InvokeOnValidate(group);

            Assert.That(group.SelectedToggle, Is.Null);
        }

        [Test]
        public void OnValidate_APreviouslyActivatedToggleThatIsNoLongerAMember_IsCleared()
        {
            var otherGroup = scene.Group();
            var foreign = scene.Toggle(otherGroup);
            UiTestScene.SetObject(group, "<PreviouslySelectedToggle>k__BackingField", foreign);

            InvokeOnValidate(group);

            Assert.That(group.PreviouslySelectedToggle, Is.Null);
        }

        [Test]
        public void OnValidate_DoesNotClearAGenuineMember()
        {
            var toggle = scene.Toggle(group);
            group.Select(toggle);

            InvokeOnValidate(group);

            Assert.That(group.SelectedToggle, Is.SameAs(toggle));
        }

        private static void InvokeOnValidate(RadioGroup target) =>
            typeof(RadioGroup)
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(target, null);
    }
}
