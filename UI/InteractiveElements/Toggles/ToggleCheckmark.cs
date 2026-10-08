using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class ToggleCheckmark : MonoBehaviour
    {
        [SerializeField, ReadOnly] private Image image = null;
        private Image Image => image ? image : image = GetComponent<Image>();
        
        [SerializeField, ReadOnly] private AbstractToggle toggle;
        private AbstractToggle Toggle => toggle ? toggle : toggle = GetComponentInParent<AbstractToggle>(true);
        
        private bool show;

#if UNITY_EDITOR
        private void OnValidate() => Image.enabled = true;
#endif //UNITY_EDITOR

        private void Awake()
        {
            Refresh();
            enabled = Toggle;
        }

        private void Update()
        {
            if(!Toggle || !Image)
                return;
            
            if (show != Toggle.IsOn) 
                Refresh();
        }

        private void Refresh()
        {
            show = Toggle.IsOn;
            Image.enabled = show;
        }
    }
}