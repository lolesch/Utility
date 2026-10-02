using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class ToggleCheckmark : MonoBehaviour
    {
        [SerializeField, ReadOnly] private Image image = null;
        
        [SerializeField, ReadOnly] private AbstractToggle toggle;
        
        private bool show;

#if UNITY_EDITOR
        private void OnValidate()
        {
            GetComponents();
            image.enabled = true;
        }
#endif //UNITY_EDITOR

        private void Awake()
        {
            GetComponents();
            Refresh();
        }

        private void Update()
        {
            if(!toggle || !image)
                return;
            
            if (show != toggle.IsOn) 
                Refresh();
        }

        private void GetComponents()
        {
            image = GetComponent<Image>();
            toggle = GetComponentInParent<AbstractToggle>();

            if (!toggle)
                Debug.LogWarning($"[ToggleCheckmark] {name} has no Toggle parent component!", this);
        }

        private void Refresh()
        {
            show = toggle.IsOn;
            image.enabled = show;
        }
    }
}