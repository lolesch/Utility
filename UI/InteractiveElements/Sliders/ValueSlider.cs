using System.Globalization;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>The authored slider for a numeric readout: the 0..1 value, or something mapped from it,
    /// printed as a percentage or a number. Anything that is not a number (an enum name)
    /// derives from <see cref="AbstractSlider"/> and overrides <c>Format</c> instead.</summary>
    public class ValueSlider : AbstractSlider
    {
        [Tooltip("True to show the value as a 0.00 number, false to show it as a percent (50 %)")]
        [SerializeField] private bool useFloatingPoint = false;

        [Tooltip("How many equal intervals the slider is cut into, so a drag can land on Steps + 1 values: " +
                 "10 = 0, 0.1, 0.2 ... 1. 0 is continuous.")]
        [SerializeField, Min(0)] private int steps = 0;

        public override int Steps => steps;

        /// <summary>What the readout shows for a 0..1 value; override to run it through a curve or range.</summary>
        protected virtual float Map(float value) => value;

        // Invariant so the readout does not flip between "1.5" and "1,5" with the OS locale.
        protected override string Format(float value) =>
            Map(value).ToString(useFloatingPoint ? "F2" : "P0", CultureInfo.InvariantCulture);
    }
}
