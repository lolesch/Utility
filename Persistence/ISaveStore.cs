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

        /// <summary>
        /// Moves the value under <paramref name="key"/>, and its backup, out of the way so neither is read as
        /// a save again, and keeps their text where a person can find it. Never deletes: a damaged save is
        /// evidence. Setting aside twice keeps both. False when the key had no current value.
        /// </summary>
        bool SetAside(string key);

        /// <summary>
        /// Appends <paramref name="text"/> to a side file named <paramref name="fileName"/> beside the
        /// saves, creating it on the first call. A side file is not a save: it is never listed by
        /// <see cref="Keys"/> and nothing in this interface reads it back. The name is a plain file name,
        /// never a path, and never ends in <c>.sav</c>.
        /// </summary>
        void AppendSideFile(string fileName, string text);
    }
}
