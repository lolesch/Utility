using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Submodules.Utility.Attributes.Editor
{
    [CustomPropertyDrawer( typeof(PreviewIconAttribute) )]
    public sealed class PreviewIconDrawer : PropertyDrawer
    {
        private const float _textureSize = 64;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => 
            property.objectReferenceValue is Sprite ? _textureSize : EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            if (property.objectReferenceValue == null)
                EditorGUI.PropertyField(position, property, label);
            else
            {
                Rect incented = EditorGUI.PrefixLabel(position, label);
             
                int prevIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                int controlID = GUIUtility.GetControlID( FocusType.Passive );
                Sprite sprite = property.objectReferenceValue as Sprite;
                
                Rect previewRect = new Rect(incented.x, incented.y, _textureSize, _textureSize);
                
                // Draw preview
                if (Event.current.type == EventType.Repaint)
                {
                    var tex = sprite.texture;
                    if (tex != null)
                    {
                        Rect texCoords = sprite.rect;
                        texCoords.x /= tex.width;
                        texCoords.y /= tex.height;
                        texCoords.width /= tex.width;
                        texCoords.height /= tex.height;

                        GUI.DrawTextureWithTexCoords(previewRect, tex, texCoords);
                    }
                    EditorStyles.helpBox.Draw(previewRect, false, false, false, false);
                }
                
                // Open picker on click
                if (Event.current.type == EventType.MouseDown &&
                    Event.current.button == 0 &&
                    previewRect.Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.ShowObjectPicker<Sprite>(sprite, false, null, controlID);
                    Event.current.Use();
                }
                
                // Handle picker update or cleared selection
                if (Event.current.type == EventType.ExecuteCommand && 
                    EditorGUIUtility.GetObjectPickerControlID() == controlID &&
                    Event.current.commandName == "ObjectSelectorUpdated")
                {
                    property.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject() as Sprite;
                    property.serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUI.indentLevel = prevIndent;
            }
            
            EditorGUI.EndProperty();
        }
    }
}
#endif