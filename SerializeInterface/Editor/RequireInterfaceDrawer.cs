using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Submodules.Utility.SerializeInterface.Editor
{
    [CustomPropertyDrawer( typeof(RequireInterfaceAttribute) )]
    internal sealed class RequireInterfaceDrawer : PropertyDrawer
    {
        private RequireInterfaceAttribute RequireInterfaceAttribute => (RequireInterfaceAttribute) attribute;

        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
        {
            var requiredInterfaceType = RequireInterfaceAttribute.InterfaceType;
            _ = EditorGUI.BeginProperty( position, label, property );

            if ( property.isArray && property.propertyType == SerializedPropertyType.Generic )
                DrawArrayField( position, property, label, requiredInterfaceType );
            else
                DrawInterfaceObjectField( position, property, label, requiredInterfaceType );

            EditorGUI.EndProperty();
            var args = new InterfaceArgs( GetTypeOrElementType( fieldInfo.FieldType ), requiredInterfaceType );
            InterfaceReferenceUtil.OnGUI( position, property, label, args );
        }

        private void DrawArrayField( Rect position, SerializedProperty property, GUIContent label, Type interfaceType )
        {
            property.arraySize = EditorGUI.IntField(
                new Rect( position.x, position.y, position.width, EditorGUIUtility.singleLineHeight ),
                label.text + " Size", property.arraySize );

            var yOffset = EditorGUIUtility.singleLineHeight;
            for ( var i = 0; i < property.arraySize; i++ )
            {
                var element = property.GetArrayElementAtIndex( i );
                var elementRect = new Rect( position.x, position.y + yOffset, position.width,
                    EditorGUIUtility.singleLineHeight );
                DrawInterfaceObjectField( elementRect, element, new GUIContent( $"Element {i}" ), interfaceType );
                yOffset += EditorGUIUtility.singleLineHeight;
            }
        }

        private void DrawInterfaceObjectField( Rect position, SerializedProperty property, GUIContent label,
            Type interfaceType )
        {
            var oldReference = property.objectReferenceValue;
            var baseType = GetAssignableBaseType( fieldInfo.FieldType, interfaceType );
            var newReference = EditorGUI.ObjectField( position, label, oldReference, baseType, true );

            if ( newReference != null && newReference != oldReference )
                ValidateAndAssignObject( property, newReference, interfaceType );
            else if ( newReference == null ) property.objectReferenceValue = null;
        }

        private Type GetAssignableBaseType( Type fieldType, Type interfaceType )
        {
            var elementType = fieldType.IsArray
                ? fieldType.GetElementType()
                : fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>)
                    ? fieldType.GetGenericArguments()[0]
                    : fieldType;

            return interfaceType.IsAssignableFrom( elementType )
                ? elementType
                : typeof(ScriptableObject).IsAssignableFrom( elementType )
                    ? typeof(ScriptableObject)
                    : typeof(MonoBehaviour).IsAssignableFrom( elementType )
                        ? typeof(MonoBehaviour)
                        : typeof(Object);
        }

        private void ValidateAndAssignObject( SerializedProperty property, Object newReference, Type interfaceType )
        {
            if ( newReference is GameObject gameObject )
            {
                var component = gameObject.GetComponent( interfaceType );
                if ( component != null )
                {
                    property.objectReferenceValue = component;
                    return;
                }
            }
            else if ( interfaceType.IsAssignableFrom( newReference.GetType() ) )
            {
                property.objectReferenceValue = newReference;
                return;
            }

            Debug.LogWarning( $"The assigned object does not implement '{interfaceType.Name}'." );
            property.objectReferenceValue = null;
        }

        private Type GetTypeOrElementType( Type type )
        {
            return type.IsArray ? type.GetElementType() : type.IsGenericType ? type.GetGenericArguments()[0] : type;
        }
    }
}