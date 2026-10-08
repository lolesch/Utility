namespace Submodules.Utility.UI
{
    /// <summary>
    /// What a <see cref="TwoPanelPeek"/> needs of the driver of a two-panel switch: the bool, a count of
    /// every write to it, a count of its group's resets, and a way to write it from the group's side.
    /// <see cref="TwoPanelToggle"/> implements it; a peek depends on this and not on the toggle.
    /// </summary>
    public interface ITwoPanelDriver
    {
        /// <summary>The bool the two panels follow.</summary>
        bool IsOn { get; }

        /// <summary>How often the bool was written, whether it changed or not. Compare two reads: a
        /// click, the group's reset and a peek's own write all count.</summary>
        int Writes { get; }

        /// <summary>How often the driver's group reset itself on its panel closing, whether or not the
        /// reset moved the pair. Compare two reads: unlike <see cref="Writes"/> it also counts a reset
        /// that found the pair already home.</summary>
        int Resets { get; }

        /// <summary>Writes the bool from the group's side, the way state derived from elsewhere is: it
        /// succeeds even where the user's own click on the active tab is refused, and it works whichever
        /// of the pair's two buttons is the group's first member.</summary>
        void SetFromGroup(bool on);
    }
}
