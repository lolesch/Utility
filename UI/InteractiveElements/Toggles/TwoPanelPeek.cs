using System;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A peek: while a key is held and a panel is open it switches a two-panel pair to its other button,
    /// and on release it switches back. It is a click made through the group, so both buttons follow and
    /// the panels with them, and it does not care which of the two is the group's first member.
    ///
    /// <para>It gives the button back only while the pair is still where the peek put it. A click on the
    /// home button moves the pair on and the peek stands down; a click on the button being peeked at is
    /// refused because it is already on, and the release returns home. The group resetting on its panel
    /// closing (<see cref="AbstractGroup{TMember}.WasReset"/>) stands the peek down too, even where it had
    /// landed on the group's first member and the reset found nothing to move. A peek never becomes a
    /// choice.</para>
    ///
    /// <para>The two answers it needs - whether the key is held, whether the panel is open - are
    /// injected, so a test drives it without a keyboard or a canvas. It begins on the first frame both
    /// hold; it ends only when the key is let go (or <see cref="Release"/> says the app lost focus), so a
    /// panel closed and reopened during one hold is not peeked at again, and a hold begun while the panel
    /// was closed peeks when it opens. Whether the panel is open gates only the beginning: a frame where
    /// it is merely not open (an ancestor fading, another screen over it) without the reset is nothing.</para>
    /// </summary>
    public sealed class TwoPanelPeek : ITwoPanelPeek
    {
        private readonly TwoPanelToggle driver;
        private readonly TwoPanelMirrorToggle mirror;
        private readonly Func<bool> keyHeld;
        private readonly Func<bool> panelOpen;

        private ToggleGroup group;
        private AbstractToggle home;
        private AbstractToggle away;
        private bool peeking;
        private bool peekedThisHold;

        public TwoPanelPeek(TwoPanelToggle driver, TwoPanelMirrorToggle mirror, Func<bool> keyHeld, Func<bool> panelOpen)
        {
            this.driver = driver;
            this.mirror = mirror;
            this.keyHeld = keyHeld;
            this.panelOpen = panelOpen;
        }

        /// <inheritdoc/>
        /// <remarks>Evaluates "key held and panel open" for this frame.</remarks>
        public void Tick()
        {
            if (!keyHeld())
            {
                Release();
                peekedThisHold = false;
                return;
            }

            if (peeking && group.ActiveMember != away)
                StandDown();

            if (peekedThisHold || !panelOpen() || !driver.RadioGroup)
                return;

            peekedThisHold = true;
            peeking = true;
            group = driver.RadioGroup;
            home = driver.IsOn ? driver : mirror;
            away = driver.IsOn ? mirror : driver;
            group.WasReset += StandDown;
            away.SetToggle(true);
        }

        /// <inheritdoc/>
        /// <remarks>A hold peeks once: it stays used up until <see cref="Tick"/> sees the key let go, so a
        /// key that is still reported held does not peek a second time.</remarks>
        public void Release()
        {
            if (!peeking)
                return;

            var stillAway = group.ActiveMember == away;

            StandDown();

            if (stillAway)
                home.SetToggle(true);
        }

        private void StandDown()
        {
            peeking = false;
            group.WasReset -= StandDown;
        }
    }
}
