using UnityEngine;
using UnityEngine.UI;

namespace Submodules.Utility.Extensions
{
    public static class UIExtensions
    {
        public static bool IsOutsideOfGameWindow(Vector2 position) =>
            position.x < 0 || position.x > Screen.width ||
            position.y < 0 || position.y > Screen.height;

        // The pixel distance from one cell origin to the next, including spacing.
        public static Vector2 CellStep(this GridLayoutGroup grid) => grid.cellSize + grid.spacing;

        // Usage should be restricted to cases where multiple layout passes are unavoidable.
        public static void RefreshContentFitter(this RectTransform transform)
        {
            if (transform == null || !transform.gameObject.activeSelf)
                return;

            foreach (RectTransform child in transform)
                RefreshContentFitter(child);

            if (transform.TryGetComponent(out LayoutGroup layoutGroup))
            {
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }

            if (transform.GetComponent<ContentSizeFitter>())
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform);
        }
    }
}