using UnityEngine;

namespace Submodules.Utility.Persistence
{
    /// <summary>The Unity JSON serializer behind <see cref="ISaveSerializer"/>. It covers flat
    /// graphs of <c>[Serializable]</c> classes with public fields; no dictionaries, no polymorphism.</summary>
    public sealed class JsonUtilitySerializer : ISaveSerializer
    {
        public string Serialize<T>(T value) => JsonUtility.ToJson(value);

        public T Deserialize<T>(string text) => JsonUtility.FromJson<T>(text);
    }
}
