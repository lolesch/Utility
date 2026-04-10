using UnityEditor;
using UnityEngine;

namespace Submodules.Utility.Extensions
{
    public static class ScriptableObjectExtensions
    {
        public static void Save(this ScriptableObject obj, string path = "")
        {
#if UNITY_EDITOR
            if (path == "")
                path = $"Assets/Resources/{obj.name}.asset";
            
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset != null)
                Debug.LogError($"Saving would override {asset.GetType().Name} @ {path.ColoredComponent()} - If intended, remove it manually before saving!");
            
            AssetDatabase.CreateAsset(obj, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}
