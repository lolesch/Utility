using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Submodules.Utility.Tools.ShapeInspector.HexShape.Editor
{
    public abstract class HexShapeDrawer : PropertyDrawer
    {
        private static float LineHeight => EditorGUIUtility.singleLineHeight;

        private const float firstLineMargin = 5f;
        private const float lastLineMargin  = 2f;

        private static readonly Vector2 cellSpacing = new(5f, 2f);

        private SerializedProperty thisProperty;
        private SerializedProperty hexRadiusProperty;
        private SerializedProperty cellSizeProperty;
        private SerializedProperty rowsProperty;

        private int Diameter => hexRadiusProperty.intValue * 2 + 1;

        #region SerializedProperty getters

        private void GetHexRadiusProperty(SerializedProperty property) =>
            TryFindPropertyRelative(property, "radius", out hexRadiusProperty);

        private void GetCellSizeProperty(SerializedProperty property) =>
            TryFindPropertyRelative(property, "cellSize", out cellSizeProperty);

        private void GetCellsProperty(SerializedProperty property) =>
            TryFindPropertyRelative(property, "rows", out rowsProperty);

        #endregion

        #region Abstract and virtual methods

        protected virtual Vector2Int GetDefaultCellSizeValue() => new(32, 16);

        protected abstract object GetDefaultCellValue();
        protected abstract object GetCellValue(SerializedProperty cell);
        protected abstract void   SetValue(SerializedProperty cell, object obj);

        #endregion

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            thisProperty = property;

            GetHexRadiusProperty(property);
            GetCellSizeProperty(property);
            GetCellsProperty(property);

            if (hexRadiusProperty == null || cellSizeProperty == null || rowsProperty == null)
                return;

            // Auto-initialize if the rows array is empty or size-mismatched —
            // happens on fresh instances where Unity skips the parameterized constructor.
            if (rowsProperty.arraySize != hexRadiusProperty.intValue * 2 + 1)
                InitNewShape(hexRadiusProperty.intValue);

            if (cellSizeProperty.vector2IntValue == default)
                cellSizeProperty.vector2IntValue = GetDefaultCellSizeValue();

            position = EditorGUI.IndentedRect(position);

            EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position) { height = LineHeight };

            EditorGUI.indentLevel = 0;

            label.tooltip = $"Radius: {hexRadiusProperty.intValue}";

            const float resetButtonWidth = 52f;
            const float radiusFieldWidth = 56f;
            const float spacing = 4f;
            
            var foldRect = new Rect(foldoutRect) { width = foldoutRect.width - radiusFieldWidth - resetButtonWidth - spacing * 3 };
            var radiusRect = new Rect(foldoutRect) { x = foldRect.xMax + spacing, width = radiusFieldWidth };
            var resetRect  = new Rect(foldoutRect) { x = radiusRect.xMax + spacing, width = resetButtonWidth };
            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);

            EditorGUI.BeginChangeCheck();
            var prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 14f;
            var newRadius = Mathf.Max(0, EditorGUI.IntField(radiusRect, "R", hexRadiusProperty.intValue));
            EditorGUIUtility.labelWidth = prevLabelWidth;
            if (EditorGUI.EndChangeCheck()) // guarded by Math.Max(0, newValue)
                InitNewShapeWithPreviousValues(newRadius);

            if (GUI.Button(resetRect,    "Reset"))   OnReset();

            position.y += LineHeight;

            if (property.isExpanded)
            {
                position.y += firstLineMargin;
                DisplayShape(position);
            }

            EditorGUI.EndProperty();
        }

        private void OnReset()         => InitNewShape(hexRadiusProperty.intValue);
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = base.GetPropertyHeight(property, label);

            GetHexRadiusProperty(property);
            GetCellSizeProperty(property);

            if (property.isExpanded)
            {
                height += firstLineMargin;
                height += Diameter * (cellSizeProperty.vector2IntValue.y + cellSpacing.y) - cellSpacing.y;
                height += lastLineMargin;
            }

            return height;
        }

        private void InitNewShapeWithPreviousValues(int newRadius)
        {
            var previousValues     = GetValues();
            var previousDiameter = Diameter;
            var diameter         = newRadius * 2 + 1;

            InitNewShape(newRadius);
            
            // center-based previous values
            var offset = (diameter - previousDiameter) / 2;
            for (var y = 0; y < diameter; y++)
            {
                var row = GetRowAt(y);
                for (var x = 0; x < diameter; x++)
                {
                    var cell = row.GetArrayElementAtIndex(x);
                    var py   = y - offset;
                    var px   = x - offset;
                    if (px >= 0 && py >= 0 && px < previousDiameter && py < previousDiameter)
                        SetValue(cell, previousValues[py][px]);
                }
            }

            thisProperty.serializedObject.ApplyModifiedProperties();
        }

        private void InitNewShape(int newRadius)
        {
            rowsProperty.ClearArray();

            var diameter = newRadius * 2 + 1;

            for (var y = 0; y < diameter; y++)
            {
                rowsProperty.InsertArrayElementAtIndex(y);
                var row = GetRowAt(y);
                row.ClearArray();

                for (var x = 0; x < diameter; x++)
                {
                    row.InsertArrayElementAtIndex(x);
                    var cell = row.GetArrayElementAtIndex(x);
                    SetValue(cell, GetDefaultCellValue());
                }
            }

            hexRadiusProperty.intValue = newRadius;
            thisProperty.serializedObject.ApplyModifiedProperties();
        }

        private object[][] GetValues()
        {
            var arr = new object[Diameter][];
            for (var y = 0; y < Diameter; y++)
            {
                arr[y] = new object[Diameter];
                for (var x = 0; x < Diameter; x++)
                    arr[y][x] = GetCellValue(GetRowAt(y).GetArrayElementAtIndex(x));
            }
            return arr;
        }

        private void DisplayShape(Rect position)
        {
            var cellRect = new Rect(position.x, position.y,
                cellSizeProperty.vector2IntValue.x,
                cellSizeProperty.vector2IntValue.y);

            for (var y = 0; y < Diameter; y++)
            {
                for (var x = 0; x < Diameter; x++)
                {
                    var rowOffset = (cellRect.width + cellSpacing.x) / 2 * y;
                    var pos = new Rect(cellRect)
                    {
                        x = cellRect.x + (cellRect.width + cellSpacing.x) * x + rowOffset,
                        y = cellRect.y + (cellRect.height + cellSpacing.y) * y
                    };

                    var centerIndex = hexRadiusProperty.intValue;

                    if (x + y < centerIndex || x + y >= Diameter + centerIndex)
                    {
                        EditorGUI.DrawRect(pos, Color.clear);
                        continue;
                    }

                    var prop = GetRowAt(y).GetArrayElementAtIndex(x);

                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var match = Regex.Match(prop.type, @"PPtr<\$(.+)>");
                        if (match.Success)
                        {
                            var objectType   = match.Groups[1].ToString();
                            var assemblyName = "UnityEngine";
                            EditorGUI.ObjectField(pos, prop,
                                Type.GetType($"{assemblyName}.{objectType}, {assemblyName}"), GUIContent.none);
                        }
                    }
                    else
                        EditorGUI.PropertyField(pos, prop, GUIContent.none);
                }
            }
        }

        private SerializedProperty GetRowAt(int idx) =>
            rowsProperty.GetArrayElementAtIndex(idx).FindPropertyRelative("row");

        private void TryFindPropertyRelative(SerializedProperty parent, string relativePropertyPath,
            out SerializedProperty prop)
        {
            prop = parent.FindPropertyRelative(relativePropertyPath);
            if (prop == null)
                Debug.LogError($"Couldn't find variable \"{relativePropertyPath}\" in {parent.name}");
        }
    }

    [CustomPropertyDrawer(typeof(HexShapeBool))]
    public class HexShapeBoolDrawer : HexShapeDrawer
    {
        protected override Vector2Int GetDefaultCellSizeValue() => new(16, 16);

        protected override object GetDefaultCellValue()  => false;

        protected override object GetCellValue(SerializedProperty cell) => cell.boolValue;

        protected override void SetValue(SerializedProperty cell, object obj) => cell.boolValue = (bool)obj;
    }

    public class HexShapeEnumDrawer<T> : HexShapeDrawer where T : Enum
    {
        protected override Vector2Int GetDefaultCellSizeValue() => new(64, 16);

        protected override object GetDefaultCellValue()  => 0;

        protected override object GetCellValue(SerializedProperty cell) => cell.enumValueIndex;

        protected override void SetValue(SerializedProperty cell, object obj) => cell.enumValueIndex = (int)obj;
    }
}