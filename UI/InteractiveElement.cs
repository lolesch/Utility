using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// The shared <see cref="Selectable"/> + <see cref="ISubmitHandler"/> wiring every
    /// button and toggle sits on: raycast-target enforcement, pointer / submit / select
    /// hooks. When <see cref="tooltip"/> is non-empty it requests a hint from the ambient
    /// <see cref="TooltipHost{T}.Current"/> on pointer-enter / select and hides it on
    /// exit / deselect; with no host in the scene the calls are inert.
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

        protected override void OnDisable()
        {
            base.OnDisable();

            HideTooltip();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            ShowTooltip();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            HideTooltip();
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);

            ShowTooltip();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);

            HideTooltip();
        }

        public virtual void OnSubmit(BaseEventData eventData) { }

        private void ShowTooltip()
        {
            if (string.IsNullOrEmpty(tooltip))
                return;

            TooltipHost.Current?.Show(tooltip);
        }

        private void HideTooltip()
        {
            if (string.IsNullOrEmpty(tooltip))
                return;

            TooltipHost.Current?.Hide();
        }
    }
}
