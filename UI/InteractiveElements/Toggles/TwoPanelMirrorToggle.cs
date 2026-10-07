using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// The off-state button of a two-panel switch: a toggle with no panel that names its
    /// <see cref="TwoPanelToggle"/> driver and sits in the driver's <see cref="ToggleGroup"/>. It does
    /// nothing itself, because the group does the mirroring: switching it on switches the driver off,
    /// which moves the panels, and the driver switching on switches it off. What it adds is the
    /// reference - a pair named by a field rather than found by hierarchy - and the warnings that
    /// can only be made once the driver is known.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TwoPanelMirrorToggle : AbstractToggle
    {
        [SerializeField] private TwoPanelToggle driver;

        protected override void OnToggle() { }

        /// <summary>What is wrong with how this mirror is authored, one sentence each; empty when it is
        /// wired as the off-state button of its driver has to be.</summary>
        internal IEnumerable<string> AuthoringProblems()
        {
            if (!driver)
            {
                yield return "its driver is unset: a mirror needs the 'driver' TwoPanelToggle it stands in for.";
                yield break;
            }

            if (!RadioGroup || RadioGroup != driver.RadioGroup)
                yield return $"it and its driver '{driver.name}' are not in the same ToggleGroup, so neither " +
                             "switches the other off.";
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            foreach (var problem in AuthoringProblems())
                Debug.LogWarning($"{name}: {problem}", this);
        }
#endif // UNITY_EDITOR
    }
}
