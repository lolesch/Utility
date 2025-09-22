using InventorySurvivor.Code.Utility.AttributeRef.Attributes;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace InventorySurvivor.Code.Utility.AttributeRef.Drawer
{
    [CustomPropertyDrawer( typeof(PreviewIconAttribute) )]
    public sealed class PreviewIconDrawer : PropertyDrawer
    {
        private const float _textureSize = 64;

        public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
        {
            return property.objectReferenceValue != null ? _textureSize : base.GetPropertyHeight( property, label );
        }

        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
        {
            position = EditorGUI.IndentedRect( position );

            _ = EditorGUI.BeginProperty( position, label, property );

            EditorGUI.indentLevel = 0;

            var labelRect = new Rect( position.x, position.y, EditorGUIUtility.labelWidth,
                EditorGUIUtility.singleLineHeight );

            GUI.Label( labelRect, property.displayName );

            var spriteRect = new Rect( labelRect.x + EditorGUIUtility.labelWidth, position.y, _textureSize,
                _textureSize );

            if ( property.objectReferenceValue != null )
            {
                property.objectReferenceValue =
                    EditorGUI.ObjectField( spriteRect, property.objectReferenceValue, typeof(Sprite), false );

                //if this is not a repaint or the property is null exit now
                if ( Event.current.type != EventType.Repaint || property.objectReferenceValue == null )
                    return;
            }
            else
            {
                _ = EditorGUI.PropertyField( spriteRect, property, true );
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif