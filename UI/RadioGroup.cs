using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Submodules.Utility.UI
{
    public class RadioGroup : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public AbstractToggle ActivatedToggle { get; private set; }
        [field: SerializeField] public bool AllowSwitchOff { get; private set; } = false;

        /// <summary>Fires whenever the group's state changes: a toggle registered or
        /// unregistered, a sibling took over, or the active toggle switched itself off
        /// (then <see cref="ActivatedToggle"/> is null - issue #30).</summary>
        public event Action OnGroupChanged;

        private readonly List<AbstractToggle> radioToggles = new();
        private AbstractToggle previouslyActivatedToggle;

        public void Activate(AbstractToggle toActivate)
        {
            if (toActivate == null || ActivatedToggle == toActivate)
                return;

            previouslyActivatedToggle = ActivatedToggle;
            ActivatedToggle = toActivate;
            
            if (previouslyActivatedToggle != null) /*&& previouslyActivatedToggle.IsOn)*/
                previouslyActivatedToggle.SetToggle(false);
    
            OnGroupChanged?.Invoke();
        }

        public void Register(AbstractToggle item)
        {
            if (radioToggles.Contains(item))
                return;

            radioToggles.Add(item);

            OnGroupChanged?.Invoke();
        }

        public void Unregister(AbstractToggle item)
        {
            if (!radioToggles.Contains(item))
                return;

            radioToggles.Remove(item);

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
            if (toggle == null || toggle != ActivatedToggle)
                return;

            ActivatedToggle = null;

            OnGroupChanged?.Invoke();
        }
    }
}
