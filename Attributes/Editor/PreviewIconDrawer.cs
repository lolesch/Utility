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
            property.serializedObject.Update();
            
            position = EditorGUI.IndentedRect(position);
            EditorGUI.BeginProperty(position, label, property);
            
            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            int controlID = property.propertyPath.GetHashCode();
            Sprite sprite = property.objectReferenceValue as Sprite;

            if ( !sprite )
            {
                Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(fieldRect, property, label);
            }
            else
            {
                Rect labelRect = new(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
                GUI.Label(labelRect, label);

                // Draw preview instead of picker
                Rect previewRect = new(labelRect.x + EditorGUIUtility.labelWidth, position.y, _textureSize, _textureSize);

                if (Event.current.type == EventType.Repaint)
                {
                    var tex = sprite.texture;
                    Rect texCoords = sprite.rect;
                    texCoords.x /= tex.width;
                    texCoords.y /= tex.height;
                    texCoords.width /= tex.width;
                    texCoords.height /= tex.height;

                    GUI.DrawTextureWithTexCoords(previewRect, tex, texCoords);
                    GUI.Box(previewRect, GUIContent.none, EditorStyles.helpBox);
                }

                // Open picker on click
                if ( Event.current.type == EventType.MouseDown &&
                     Event.current.button == 0 &&
                     previewRect.Contains(Event.current.mousePosition))
                {
                    EditorGUIUtility.ShowObjectPicker<Sprite>(sprite, false, null, controlID);
                    Event.current.Use();
                }
            }

            // Handle picker update or cleared selection
            if ((Event.current.commandName == "ObjectSelectorUpdated" ||
                 Event.current.commandName == "ObjectSelectorClosed") &&
                EditorGUIUtility.GetObjectPickerControlID() == controlID)
            {
                property.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject() as Sprite;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel = prevIndent;
            EditorGUI.EndProperty();
            
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif