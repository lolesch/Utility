using System;
using NaughtyAttributes;
using Submodules.Utility.Attributes;
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
        
        [SerializeField, ReadOnly, SceneRef] private string currentScene;

        private void OnValidate()
        {
            currentScene = SceneManager.GetActiveScene().name;
            OnEnable();
        }

        private void OnEnable()
        {
            currentScene = SceneManager.GetActiveScene().name;
            SetSceneText();
        }

        private void SetSceneText()
        {
            //if (!Debug.isDebugBuild) return;
            if (SceneText != null && currentScene != null)
                SceneText.text = $"{currentScene} Scene";
        }
    }
}
