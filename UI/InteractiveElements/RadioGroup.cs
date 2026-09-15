using System;
using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    public class RadioGroup : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public AbstractToggle ActivatedToggle { get; private set; }
        [field: SerializeField, ReadOnly] public AbstractToggle PreviouslyActivatedToggle { get; private set; }
        [field: SerializeField] public bool CanDeactivateAll { get; private set; } = false;

        /// <summary>Fires whenever the group's selection changes: a sibling took over, or
        /// the active toggle switched itself off (then <see cref="ActivatedToggle"/> is null
        /// - issue #30). <see cref="Activate"/> and <see cref="Deactivate"/> are the only
        /// emitters; the group keeps no membership list, so a toggle being enabled or
        /// disabled is not by itself a change and does not raise this.</summary>
        public event Action OnGroupChanged;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (ActivatedToggle != null && ActivatedToggle.RadioGroup != this)
                ActivatedToggle = null;

            if (PreviouslyActivatedToggle != null && PreviouslyActivatedToggle.RadioGroup != this)
                PreviouslyActivatedToggle = null;
        }
#endif

        public void Activate(AbstractToggle toggle)
        {
            if (toggle == null || toggle.RadioGroup != this || ActivatedToggle == toggle)
                return;

            PreviouslyActivatedToggle = ActivatedToggle;
            ActivatedToggle = toggle;
            
            if (PreviouslyActivatedToggle != null) /*&& PreviouslyActivatedToggle.IsOn)*/
                PreviouslyActivatedToggle.SetToggle(false);
    
            OnGroupChanged?.Invoke();
        }
        
        /// <summary>
        /// The counterpart to <see cref="Activate"/>: the active toggle turned off, so the
        /// group clears and fires <see cref="OnGroupChanged"/> (issue #30). Reached from
        /// <see cref="AbstractToggle.SetToggle"/> however the toggle went off - its own
        /// click, a keyboard submit, or a direct <c>SetToggle(false)</c>. A sibling taking
        /// over never lands here: <see cref="Activate"/> reassigns <see cref="ActivatedToggle"/>
        /// before it deselects the others, so the guard below rejects them.
        /// </summary>
        public void Deactivate(AbstractToggle toggle)
        {
            if (toggle == null || toggle.RadioGroup != this || toggle != ActivatedToggle)
                return;

            // The toggle that just went off is the one a restore has to bring back —
            // MultiplePanelToggle reads PreviouslyActivatedToggle to undo itself.
            PreviouslyActivatedToggle = toggle;
            ActivatedToggle = null;

            OnGroupChanged?.Invoke();
        }
    }
}
