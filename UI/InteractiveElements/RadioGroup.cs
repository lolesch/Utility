using System;
using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>A collection of mutually exclusive <see cref="AbstractToggle"/>s.
    /// <see cref="Select"/> will un-toggle the previously selected one.
    /// </summary>
    public class RadioGroup : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public AbstractToggle SelectedToggle { get; private set; }
        [field: SerializeField, ReadOnly] public AbstractToggle PreviouslySelectedToggle { get; private set; }
        [field: SerializeField] public bool IsDeselectable { get; private set; }

        public event Action OnGroupChanged;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (SelectedToggle && SelectedToggle.RadioGroup != this)
                SelectedToggle = null;

            if (PreviouslySelectedToggle && PreviouslySelectedToggle.RadioGroup != this)
                PreviouslySelectedToggle = null;
        }
#endif

        public void ClearSelection() => Deselect(SelectedToggle);

        public void Select(AbstractToggle toggle)
        {
            if (toggle == null || toggle.RadioGroup != this || SelectedToggle == toggle)
                return;

            PreviouslySelectedToggle = SelectedToggle;
            SelectedToggle = toggle;
            
            if (PreviouslySelectedToggle != null)
                PreviouslySelectedToggle.SetToggle(false);
    
            OnGroupChanged?.Invoke();
        }
        
        public void Deselect(AbstractToggle toggle)
        {
            if (!IsDeselectable || toggle == null || toggle.RadioGroup != this || toggle != SelectedToggle)
                return;

            PreviouslySelectedToggle = toggle;
            SelectedToggle = null;

            OnGroupChanged?.Invoke();
        }
    }
}
