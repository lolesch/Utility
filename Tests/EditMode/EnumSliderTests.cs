using NUnit.Framework;
using Submodules.Utility.Tests.TestSupport;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// Pins what <see cref="Submodules.Utility.UI.EnumSlider{TEnum}"/> derives so a concrete one
    /// authors nothing: one value per offered member, and the enum member (not its underlying
    /// number) behind a slider position.
    /// </summary>
    [TestFixture]
    public sealed class EnumSliderTests
    {
        private UiTestScene scene;

        [SetUp]
        public void SetUp() => scene = new UiTestScene();

        [TearDown]
        public void TearDown() => scene.Dispose();

        [Test]
        public void TheSteps_AreTheOfferedMembers()
        {
            var slider = scene.Element<SpyEnumSlider>();

            Assert.That(slider.Steps, Is.EqualTo(3), "four members are three intervals");
            Assert.That(slider.IsStepped, Is.True);
        }

        [Test]
        public void WithoutAnOverride_EveryMemberIsOffered()
        {
            var slider = scene.Element<SpyAllMembersEnumSlider>();

            Assert.That(slider.Steps, Is.EqualTo(4), "five members including None are four intervals");
        }

        [Test]
        public void EachPosition_SelectsTheMemberAtThatIndex_NotTheEnumNumber()
        {
            var slider = scene.Element<SpyEnumSlider>();

            slider.SetStepIndex(2);

            Assert.That(slider.Selected, Is.EqualTo(TestRarity.Rare), "index 2 -> Rare (20), not (TestRarity)2");
        }

        [Test]
        public void TheLastMember_SitsAtFull()
        {
            var slider = scene.Element<SpyEnumSlider>();

            slider.SetStepIndex(3);

            Assert.That(slider.Value, Is.EqualTo(1f));
            Assert.That(slider.Selected, Is.EqualTo(TestRarity.Unique));
        }

        [Test]
        public void ADragBetweenMembers_Snaps()
        {
            var slider = scene.Element<SpyEnumSlider>();

            slider.SetValue(.4f);

            Assert.That(slider.Selected, Is.EqualTo(TestRarity.Magic));
            Assert.That(slider.Value, Is.EqualTo(1f / 3f).Within(1e-5f));
        }

        [Test]
        public void TheReadout_IsTheMembersName()
        {
            var slider = scene.Element<SpyEnumSlider>();

            slider.SetValue(1f);

            Assert.That(slider.FormattedValue, Is.EqualTo("Unique"));
        }

        [Test]
        public void SetSelectedWithoutNotify_MovesToTheMemberSilently()
        {
            var slider = scene.Element<SpyEnumSlider>();
            var raised = 0;
            slider.OnValueChanged += _ => raised++;

            slider.SetSelectedWithoutNotify(TestRarity.Magic);

            Assert.That(slider.Selected, Is.EqualTo(TestRarity.Magic));
            Assert.That(raised, Is.Zero);
        }

        [Test]
        public void SetSelectedWithoutNotify_IgnoresAMemberTheSliderDoesNotOffer()
        {
            var slider = scene.Element<SpyEnumSlider>();
            slider.SetStepIndex(2);

            slider.SetSelectedWithoutNotify(TestRarity.None);

            Assert.That(slider.Selected, Is.EqualTo(TestRarity.Rare));
        }
    }
}
