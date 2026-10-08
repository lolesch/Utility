using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// The other button of a two-panel switch: a toggle with no panel that names its
    /// <see cref="TwoPanelToggle"/> driver and sits in the driver's <see cref="ToggleGroup"/>. It is
    /// the driver's off-state button, and so the only way to switch the driver off when the driver is
    /// the group's first member. It does
    /// nothing itself, because the group does the mirroring: switching it on switches the driver off,
    /// which moves the panels, and the driver switching on switches it off. What it adds is the
    /// reference - a pair named by a field rather than found by hierarchy - and the warnings that
    /// can only be made once the driver is known.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TwoPanelMirrorToggle : AbstractToggle
    {
        [SerializeField] private TwoPanelToggle driver;

        /// <summary>The driver this mirror stands in for, so the driver can find its mirror.</summary>
        internal TwoPanelToggle Driver => driver;

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

            if (IsOn && driver.IsOn)
                yield return $"it and its driver '{driver.name}' are both authored on, so the pair would have both " +
                             "tab buttons selected. Author one of them on: the group's first member.";
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
