namespace Submodules.Utility.Bindings
{
    /// <summary>How a <see cref="IInputService{TAction}.Rebind"/> ended.</summary>
    public enum RebindResult
    {
        /// <summary>The player's key is bound and saved.</summary>
        Bound,

        /// <summary>Escape: nothing changed.</summary>
        Cancelled,

        /// <summary>The key is another action's: nothing changed, and the player should pick again.</summary>
        Conflict,
    }
}
