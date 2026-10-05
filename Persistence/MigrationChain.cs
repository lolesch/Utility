using System;
using System.Collections.Generic;

namespace Submodules.Utility.Persistence
{
    /// <summary>
    /// The registered steps that carry an old payload to the current schema. A step is keyed by
    /// the version it upgrades <i>from</i> and yields the payload text of the next version, so a
    /// payload several versions behind runs every step in between, in order. A step works on text
    /// and uses the serializer for the old and new shapes it knows; the chain names no payload type.
    /// </summary>
    public sealed class MigrationChain
    {
        private readonly Dictionary<int, Func<string, string>> steps = new();

        /// <summary>Registers the step from <paramref name="fromVersion"/> to the next version.
        /// Throws when that version already has a step.</summary>
        public MigrationChain Add(int fromVersion, Func<string, string> step)
        {
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            if (!steps.TryAdd(fromVersion, step))
                throw new InvalidOperationException($"A migration from version {fromVersion} is already registered.");

            return this;
        }

        /// <summary>Runs every step from <paramref name="fromVersion"/> up to <paramref name="toVersion"/>.
        /// Throws when a step in between is not registered.</summary>
        public string Upgrade(string payload, int fromVersion, int toVersion)
        {
            for (var version = fromVersion; version < toVersion; version++)
            {
                if (!steps.TryGetValue(version, out var step))
                    throw new InvalidOperationException($"No migration from version {version} to {version + 1} is registered.");

                payload = step(payload);
            }

            return payload;
        }
    }
}
