using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Submodules.Utility.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DebugSceneName : MonoBehaviour
    {
        [SerializeField, ReadOnly] private TextMeshProUGUI sceneText;
        private TextMeshProUGUI SceneText => sceneText != null ? sceneText : sceneText = GetComponent<TextMeshProUGUI>();

        private void OnEnable()
        {
            if (SceneText != null)
                SceneText.text = Debug.isDebugBuild 
                    ? $"{SceneManager.GetActiveScene().name} Scene"
                    : string.Empty;
        }
    }
}
