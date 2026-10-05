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
        private readonly List<string> setAside = new();
        private readonly Dictionary<string, string> sideFiles = new();

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

        /// <summary>What <see cref="SetAside"/> took out of the way: one entry per set-aside value, current and backup alike.</summary>
        public IReadOnlyList<string> SetAsideValues => setAside;

        /// <summary>The side files by name, as appended. For a test to read; the interface cannot.</summary>
        public IReadOnlyDictionary<string, string> SideFiles => sideFiles;

        public bool SetAside(string key)
        {
            if (!current.TryGetValue(Checked(key), out var value))
                return false;

            setAside.Add(value);
            _ = current.Remove(key);

            if (backups.Remove(key, out var backup))
                setAside.Add(backup);

            return true;
        }

        public void AppendSideFile(string fileName, string text)
        {
            if (string.IsNullOrEmpty(fileName) || fileName.EndsWith(".sav", StringComparison.Ordinal))
                throw new ArgumentException($"'{fileName}' is not a usable side file name.", nameof(fileName));

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            sideFiles[fileName] = sideFiles.TryGetValue(fileName, out var existing) ? existing + text : text;
        }

        private static string Checked(string key) =>
            string.IsNullOrEmpty(key) ? throw new ArgumentException("A save key cannot be empty.", nameof(key)) : key;
    }
}
