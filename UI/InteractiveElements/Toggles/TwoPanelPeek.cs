using System;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A peek: while a key is held and a panel is open it flips a two-panel switch to its other state,
    /// and on release it flips it back - unless something else wrote the bool since the peek began. It is
    /// a real selection made through the group, not a view-only overlay, so both tab buttons follow.
    ///
    /// <para>The restore is "nothing else wrote the bool, and the panel stayed open": the driver counts
    /// its writes (<see cref="ITwoPanelDriver.Writes"/>) and the peek compares the count after its own
    /// write with the count on release. A click on the home tab is a write and so cancels the restore,
    /// with no case of its own; a click on the tab being peeked at is refused because it is already on,
    /// writes nothing, and the release returns home. A peek never becomes a choice.</para>
    ///
    /// <para>The panel closing is the second half. Its group's reset is a write only where it moves the
    /// pair, and a peek that landed on the group's first member (which of the two buttons that is does
    /// not matter) leaves it nothing to move, so the count cannot tell. The peek therefore abandons its
    /// restore itself the first frame it sees the panel closed during the hold, and a release after the
    /// reopen leaves the pair where the reset (or the peek) put it. A reopen inside the fade, before the
    /// reset has run, thus keeps the peeked tab.</para>
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

            if (!panelOpen())
            {
                // The panel closing ends the peek. Its reset is no write where the peek already landed
                // on the group's first member, so the count alone would restore the tab the player left.
                restorePending = false;
                return;
            }

            if (spent)
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
