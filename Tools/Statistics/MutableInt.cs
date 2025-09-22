using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Submodules.Utility.Tools.Statistics
{
    public sealed class MutableInt : IMutable<int>
    {
        [OdinSerialize, ReadOnly] private int _totalValue;
        [OdinSerialize] private readonly int _baseValue;
        [OdinSerialize] private readonly List<Modifier> _modifiers;
        
        public event Action<int> OnTotalChanged;
        
        public MutableInt( int baseValue )
        {
            _baseValue = baseValue;
            _totalValue = baseValue;
            _modifiers = new List<Modifier>();
            OnTotalChanged = null;
        }
        public static implicit operator int( MutableInt mutableInt ) => mutableInt._totalValue;

        private void CalculateTotalValue()
        {
            ApplyModifiers( out var newTotal );

            //newTotal = Mathf.Clamp(newTotal, range.min, range.max);

            if( Mathf.Approximately( _totalValue, newTotal ) )
                return;

            _totalValue = newTotal;
            OnTotalChanged?.Invoke( _totalValue );
        }

        private void ApplyModifiers( out int newTotal )
        {
            float moddedFloat = _baseValue;
            if( !_modifiers.Any() )
            {
                newTotal = _baseValue;   
                return;
            }

            var overwriteMods = _modifiers.Where( x => x.Type == ModifierType.Overwrite )
                .OrderByDescending( x => x );
            if( overwriteMods.Any() )
            {
                moddedFloat = overwriteMods.FirstOrDefault();
                newTotal = Mathf.RoundToInt( moddedFloat ); // should always round up?
                return;
            }

            var flatAddModValue = _modifiers.Where( x => x.Type == ModifierType.FlatAdd ).Sum( x => x );
            moddedFloat += flatAddModValue;

            var percentAddModValue = _modifiers.Where( x => x.Type == ModifierType.PercentAdd ).Sum( x => x / 100f );
            moddedFloat *= 1 + percentAddModValue;

            var percentMultMods = _modifiers.Where( x => x.Type == ModifierType.PercentMult );
            moddedFloat = percentMultMods.Aggregate( moddedFloat, ( current, mod ) => current * ( 1 + mod / 100f ) );
                
            newTotal = Mathf.RoundToInt( moddedFloat );
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
    }
}