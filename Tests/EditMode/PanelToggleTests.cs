using NUnit.Framework;
using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="PanelToggle"/> fans a single toggle out to a single panel: the panel follows
    /// the toggle 1:1. A toggle needing the opposite mapping derives <see cref="AbstractToggle"/>
    /// directly instead (see <c>SidePanelToggle</c>).
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
        public void SetToggle_WithNoPanel_DoesNotThrow()
        {
            Assert.That(() => toggle.SetToggle(true), Throws.Nothing);
        }
    }
}
