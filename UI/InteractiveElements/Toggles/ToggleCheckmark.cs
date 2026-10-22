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

        [SerializeField, Tooltip("Shows the parent Image's sprite (the toggle's Icon) as the checkmark, " +
                                 "so the two cannot drift apart. Turn off for a checkmark with a sprite of its own.")]
        private bool matchParentSprite = true;

        private bool show;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Image.enabled = true;
            ApplyParentSprite();
        }
#endif //UNITY_EDITOR

        private void ApplyParentSprite()
        {
            if (!matchParentSprite || !transform.parent)
                return;

            var parentImage = transform.parent.GetComponent<Image>();
            if (parentImage && Image.sprite != parentImage.sprite)
                Image.sprite = parentImage.sprite;
        }

        private void Awake()
        {
            ApplyParentSprite();
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