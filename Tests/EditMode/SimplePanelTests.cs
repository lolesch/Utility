using NUnit.Framework;
using Submodules.Utility.Tests.TestSupport;
using Submodules.Utility.Tools.Tweening;
using Submodules.Utility.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="SimplePanel"/>'s fade primitives, pinned at the one place they can go wrong
    /// without anybody noticing: a fade that is interrupted by the opposite fade, or by the
    /// same one re-fired. Each of <see cref="SimplePanel.Appear"/> and
    /// <see cref="SimplePanel.Disappear"/> kills whatever is in flight before starting its
    /// own, because the predecessor's <c>OnComplete</c> writes <c>alpha</c> and
    /// <c>blocksRaycasts</c> — landing after the interruption it would undo it, leaving a
    /// panel that is visible but inert, or hidden but still swallowing clicks.
    ///
    /// <para>EditMode has no tween pump, so the observable is <see cref="Tween.ActiveCount"/>
    /// rather than the alpha a tick would produce: exactly one fade running is the same
    /// statement as "the stale <c>OnComplete</c> can no longer fire".</para>
    /// </summary>
    [TestFixture]
    public sealed class SimplePanelTests
    {
        private UiTestScene scene;
        private SpyPanel panel;

        [SetUp]
        public void SetUp()
        {
            Tween.KillAll();
            scene = new UiTestScene();
            panel = scene.Panel();
        }

        [TearDown]
        public void TearDown()
        {
            Tween.KillAll();
            scene.Dispose();
        }

        [Test]
        public void Appear_StartsAFade()
        {
            panel.Appear(false);

            Assert.That(Tween.ActiveCount, Is.EqualTo(1), "killing the old fade must not kill the new one");
        }

        [Test]
        public void Appear_KillsTheFadeOutItInterrupts()
        {
            panel.Disappear(false);

            panel.Appear(false);

            Assert.That(Tween.ActiveCount, Is.EqualTo(1),
                "the interrupted fade-out is still running, so its OnDisappear will set alpha 0 on a re-shown panel");
        }

        [Test]
        public void Disappear_KillsTheFadeInItInterrupts()
        {
            panel.Appear(false);

            panel.Disappear(false);

            Assert.That(Tween.ActiveCount, Is.EqualTo(1),
                "the interrupted fade-in is still running, so its OnAppear will re-arm blocksRaycasts on a hidden panel");
        }

        [Test]
        public void Appear_RefiredBeforeItFinishes_DoesNotStackFades()
        {
            panel.Appear(false);
            panel.Appear(false);
            panel.Appear(false);

            Assert.That(Tween.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Appear_Instant_LeavesNothingRunning()
        {
            panel.Disappear(false);

            panel.Appear(true);

            Assert.That(Tween.ActiveCount, Is.Zero, "an instant appear still has to clear the fade it replaces");
        }
    }
}
