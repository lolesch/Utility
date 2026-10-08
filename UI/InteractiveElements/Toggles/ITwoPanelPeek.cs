namespace Submodules.Utility.UI
{
    /// <summary>
    /// What an owner needs of a peek: to tick it every frame and to give it back early. A
    /// <see cref="TwoPanelPeek"/> implements it; the component that owns one depends on this and not on
    /// the class.
    /// </summary>
    public interface ITwoPanelPeek
    {
        /// <summary>Evaluates the peek for this frame: begins it, ends it or leaves it be.</summary>
        void Tick();

        /// <summary>Gives the peek back now, as letting go of the key does: the app lost focus, or the
        /// owner is being disabled.</summary>
        void Release();
    }
}
