namespace Submodules.Utility.Persistence
{
    /// <summary>
    /// Turns a plain object into text and back. The seam that keeps the slot independent of the
    /// format: <see cref="JsonUtilitySerializer"/> is the adapter today, and moving to another
    /// library is one new implementation.
    /// </summary>
    public interface ISaveSerializer
    {
        string Serialize<T>(T value);

        /// <summary>Throws when <paramref name="text"/> is not a valid <typeparamref name="T"/>.
        /// May return null for empty text.</summary>
        T Deserialize<T>(string text);
    }
}
