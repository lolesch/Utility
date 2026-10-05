using System;

namespace Submodules.Utility.Persistence
{
    /// <summary>
    /// What a slot writes: the schema version the payload was saved under, when, and the payload as
    /// the serializer's own text. The payload stays text so a migration step can rewrite it before
    /// any payload type is asked for, and so a file from a newer version is judged by its header alone.
    /// </summary>
    [Serializable]
    internal sealed class SaveEnvelope
    {
        public int schemaVersion;
        public string savedAtUtc;
        public string payload;
    }
}
