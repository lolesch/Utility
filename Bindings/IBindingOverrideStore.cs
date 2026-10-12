namespace Submodules.Utility.Bindings
{
    /// <summary>Where the player's rebindings are kept between runs, as the Input System's override JSON.</summary>
    public interface IBindingOverrideStore
    {
        /// <summary>The saved JSON, or null when nothing was ever saved.</summary>
        string Load();

        void Save(string json);
    }
}
