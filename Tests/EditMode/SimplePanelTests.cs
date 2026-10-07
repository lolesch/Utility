using NUnit.Framework;
using Submodules.Utility.Tests.TestSupport;
using Submodules.Utility.Tools.Tweening;
using Submodules.Utility.UI;
using UnityEngine;

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

        /// <summary>The Inspector's Expand / Collapse run in Edit Mode, where no tween is ever pumped: they
        /// must land at once, without starting a fade that would never finish.</summary>
        [TestCase("ExpandFromInspector", 1f)]
        [TestCase("CollapseFromInspector", 0f)]
        public void TheInspectorMenu_TogglesInstantly_InEditMode(string menuItem, float alpha)
        {
            if (alpha == 0f)
                panel.Appear(true);
            else
                panel.Disappear(true);

            _ = typeof(SimplePanel).GetMethod(menuItem, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(panel, null);

            Assert.That(panel.GetComponent<CanvasGroup>().alpha, Is.EqualTo(alpha));
            Assert.That(Tween.ActiveCount, Is.Zero);
        }

        /// <summary>A hidden panel's contents must be out of reach of a hotkey as well as of a
        /// click: <c>blocksRaycasts</c> stops only the second, whereas a <c>Selectable</c> below
        /// asks <c>IsInteractable()</c>, which reads the group's <c>interactable</c>.</summary>
        [Test]
        public void Disappear_TakesTheContentsOutOfReach_OfAClickAndAHotkeyAlike()
        {
            panel.Appear(true);

            panel.Disappear(true);

            var group = panel.GetComponent<CanvasGroup>();
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(group.interactable, Is.False);
        }

        /// <summary>The consumer side of the same fact: a <c>Selectable</c> below a hidden panel reports
        /// itself non-interactable, which is what silences a hotkey toggle that polls
        /// <c>IsInteractable()</c>.</summary>
        [Test]
        public void Disappear_MakesASelectableBelow_ReportNonInteractable()
        {
            var below = scene.Element<SpyToggle>(parent: panel.transform);
            panel.Appear(true);
            Assume.That(below.IsInteractable(), Is.True);

            panel.Disappear(true);

            Assert.That(below.IsInteractable(), Is.False);
        }

        [Test]
        public void Appear_PutsTheContentsBackInReach()
        {
            panel.Disappear(true);

            panel.Appear(true);

            var group = panel.GetComponent<CanvasGroup>();
            Assert.That(group.blocksRaycasts, Is.True);
            Assert.That(group.interactable, Is.True);
        }
    }
}
