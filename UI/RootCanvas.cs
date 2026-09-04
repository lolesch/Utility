using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    [RequireComponent(typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Canvas))]
    public class RootCanvas : MonoBehaviour
    {
        [SerializeField, ReadOnly] protected Canvas canvas;
        [SerializeField, ReadOnly] protected CanvasScaler scaler;
        [Space]
        [SerializeField] protected ScreenOrientation orientation = ScreenOrientation.AutoRotation;
        [Space]
        [SerializeField] protected Vector2 referenceResolution = new(1920, 1080);
        [SerializeField, Range(0f, 1f)] protected float matchWidthOrHeight = 1f;

        public Canvas Canvas => canvas != null ? canvas : canvas = GetComponent<Canvas>();

        public CanvasScaler Scaler => scaler != null ? scaler : scaler = GetComponent<CanvasScaler>();

        private void OnValidate()
        {
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            Scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            Scaler.matchWidthOrHeight = matchWidthOrHeight;
            Scaler.referenceResolution = referenceResolution;
        }
    }
}
