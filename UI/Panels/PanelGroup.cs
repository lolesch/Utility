using System;
using NaughtyAttributes;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary><see cref="RadioGroup"/>'s counterpart for content rather than input: a
    /// collection of mutually exclusive <see cref="SimplePanel"/>s. <see cref="Show"/> hides
    /// whichever sibling was up. Hiding the active panel with no replacement is governed by two
    /// mutually exclusive settings, checked in this order: <see cref="IsClearable"/> lets the
    /// group end up with nothing shown; otherwise <see cref="IsRestorable"/> falls back to
    /// <see cref="PreviouslyActivePanel"/> instead. With both off (the default), hiding the
    /// active panel is prevented.
    /// </summary>
    public sealed class PanelGroup : MonoBehaviour
    {
        [field: SerializeField, ReadOnly] public SimplePanel ActivePanel { get; private set; }
        [field: SerializeField, ReadOnly] public SimplePanel PreviouslyActivePanel { get; private set; }
        [field: SerializeField] public bool IsClearable { get; private set; }

        [field: SerializeField, ShowIf("!" + nameof(IsClearable)), Tooltip(
            "Hiding the active panel with no replacement falls back to whichever one was " +
            "shown before it, instead of being prevented. Only consulted while the group is " +
            "not Clearable.")]
        public bool IsRestorable { get; private set; }

        public event Action<SimplePanel> OnGroupChanged;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (ActivePanel && ActivePanel.PanelGroup != this)
                ActivePanel = null;

            if (PreviouslyActivePanel && PreviouslyActivePanel.PanelGroup != this)
                PreviouslyActivePanel = null;
        }
#endif

        public void ClearActive() => Hide(ActivePanel);

        internal void Show(SimplePanel panel)
        {
            if (panel == null || panel.PanelGroup != this || ActivePanel == panel)
                return;

            PreviouslyActivePanel = ActivePanel;
            ActivePanel = panel;

            PreviouslyActivePanel?.Disappear(false);
            panel.Appear(false);

            OnGroupChanged?.Invoke(ActivePanel);
        }

        internal void Hide(SimplePanel panel)
        {
            if (panel == null || panel.PanelGroup != this || panel != ActivePanel)
                return;

            if (!IsClearable)
            {
                // Restoring re-shows PreviouslyActivePanel directly rather than routing through
                // Show(), which would try to hide `panel` a second time — it is already being
                // hidden, mid-way through the FadeOut() call that got us here.
                if (!IsRestorable || !PreviouslyActivePanel || PreviouslyActivePanel == panel)
                    return;
                
                var restored = PreviouslyActivePanel;
                PreviouslyActivePanel = panel;
                ActivePanel = restored;

                panel.Disappear(false);
                restored.Appear(false);

                OnGroupChanged?.Invoke(restored);

                return;
            }

            PreviouslyActivePanel = panel;
            ActivePanel = null;
            panel.Disappear(false);

            OnGroupChanged?.Invoke(null);
        }
    }
}
