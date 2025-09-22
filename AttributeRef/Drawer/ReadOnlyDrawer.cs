using InventorySurvivor.Code.Utility.AttributeRef.Attributes;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace InventorySurvivor.Code.Utility.AttributeRef.Drawer
{
    [CustomPropertyDrawer( typeof(ReadOnlyAttribute) )]
    public sealed class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
        {
            GUI.enabled = false;
            _ = EditorGUI.PropertyField( position, property, label, true );
            GUI.enabled = true;
        }

        public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
        {
            return EditorGUI.GetPropertyHeight( property, label, true );
        }
    }
}
#endif