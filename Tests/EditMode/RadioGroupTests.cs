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
    /// — the whole public surface since the membership list was removed.
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
            Assert.That(group.ActivatedToggle, Is.Null);
            Assert.That(group.PreviouslyActivatedToggle, Is.Null);
        }

        [Test]
        public void Activate_MakesThatToggleTheActiveOne()
        {
            var toggle = scene.Toggle(group);

            group.Activate(toggle);

            Assert.That(group.ActivatedToggle, Is.SameAs(toggle));
        }

        [Test]
        public void Activate_Null_LeavesTheGroupAlone()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);

            group.Activate(null);

            Assert.That(group.ActivatedToggle, Is.SameAs(toggle));
        }

        [Test]
        public void Activate_RemembersTheToggleItReplaced()
        {
            var first = scene.Toggle(group);
            var second = scene.Toggle(group);

            group.Activate(first);
            group.Activate(second);

            Assert.That(group.ActivatedToggle, Is.SameAs(second));
            Assert.That(group.PreviouslyActivatedToggle, Is.SameAs(first));
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
            group.OnGroupChanged += () => changes++;

            group.Activate(toggle);

            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Activate_AnnouncesTheChange()
        {
            var toggle = scene.Toggle(group);
            var changes = 0;
            group.OnGroupChanged += () => changes++;

            group.Activate(toggle);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Deactivate_ClearsTheActiveToggle()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);

            group.Deactivate(toggle);

            Assert.That(group.ActivatedToggle, Is.Null);
        }

        [Test]
        public void Deactivate_AToggleThatIsNotActive_LeavesTheGroupAlone()
        {
            var active = scene.Toggle(group);
            var other = scene.Toggle(group);
            group.Activate(active);

            var changes = 0;
            group.OnGroupChanged += () => changes++;

            group.Deactivate(other);

            Assert.That(group.ActivatedToggle, Is.SameAs(active));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Deactivate_AnnouncesTheChange()
        {
            var toggle = scene.Toggle(group);
            group.Activate(toggle);

            var changes = 0;
            group.OnGroupChanged += () => changes++;

            group.Deactivate(toggle);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Deactivate_RemembersTheToggleItCleared()
        {
            var first = scene.Toggle(group);
            var second = scene.Toggle(group);

            group.Activate(first);
            group.Activate(second);

            group.Deactivate(second);

            Assert.That(group.PreviouslyActivatedToggle, Is.SameAs(second),
                "the toggle that just went off is the one a restore has to bring back — "
                + "MultiplePanelToggle reads PreviouslyActivatedToggle to undo itself");
        }
    }
}
