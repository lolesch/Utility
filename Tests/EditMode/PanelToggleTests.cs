using NUnit.Framework;
using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="PanelToggle"/> fans a single toggle out to a single panel. <c>invert</c> is
    /// the one behavioural switch: without it the panel follows the toggle (an opened panel);
    /// with it the panel shows exactly while the toggle is off (an "other face" indicator,
    /// e.g. a two-way switch showing only the destination you are not currently at).
    /// </summary>
    [TestFixture]
    public sealed class PanelToggleTests
    {
        private UiTestScene scene;
        private PanelToggle toggle;

        [SetUp]
        public void SetUp()
        {
            scene = new UiTestScene();
            toggle = scene.Element<PanelToggle>();
        }

        [TearDown]
        public void TearDown() => scene.Dispose();

        [Test]
        public void SetToggle_True_FadesInThePanel()
        {
            var panel = scene.Panel();
            UiTestScene.SetObject(toggle, "panel", panel);

            toggle.SetToggle(true);

            Assert.That(panel.FadeInCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetToggle_False_FadesOutThePanel()
        {
            var panel = scene.Panel();
            UiTestScene.SetObject(toggle, "panel", panel);
            toggle.SetToggle(true);

            toggle.SetToggle(false);

            Assert.That(panel.FadeOutCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetToggle_True_WithInvert_FadesOutThePanel()
        {
            var panel = scene.Panel();
            UiTestScene.SetObject(toggle, "panel", panel);
            UiTestScene.SetBool(toggle, "invert", true);

            toggle.SetToggle(true);

            Assert.That(panel.FadeOutCalls, Is.EqualTo(1));
            Assert.That(panel.FadeInCalls, Is.Zero);
        }

        [Test]
        public void SetToggle_False_WithInvert_FadesInThePanel()
        {
            var panel = scene.Panel();
            UiTestScene.SetObject(toggle, "panel", panel);
            UiTestScene.SetBool(toggle, "invert", true);
            toggle.SetToggle(true);

            toggle.SetToggle(false);

            Assert.That(panel.FadeInCalls, Is.EqualTo(1));
        }

        [Test]
        public void SetToggle_WithNoPanel_DoesNotThrow()
        {
            Assert.That(() => toggle.SetToggle(true), Throws.Nothing);
        }
    }
}
