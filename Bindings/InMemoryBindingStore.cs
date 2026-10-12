namespace Submodules.Utility.Bindings
{
    /// <summary>A store that lives and dies with the object: the test double, and the default for a
    /// session that must not read or write the player's own rebindings.</summary>
    public sealed class InMemoryBindingStore : IBindingOverrideStore
    {
        /// <summary>The JSON as last saved, or null. A test may set it to start from a saved state.</summary>
        public string Json { get; set; }

        /// <summary>How many times it was saved, for a test to read.</summary>
        public int Saves { get; private set; }

        public string Load() => Json;

        public void Save(string json)
        {
            Json = json;
            Saves++;
        }
    }
}
