using System;
using System.Collections.Generic;
using UnityEngine;

namespace Submodules.Utility.UI
{
    /// <summary>
    /// A slider over the members of an enum: one value per entry of <see cref="Members"/>, and
    /// the readout is the member's name. Nothing is authored — <see cref="AbstractSlider.Steps"/>
    /// comes from the member count. Needs a concrete subclass to be attachable (Unity cannot
    /// serialize an open generic), which is also where <see cref="Members"/> is narrowed when the
    /// slider should offer fewer members than the enum has.
    ///
    /// The slider position is an index, not the enum's underlying number: enums like
    /// <c>ItemRarity</c> (0, 5, 15, 20, 30) are not contiguous, so <see cref="Selected"/> is the
    /// translation.
    /// </summary>
    public abstract class EnumSlider<TEnum> : AbstractSlider where TEnum : struct, Enum
    {
        private static readonly TEnum[] AllMembers = (TEnum[])Enum.GetValues(typeof(TEnum));

        /// <summary>The members the slider offers, in slider order. Defaults to every member in declaration order.</summary>
        protected virtual IReadOnlyList<TEnum> Members => AllMembers;

        public override int Steps => Mathf.Max(0, Members.Count - 1);

        public TEnum Selected => Members[Mathf.Clamp(StepIndex, 0, Members.Count - 1)];

        /// <summary>Moves the slider to <paramref name="member"/> without raising <see cref="AbstractSlider.OnValueChanged"/>.
        /// A member the slider does not offer is ignored.</summary>
        public void SetSelectedWithoutNotify(TEnum member)
        {
            for (var i = 0; i < Members.Count; i++)
            {
                if (!EqualityComparer<TEnum>.Default.Equals(Members[i], member))
                    continue;

                SetStepIndexWithoutNotify(i);
                return;
            }
        }

        protected override string Format(float value) =>
            Members[Mathf.Clamp(ToStepIndex(value), 0, Members.Count - 1)].ToString();
    }
}
