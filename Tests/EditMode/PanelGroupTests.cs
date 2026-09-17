using System.Reflection;
using NUnit.Framework;
using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="PanelGroup"/> is <see cref="RadioGroup"/>'s counterpart for content rather
    /// than input: panels occupying the same screen space, where showing one hides whichever
    /// else is up. Deliberately driven the same way — <see cref="PanelGroup.Show"/> /
    /// <see cref="PanelGroup.Clear"/>, membership by hierarchy, an
    /// <see cref="PanelGroup.OnGroupChanged"/> announcement — so a caller who already knows
    /// <see cref="RadioGroup"/> reads this for free. The difference is what drives it: a
    /// toggle asking the group to show its panel is one caller, but not the only one —
    /// phase-change code can call <see cref="PanelGroup.Show"/> directly, with no toggle
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
            Assert.That(group.ActivePanel, Is.Null);
        }

        [Test]
        public void Show_MakesThatPanelTheActiveOne()
        {
            var panel = scene.Panel(group);

            group.Show(panel);

            Assert.That(group.ActivePanel, Is.SameAs(panel));
        }

        [Test]
        public void Show_FadesThePanelIn()
        {
            var panel = scene.Panel(group);

            group.Show(panel);

            Assert.That(panel.FadeInCalls, Is.EqualTo(1));
        }

        [Test]
        public void Show_Null_LeavesTheGroupAlone()
        {
            var panel = scene.Panel(group);
            group.Show(panel);

            group.Show(null);

            Assert.That(group.ActivePanel, Is.SameAs(panel));
        }

        [Test]
        public void Show_FadesTheReplacedPanelOut()
        {
            var first = scene.Panel(group);
            var second = scene.Panel(group);

            group.Show(first);
            group.Show(second);

            Assert.That(first.FadeOutCalls, Is.EqualTo(1));
            Assert.That(second.FadeInCalls, Is.EqualTo(1));
            Assert.That(group.ActivePanel, Is.SameAs(second));
        }

        [Test]
        public void Show_TheAlreadyActivePanel_ChangesNothing()
        {
            var panel = scene.Panel(group);
            group.Show(panel);

            var changes = 0;
            group.OnGroupChanged += () => changes++;

            group.Show(panel);

            Assert.That(changes, Is.Zero);
            Assert.That(panel.FadeInCalls, Is.EqualTo(1), "re-showing the active panel must not re-fade it");
        }

        [Test]
        public void Show_AnnouncesTheChange()
        {
            var panel = scene.Panel(group);
            var changes = 0;
            group.OnGroupChanged += () => changes++;

            group.Show(panel);

            Assert.That(changes, Is.EqualTo(1));
        }

        /// <summary>The membership guard, mirroring <c>RadioGroup.Select</c>: a group can
        /// only ever show its own child.</summary>
        [Test]
        public void Show_APanelThatBelongsToAnotherGroup_IsIgnored()
        {
            var otherGroup = scene.PanelGroup();
            var foreign = scene.Panel(otherGroup);

            group.Show(foreign);

            Assert.That(group.ActivePanel, Is.Null);
        }

        [Test]
        public void Clear_NotClearable_IsANoOp()
        {
            var panel = scene.Panel(group);
            group.Show(panel);

            group.Clear(panel);

            Assert.That(group.ActivePanel, Is.SameAs(panel), "the group must always keep a panel shown unless it opted into being clearable");
        }

        [Test]
        public void Clear_Clearable_ClearsTheActivePanel()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var panel = scene.Panel(clearable);
            clearable.Show(panel);

            clearable.Clear(panel);

            Assert.That(clearable.ActivePanel, Is.Null);
        }

        [Test]
        public void Clear_Clearable_FadesThePanelOut()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var panel = scene.Panel(clearable);
            clearable.Show(panel);

            clearable.Clear(panel);

            Assert.That(panel.FadeOutCalls, Is.EqualTo(1));
        }

        [Test]
        public void Clear_APanelThatIsNotActive_LeavesTheGroupAlone()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var active = scene.Panel(clearable);
            var other = scene.Panel(clearable);
            clearable.Show(active);

            var changes = 0;
            clearable.OnGroupChanged += () => changes++;

            clearable.Clear(other);

            Assert.That(clearable.ActivePanel, Is.SameAs(active));
            Assert.That(changes, Is.Zero);
        }

        [Test]
        public void Clear_AnnouncesTheChange()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var panel = scene.Panel(clearable);
            clearable.Show(panel);

            var changes = 0;
            clearable.OnGroupChanged += () => changes++;

            clearable.Clear(panel);

            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void Clear_Null_LeavesTheGroupAlone_EvenWhenNothingIsActive()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var changes = 0;
            clearable.OnGroupChanged += () => changes++;

            Assert.That(() => clearable.Clear(null), Throws.Nothing);
            Assert.That(changes, Is.Zero);
        }

        /// <summary>The convenience a caller like Go Venture needs: close whichever panel is
        /// open without first asking the group which one that is.</summary>
        [Test]
        public void ClearActive_ClearsWhicheverPanelIsShown()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var panel = scene.Panel(clearable);
            clearable.Show(panel);

            clearable.ClearActive();

            Assert.That(clearable.ActivePanel, Is.Null);
        }

        [Test]
        public void ClearActive_WhenNothingIsActive_IsANoOp()
        {
            var clearable = scene.PanelGroup(isClearable: true);
            var changes = 0;
            clearable.OnGroupChanged += () => changes++;

            Assert.That(() => clearable.ClearActive(), Throws.Nothing);
            Assert.That(changes, Is.Zero);
        }

        /// <summary>The self-heal counterpart to the membership guard, mirroring
        /// <c>RadioGroup.OnValidate</c>: a reference that is no longer one of the group's own
        /// children is cleared the next time the Editor validates the group.</summary>
        [Test]
        public void OnValidate_AnActivePanelThatIsNoLongerAMember_IsCleared()
        {
            var otherGroup = scene.PanelGroup();
            var foreign = scene.Panel(otherGroup);
            UiTestScene.SetObject(group, "<ActivePanel>k__BackingField", foreign);

            InvokeOnValidate(group);

            Assert.That(group.ActivePanel, Is.Null);
        }

        [Test]
        public void OnValidate_DoesNotClearAGenuineMember()
        {
            var panel = scene.Panel(group);
            group.Show(panel);

            InvokeOnValidate(group);

            Assert.That(group.ActivePanel, Is.SameAs(panel));
        }

        private static void InvokeOnValidate(PanelGroup target) =>
            typeof(PanelGroup)
                .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(target, null);
    }
}
