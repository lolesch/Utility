using System;
using System.Globalization;

namespace Submodules.Utility.Persistence
{
    /// <summary>
    /// A payload kept in one store key behind a versioned <see cref="SaveEnvelope"/>. A load tries
    /// the current file, then the backup, and says which one served it. An older schema is carried
    /// up through the <see cref="MigrationChain"/>; a newer one is reported and never touched.
    /// </summary>
    public sealed class SaveSlot<T> : ISaveSlot<T>
    {
        private enum Decoded { Ok, Bad, Newer }

        private const string SavedAtFormat = "o";

        private readonly ISaveStore store;
        private readonly ISaveSerializer serializer;
        private readonly string key;
        private readonly int currentVersion;
        private readonly MigrationChain migrations;
        private readonly Func<DateTime> utcNow;

        /// <param name="currentVersion">The schema version this build writes. At least 1.</param>
        /// <param name="migrations">Steps from older versions; none when the schema has never changed.</param>
        /// <param name="utcNow">The clock for the saved-at stamp; the system clock when null.</param>
        public SaveSlot(ISaveStore store, ISaveSerializer serializer, string key, int currentVersion,
            MigrationChain migrations = null, Func<DateTime> utcNow = null)
        {
            if (currentVersion < 1)
                throw new ArgumentOutOfRangeException(nameof(currentVersion), "A schema version starts at 1.");

            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.key = string.IsNullOrEmpty(key) ? throw new ArgumentException("A slot needs a key.", nameof(key)) : key;
            this.currentVersion = currentVersion;
            this.migrations = migrations ?? new MigrationChain();
            this.utcNow = utcNow ?? (() => DateTime.UtcNow);
        }

        public void Save(T payload)
        {
            var envelope = new SaveEnvelope
            {
                schemaVersion = currentVersion,
                savedAtUtc = utcNow().ToUniversalTime().ToString(SavedAtFormat, CultureInfo.InvariantCulture),
                payload = serializer.Serialize(payload)
            };

            store.Write(key, serializer.Serialize(envelope));
        }

        public LoadResult<T> Load()
        {
            if (!store.TryRead(key, out var currentText))
                return new LoadResult<T>(LoadStatus.Missing);

            switch (TryDecode(currentText, out var payload, out var savedAt, out var currentFailure))
            {
                case Decoded.Ok: return new LoadResult<T>(LoadStatus.Loaded, payload, savedAt);
                case Decoded.Newer: return new LoadResult<T>(LoadStatus.NewerVersion);
            }

            if (!store.TryReadBackup(key, out var backupText))
                return new LoadResult<T>(LoadStatus.Corrupt, failure: currentFailure);

            switch (TryDecode(backupText, out var restored, out var restoredAt, out var backupFailure))
            {
                case Decoded.Ok: return new LoadResult<T>(LoadStatus.RestoredFromBackup, restored, restoredAt, currentFailure);
                case Decoded.Newer: return new LoadResult<T>(LoadStatus.NewerVersion);
                default: return new LoadResult<T>(LoadStatus.Corrupt, failure: $"{currentFailure} The backup failed too: {backupFailure}");
            }
        }

        private Decoded TryDecode(string text, out T payload, out DateTime savedAtUtc, out string failure)
        {
            payload = default;
            savedAtUtc = default;
            failure = null;

            try
            {
                var envelope = serializer.Deserialize<SaveEnvelope>(text);

                if (envelope == null || envelope.schemaVersion < 1)
                {
                    failure = "The file has no schema version.";
                    return Decoded.Bad;
                }

                if (envelope.schemaVersion > currentVersion)
                    return Decoded.Newer;

                if (!DateTime.TryParseExact(envelope.savedAtUtc, SavedAtFormat, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out savedAtUtc))
                {
                    failure = "The file has no readable saved-at time.";
                    return Decoded.Bad;
                }

                var payloadText = migrations.Upgrade(envelope.payload, envelope.schemaVersion, currentVersion);
                payload = serializer.Deserialize<T>(payloadText);

                if (payload == null)
                {
                    failure = "The file has no payload.";
                    return Decoded.Bad;
                }

                return Decoded.Ok;
            }
            catch (Exception exception)
            {
                payload = default;
                failure = exception.Message;
                return Decoded.Bad;
            }
        }
    }
}
