using System;
using System.Collections.Generic;

namespace Submodules.Utility.Persistence
{
    /// <summary>A store that lives and dies with the object: the test double, and a throwaway store
    /// for a session that must not touch disk. It keeps the backup rule of every store.</summary>
    public sealed class InMemorySaveStore : ISaveStore
    {
        private readonly Dictionary<string, string> current = new();
        private readonly Dictionary<string, string> backups = new();

        public bool TryRead(string key, out string content) => current.TryGetValue(Checked(key), out content);

        public bool TryReadBackup(string key, out string content) => backups.TryGetValue(Checked(key), out content);

        public void Write(string key, string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (current.TryGetValue(Checked(key), out var previous))
                backups[key] = previous;

            current[key] = content;
        }

        public bool Delete(string key)
        {
            var hadCurrent = current.Remove(Checked(key));
            var hadBackup = backups.Remove(key);
            return hadCurrent || hadBackup;
        }

        public IReadOnlyCollection<string> Keys() => new List<string>(current.Keys);

        private static string Checked(string key) =>
            string.IsNullOrEmpty(key) ? throw new ArgumentException("A save key cannot be empty.", nameof(key)) : key;
    }
}
