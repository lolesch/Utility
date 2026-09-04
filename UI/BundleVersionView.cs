using NaughtyAttributes;
using Submodules.Utility.Tools;
using TMPro;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// Surfaces <see cref="BundleVersionSetter.GetVersion()"/> as <c>v&lt;version&gt;</c> on a
    /// <see cref="TextMeshProUGUI"/>. Drop it on the label that should show the build version.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class BundleVersionView : MonoBehaviour
    {
        [SerializeField, ReadOnly] private TextMeshProUGUI versionText;

        [ContextMenu("Refresh")]
        private void Start() => RefreshVersionText(BundleVersionSetter.GetVersion());

        private void RefreshVersionText(string versionNumber)
        {
            if (!versionText)
                versionText = GetComponent<TextMeshProUGUI>();

            if (versionText)
                versionText.text = $"v{versionNumber}";
        }
    }
}
