using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// The shared <see cref="Selectable"/> + <see cref="ISubmitHandler"/> wiring every
    /// button and toggle sits on: raycast-target enforcement, pointer / submit / select
    /// hooks. The serialized <see cref="tooltip"/> renders nowhere yet — an ambient
    /// tooltip host wires it later.
    /// </summary>
    [RequireComponent(typeof(GraphicRaycaster), typeof(CanvasRenderer))]
    public class InteractiveElement : Selectable, ISubmitHandler
    {
        // TODO: import and update tooltip from LOCA CSV Table
        [SerializeField, TextArea] private string tooltip = "";

        protected override void OnEnable()
        {
            base.OnEnable();

            if (targetGraphic)
                targetGraphic.raycastTarget = true;
            else
                LogExtensions.MissingComponent(nameof(Graphic), gameObject);
        }

        protected override void OnDisable() => base.OnDisable();

        public override void OnPointerEnter(PointerEventData eventData) => base.OnPointerEnter(eventData);

        public override void OnPointerExit(PointerEventData eventData) => base.OnPointerExit(eventData);

        public virtual void OnSubmit(BaseEventData eventData) { }
    }
}
