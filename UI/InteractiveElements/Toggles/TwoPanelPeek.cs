using System;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A peek: while a key is held and a panel is open it flips a two-panel switch to its other state,
    /// and on release it flips it back - unless something else wrote the bool since the peek began. It is
    /// a real selection made through the group, not a view-only overlay, so both tab buttons follow.
    ///
    /// <para>The restore is one rule, "nothing else wrote the bool": the driver counts its writes
    /// (<see cref="ITwoPanelDriver.Writes"/>) and the peek compares the count after its own write with the
    /// count on release. A click on the home tab and the group's reset on its panel closing are both
    /// writes and so cancel the restore, with no case of their own; a click on the tab being peeked at is
    /// refused because it is already on, writes nothing, and the release returns home. A peek never
    /// becomes a choice.</para>
    ///
    /// <para>The two answers it needs - whether the key is held, whether the panel is open - are
    /// injected, so a test drives it without a keyboard or a canvas. It begins on the first frame both
    /// hold; it ends only when the key is let go (or <see cref="Release"/> says the app lost focus), so a
    /// panel closed and reopened during one hold is not peeked at again, and a hold begun while the panel
    /// was closed peeks when it opens.</para>
    /// </summary>
    public sealed class TwoPanelPeek
    {
        private readonly ITwoPanelDriver driver;
        private readonly Func<bool> keyHeld;
        private readonly Func<bool> panelOpen;

        private bool spent;
        private bool restorePending;
        private bool home;
        private int writesAfterPeek;

        public TwoPanelPeek(ITwoPanelDriver driver, Func<bool> keyHeld, Func<bool> panelOpen)
        {
            this.driver = driver;
            this.keyHeld = keyHeld;
            this.panelOpen = panelOpen;
        }

        /// <summary>Evaluates "key held and panel open" for this frame.</summary>
        public void Tick()
        {
            if (!keyHeld())
            {
                Release();
                spent = false;
                return;
            }

            if (spent || !panelOpen())
                return;

            spent = true;
            restorePending = true;
            home = driver.IsOn;
            driver.SetFromGroup(!home);
            writesAfterPeek = driver.Writes;
        }

        /// <summary>Gives the peek back now, as a release does: the app lost focus, or the owner is
        /// being disabled. The hold stays spent until <see cref="Tick"/> sees the key let go, so a key that
        /// is still reported held does not peek a second time.</summary>
        public void Release()
        {
            if (restorePending && driver.Writes == writesAfterPeek)
                driver.SetFromGroup(home);

            restorePending = false;
        }
    }
}
