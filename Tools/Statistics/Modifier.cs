using System;
using Sirenix.Serialization;
using UnityEngine;

namespace Submodules.Utility.Tools.Statistics
{
    public struct Modifier : IComparable<Modifier>, IEquatable<Modifier>, IModifier
    {
        // TODO: add source to IModifier -> support removal of all modifiers from source
        [OdinSerialize] private float _value;
        [field: SerializeField] public readonly ModifierType Type { get; }

        public Modifier( float value, ModifierType type = ModifierType.FlatAdd )
        {
            _value = value;
            Type = type;
        }

        public static implicit operator float( Modifier mod ) => mod._value;

        public readonly int CompareTo( Modifier other )
        {
            var typeComparison = Type.CompareTo( other.Type );

            return typeComparison != 0 ? typeComparison : _value.CompareTo( other._value );
        }

        public readonly override string ToString() => Type switch
        {
            // WORDING
            // - Overwrite   => absolute, explicit, fix
            // - FlatAdd     => additional, additive, bonus, 
            // - PercentAdd  => more/less
            // - PercentMult => multiplicative
            
            ModifierType.Overwrite => $"= {_value:-0.###;0.###}",           //  = 123   | = -123   |  = 0
            ModifierType.FlatAdd => $"{_value:+0.###;-0.###;0.###}",        //   +123   |   -123   |    0
            ModifierType.PercentAdd => $"{_value:+0.###;-0.###;0.###} %",   //   +123 % |   -123 % |    0 %
            ModifierType.PercentMult => $"* {_value:-0.###;0.###} %",       //  * 123 % | * -123 % |  * 0 %

            var _ => $"?? {_value:+ 0.###;- 0.###;0.###}",
        };

        // TODO: extend by comparing sources!
        public readonly bool Equals( Modifier other ) =>
            Mathf.Approximately( _value, other._value ) && Type == other.Type;
    }

    internal interface IModifier
    {
        //float Value { get; }
        ModifierType Type { get; }
    }
}