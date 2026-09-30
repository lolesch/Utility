using TMPro;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class TestDisplay : MonoBehaviour, IDisplay<TextAndFontSize>
    {
        [SerializeField] protected TextMeshProUGUI testText;

        public void Refresh(TextAndFontSize newData)
        {
            testText.text = newData.text;
            testText.fontSize = newData.fontSize;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (testText == null)
                testText = GetComponent<TextMeshProUGUI>();

            Refresh(new("This is a test display - requires further implementation", testText.fontSize));
        }
#endif //UNITY_EDITOR
    }

    public struct TextAndFontSize
    {
        public string text;
        public float fontSize;

        public TextAndFontSize(string text, float fontSize)
        {
            this.text = text;
            this.fontSize = fontSize;
        }
    }
}
