using UnityEngine;

namespace Submodules.Utility.Bindings
{
    /// <summary>Keeps the rebindings in <c>PlayerPrefs</c>: per machine, following the player rather than a save.</summary>
    public sealed class PlayerPrefsBindingStore : IBindingOverrideStore
    {
        private readonly string key;

        public PlayerPrefsBindingStore(string key) => this.key = key;

        public string Load() => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

        public void Save(string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
    }
}
