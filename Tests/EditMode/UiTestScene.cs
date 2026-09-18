using System.Collections.Generic;
using Submodules.Utility.UI;
using Submodules.Utility.Tests.TestSupport;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// Scene scaffolding for the <see cref="InteractiveElement"/> family, which is
    /// <c>Selectable</c>-derived and therefore <c>[ExecuteAlways]</c> — <c>Awake</c> and
    /// <c>OnEnable</c> really do run in EditMode. Every element is built on a **deactivated**
    /// GameObject so <see cref="Selectable.targetGraphic"/> and <c>interactable</c> are in
    /// place before <c>Awake</c> sees them, exactly as scene deserialization would have it.
    ///
    /// Everything is parented under one root: <c>LogExtensions.MissingComponent</c> reads
    /// <c>transform.parent.name</c>, so a root-level element with no Graphic throws inside
    /// the logger rather than in the code under test.
    /// </summary>
    internal sealed class UiTestScene
    {
        private readonly List<GameObject> spawned = new();
        private readonly List<Object> throwaway = new();
        private readonly GameObject root;

        public UiTestScene()
        {
            root = new GameObject("ui-test-root", typeof(RectTransform), typeof(Canvas));
            spawned.Add(root);
        }

        public Transform Root => root.transform;

        /// <summary>An element wired the way the inspector would wire one.</summary>
        public T Element<T>(bool interactable = true, bool withGraphic = true, Transform parent = null,
            System.Type graphicType = null)
            where T : InteractiveElement
        {
            var go = new GameObject(typeof(T).Name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : root.transform, false);
            go.SetActive(false);

            var element = go.AddComponent<T>();

            if (withGraphic)
                element.targetGraphic = (Graphic)go.AddComponent(graphicType ?? typeof(Image));

            element.interactable = interactable;

            go.SetActive(true);
            spawned.Add(go);

            return element;
        }

        /// <summary>A <see cref="RadioGroup"/> whose children are the toggles it owns.</summary>
        public RadioGroup Group(bool isDeselectable = false)
        {
            var go = new GameObject("radio-group", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);

            var group = go.AddComponent<RadioGroup>();

            if (isDeselectable)
                SetBool(group, "<IsClearable>k__BackingField", true);

            spawned.Add(go);

            return group;
        }

        /// <summary>A <see cref="PanelGroup"/> whose children are the panels it owns.</summary>
        public PanelGroup PanelGroup(bool isClearable = false)
        {
            var go = new GameObject("panel-group", typeof(RectTransform));
            go.transform.SetParent(root.transform, false);

            var group = go.AddComponent<PanelGroup>();

            if (isClearable)
                SetBool(group, "<IsClearable>k__BackingField", true);

            spawned.Add(go);

            return group;
        }

        /// <summary>
        /// Writes a serialized field the way the inspector does. Used for authoring-only
        /// configuration whose C# setter is private — the real authoring seam, not reflection.
        /// </summary>
        public static void SetBool(Object target, string propertyPath, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyPath).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetObject(Object target, string propertyPath, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyPath).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Writes a serialized <c>List&lt;T&gt;</c> field the way the inspector
        /// does, entry by entry — including <c>null</c> entries, which a caller like
        /// <see cref="MultiplePanelToggle"/> is required to skip rather than throw on.</summary>
        public static void SetObjectList(Object target, string propertyPath, IReadOnlyList<Object> values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyPath);

            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>A toggle that belongs to <paramref name="group"/> by being its child —
        /// the same way <c>GetComponentInParent</c> resolves it in a real scene.</summary>
        public SpyToggle Toggle(RadioGroup group = null, bool interactable = true, System.Type graphicType = null) =>
            Element<SpyToggle>(interactable, parent: group != null ? group.transform : null, graphicType: graphicType);

        /// <summary>A panel with the components <see cref="SimplePanel"/> requires
        /// (<c>CanvasGroup</c>, <c>GraphicRaycaster</c>) already attached. Its <c>Awake</c>
        /// never runs — <see cref="SimplePanel"/> is not <c>[ExecuteAlways]</c> — so
        /// <c>FadeIn</c>/<c>FadeOut</c> are exercised exactly as a caller like
        /// <see cref="MultiplePanelToggle"/> would, with none of the panel's own startup
        /// side effects in the way.</summary>
        public SpyPanel Panel(PanelGroup group = null)
        {
            var go = new GameObject("panel", typeof(RectTransform), typeof(CanvasGroup), typeof(GraphicRaycaster));
            go.transform.SetParent(group != null ? group.transform : root.transform, false);

            var panel = go.AddComponent<SpyPanel>();
            spawned.Add(go);

            return panel;
        }

        /// <summary>A throwaway 1x1 sprite — the tests only care that it is not null.</summary>
        public Sprite Sprite()
        {
            var texture = new Texture2D(1, 1);
            var sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);

            throwaway.Add(texture);
            throwaway.Add(sprite);

            return sprite;
        }

        public static PointerEventData LeftClick() =>
            new(EventSystem.current) { button = PointerEventData.InputButton.Left };

        public void Dispose()
        {
            for (var i = spawned.Count - 1; 0 <= i; i--)
                if (spawned[i] != null)
                    Object.DestroyImmediate(spawned[i]);

            for (var i = throwaway.Count - 1; 0 <= i; i--)
                if (throwaway[i] != null)
                    Object.DestroyImmediate(throwaway[i]);

            spawned.Clear();
            throwaway.Clear();
        }
    }
}
