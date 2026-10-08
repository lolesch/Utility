using System;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A peek: while a key is held and a panel is open it flips a two-panel switch to its other state,
    /// and on release it flips it back - unless something else wrote the bool since the peek began. It is
    /// a real selection made through the group, not a view-only overlay, so both tab buttons follow; it
    /// touches the driver's bool and nothing else.
    ///
    /// <para>The restore is "nothing else wrote the bool, and the group did not reset": the driver counts
    /// its writes (<see cref="ITwoPanelDriver.Writes"/>) and its group's resets
    /// (<see cref="ITwoPanelDriver.Resets"/>), and the peek compares both counts after its own write with
    /// the counts on release. A click on the home tab is a write and so cancels the restore, with no case
    /// of its own; a click on the tab being peeked at is refused because it is already on, writes nothing,
    /// and the release returns home. A peek never becomes a choice.</para>
    ///
    /// <para>The panel closing is the second half, and it is the reset that is watched, not the panel. A
    /// reset is a write only where it moves the pair, and a peek that landed on the group's first member
    /// (which of the two buttons that is does not matter) leaves it nothing to move, so the write count
    /// cannot tell; the reset count can. A panel that closed and reset during the hold is where the player
    /// left it, and a release after the reopen leaves the pair where the reset (or the peek) put it. A
    /// frame where the panel is merely not open (an ancestor fading, another screen over it) without the
    /// reset is nothing, and the release still gives the tab back. A reopen inside the fade, before the
    /// reset has run, does likewise.</para>
    ///
    /// <para>The two answers it needs - whether the key is held, whether the panel is open - are
    /// injected, so a test drives it without a keyboard or a canvas. It begins on the first frame both
    /// hold; it ends only when the key is let go (or <see cref="Release"/> says the app lost focus), so a
    /// panel closed and reopened during one hold is not peeked at again, and a hold begun while the panel
    /// was closed peeks when it opens. Whether the panel is open gates only the beginning.</para>
    /// </summary>
    public sealed class TwoPanelPeek : ITwoPanelPeek
    {
        private readonly ITwoPanelDriver driver;
        private readonly Func<bool> keyHeld;
        private readonly Func<bool> panelOpen;

        private bool peekedThisHold;
        private bool restorePending;
        private bool homeState;
        private int writesAfterPeek;
        private int resetsAfterPeek;

        public TwoPanelPeek(ITwoPanelDriver driver, Func<bool> keyHeld, Func<bool> panelOpen)
        {
            this.driver = driver;
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

            if (peekedThisHold || !panelOpen())
                return;

            peekedThisHold = true;
            restorePending = true;
            homeState = driver.IsOn;
            driver.SetFromGroup(!homeState);
            writesAfterPeek = driver.Writes;
            resetsAfterPeek = driver.Resets;
        }

        /// <inheritdoc/>
        /// <remarks>A hold peeks once: it stays used up until <see cref="Tick"/> sees the key let go, so a
        /// key that is still reported held does not peek a second time.</remarks>
        public void Release()
        {
            if (restorePending && driver.Writes == writesAfterPeek && driver.Resets == resetsAfterPeek)
                driver.SetFromGroup(homeState);

            restorePending = false;
        }
    }
}
