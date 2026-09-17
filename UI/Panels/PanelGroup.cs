using System;
using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>A collection of <see cref="SimplePanel"/>s occupying the same screen space.
    /// <see cref="Show"/> hides whichever sibling was up. <see cref="RadioGroup"/>'s
    /// counterpart for content rather than input: a panel's visibility is owned here, not
    /// derived from which toggle happens to be selected, so anything — a toggle, a plain
    /// button, phase-change code — can drive it the same way.
    /// </summary>
    public class PanelGroup : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public SimplePanel ActivePanel { get; private set; }
        [field: SerializeField] public bool IsClearable { get; private set; }

        public event Action OnGroupChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (ActivePanel && ActivePanel.PanelGroup != this)
                ActivePanel = null;
        }
#endif

        public void ClearActive() => Clear(ActivePanel);

        public void Show(SimplePanel panel)
        {
            if (panel == null || panel.PanelGroup != this || ActivePanel == panel)
                return;

            var previous = ActivePanel;
            ActivePanel = panel;

            if (previous != null)
                previous.Toggle(false);

            panel.Toggle(true);

            OnGroupChanged?.Invoke();
        }

        public void Clear(SimplePanel panel)
        {
            if (!IsClearable || panel == null || panel.PanelGroup != this || panel != ActivePanel)
                return;

            ActivePanel = null;
            panel.Toggle(false);

            OnGroupChanged?.Invoke();
        }
    }
}
