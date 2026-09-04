using TMPro;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// The default <see cref="TooltipHost{T}"/> content - one TMP line, no styling beyond
    /// what the prefab wires. T = string is an ordinary <see cref="IView{T}"/> adapter, not a
    /// special case; a richer hint (an item preview, a stat card) is a new adapter over its
    /// own <see cref="TooltipHost{T}"/>, not a change here.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class StringTooltipContent : MonoBehaviour, IView<string>
    {
        [SerializeField] private TextMeshProUGUI label;

        public void Refresh(string data)
        {
            if (label)
                label.text = data;
        }
    }
}
