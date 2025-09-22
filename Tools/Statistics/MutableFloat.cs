using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Submodules.Utility.Tools.Statistics
{
    public sealed class MutableFloat : IMutable<float>, IFormattable
    {
        [OdinSerialize, ReadOnly] private float _totalValue;
        // consider making _baseValue a Modifiable -> And growthPerLevel is applied on level up
        [OdinSerialize] private readonly float _baseValue;
        [OdinSerialize] private readonly List<Modifier> _modifiers;

        public event Action<float> OnTotalChanged;
        
        public MutableFloat( float baseValue )
        {
            _baseValue = baseValue;
            _totalValue = baseValue;
            _modifiers = new List<Modifier>();
            OnTotalChanged = null;
        }

        public static implicit operator float( MutableFloat mutableFloat ) => mutableFloat._totalValue;
        
        private void CalculateTotalValue()
        {
            ApplyModifiers( out var newTotal );

            //newTotal = Mathf.Clamp(newTotal, range.min, range.max);

            if( Mathf.Approximately( _totalValue, newTotal ) )
                return;

            _totalValue = newTotal;
            OnTotalChanged?.Invoke( _totalValue );
        }

        private void ApplyModifiers( out float newTotal )
        {
            newTotal = _baseValue;
            if( !_modifiers.Any() )
                return;

            var overwriteMods = _modifiers.Where( x => x.Type == ModifierType.Overwrite )
                .OrderByDescending( x => x );
            if( overwriteMods.Any() )
            {
                newTotal = overwriteMods.FirstOrDefault();
                return;
            }

            var flatAddModValue = _modifiers.Where( x => x.Type == ModifierType.FlatAdd ).Sum( x => x );
            newTotal += flatAddModValue;

            var percentAddModValue = _modifiers.Where( x => x.Type == ModifierType.PercentAdd ).Sum( x => x / 100f );
            newTotal *= 1 + percentAddModValue;

            var percentMultMods = _modifiers.Where( x => x.Type == ModifierType.PercentMult );
            newTotal = percentMultMods.Aggregate( newTotal, ( current, mod ) => current * ( 1 + mod / 100f ) );
        }

        public void AddModifier( Modifier modifier )
        {
            _modifiers.Add( modifier );
            CalculateTotalValue();
        }

        public bool TryRemoveModifier( Modifier modifier )
        {
            for( var i = _modifiers.Count; i-- > 0; )
                if( _modifiers[i].Equals( modifier ) )
                {
                    _modifiers.RemoveAt( i );

                    CalculateTotalValue();
                    return true;
                }

            Debug.LogWarning( $"Modifier {modifier} not found" );
            return false;
        }

        public string ToString(string format) => _totalValue.ToString( format );
        public string ToString(string format, IFormatProvider provider) => _totalValue.ToString( format, provider );
    }

    internal interface IMutable<out T>
    {
        //float BaseValue { get; }
        //float TotalValue { get; }
        //List<Modifier> Modifiers { get; }
        void AddModifier( Modifier modifier );
        bool TryRemoveModifier( Modifier modifier );
        event Action<T> OnTotalChanged;
    }
}