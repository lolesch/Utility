namespace Submodules.Utility.Persistence
{
    /// <summary>One saved payload of type <typeparamref name="T"/> under one key, behind the envelope.</summary>
    public interface ISaveSlot<T>
    {
        /// <summary>Writes <paramref name="payload"/> under the current schema version. A throw from
        /// the store leaves the previous file as it was.</summary>
        void Save(T payload);

        /// <summary>Reads, migrates and decodes the slot. Never writes: a damaged or newer file is
        /// reported, not repaired.</summary>
        LoadResult<T> Load();
    }
}
