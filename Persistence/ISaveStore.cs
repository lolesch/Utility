using System.Collections.Generic;

namespace Submodules.Utility.Persistence
{
    /// <summary>
    /// Where saved text lives, by key. The store knows nothing about what the text means: the
    /// <see cref="SaveSlot{T}"/> above it owns the envelope, the version and the status. A store
    /// keeps one backup per key, so a damaged current value can still be recovered.
    /// </summary>
    public interface ISaveStore
    {
        /// <summary>The current value under <paramref name="key"/>. False when there is none.</summary>
        bool TryRead(string key, out string content);

        /// <summary>The value the current one replaced. False when the key was written once or never.</summary>
        bool TryReadBackup(string key, out string content);

        /// <summary>Makes <paramref name="content"/> the current value. The value it replaces becomes
        /// the backup. A throw leaves the previous current value and backup as they were.</summary>
        void Write(string key, string content);

        /// <summary>Removes the current value and its backup. False when the key had neither.</summary>
        bool Delete(string key);

        /// <summary>Every key that has a current value.</summary>
        IReadOnlyCollection<string> Keys();
    }
}
