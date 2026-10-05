using System;

namespace Submodules.Utility.Persistence
{
    public enum LoadStatus
    {
        /// <summary>Nothing was ever saved under the key.</summary>
        Missing,
        /// <summary>The current file loaded.</summary>
        Loaded,
        /// <summary>The current file was unusable and the backup loaded in its place.</summary>
        RestoredFromBackup,
        /// <summary>Neither the current file nor the backup could be read.</summary>
        Corrupt,
        /// <summary>The file was written by a newer schema than this build knows. It is left untouched.</summary>
        NewerVersion
    }

    /// <summary>What a <see cref="ISaveSlot{T}"/> load found. <see cref="Payload"/> is only meaningful
    /// when <see cref="HasPayload"/>.</summary>
    public readonly struct LoadResult<T>
    {
        public LoadStatus Status { get; }
        public T Payload { get; }
        public DateTime SavedAtUtc { get; }

        /// <summary>Why the file was unusable. Null unless the status is Corrupt, or RestoredFromBackup
        /// (then it names why the current file was passed over).</summary>
        public string Failure { get; }

        public bool HasPayload => Status == LoadStatus.Loaded || Status == LoadStatus.RestoredFromBackup;

        public LoadResult(LoadStatus status, T payload = default, DateTime savedAtUtc = default, string failure = null)
        {
            Status = status;
            Payload = payload;
            SavedAtUtc = savedAtUtc;
            Failure = failure;
        }
    }
}
