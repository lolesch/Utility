using System;
using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>A collection of mutually exclusive <see cref="AbstractToggle"/>s.
    /// <see cref="Select"/> deselects whichever sibling was selected. Turning off the active
    /// toggle with no replacement is governed by two mutually exclusive settings, checked in
    /// this order: <see cref="IsClearable"/> lets the group end up with nothing selected;
    /// otherwise <see cref="IsRestorable"/> falls back to <see cref="PreviouslySelectedToggle"/>
    /// instead. With both off (the default), turning off the active toggle is prevented.
    /// </summary>
    public sealed class RadioGroup : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public AbstractToggle SelectedToggle { get; private set; }
        [field: SerializeField, ReadOnly] public AbstractToggle PreviouslySelectedToggle { get; private set; }
        [field: SerializeField] public bool IsClearable { get; private set; }

        [field: SerializeField, ShowIf("!" + nameof(IsClearable)), Tooltip(
            "Turning off the active toggle with no replacement falls back to whichever one " +
            "was selected before it, instead of being prevented. Only consulted while the " +
            "group is not Clearable.")]
        public bool IsRestorable { get; private set; }

        public event Action<AbstractToggle> OnGroupChanged;
        
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

        internal void Select(AbstractToggle toggle)
        {
            if (toggle == null || toggle.RadioGroup != this || SelectedToggle == toggle)
                return;

            PreviouslySelectedToggle = SelectedToggle;
            SelectedToggle = toggle;

            PreviouslySelectedToggle?.ToggleState(false);
            toggle.ToggleState(true);

            OnGroupChanged?.Invoke(SelectedToggle);
        }

        internal void Deselect(AbstractToggle toggle)
        {
            if (toggle == null || toggle.RadioGroup != this || toggle != SelectedToggle)
                return;

            if (!IsClearable)
            {
                // Restoring re-selects PreviouslySelectedToggle directly rather than routing
                // through Select(), which would try to turn `toggle` off a second time — it is
                // already off, mid-way through the SetToggle(false) call that got us here.
                if (!IsRestorable || !PreviouslySelectedToggle || PreviouslySelectedToggle == toggle)
                    return;

                var restored = PreviouslySelectedToggle;
                PreviouslySelectedToggle = toggle;
                SelectedToggle = restored;

                toggle.ToggleState(false);
                restored.ToggleState(true);

                OnGroupChanged?.Invoke(restored);

                return;
            }

            PreviouslySelectedToggle = toggle;
            SelectedToggle = null;
            toggle.ToggleState(false);

            OnGroupChanged?.Invoke(null);
        }
    }
}
