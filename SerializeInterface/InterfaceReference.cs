using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Submodules.Utility.SerializeInterface
{
    [Serializable]
    public class InterfaceReference<TInterface, TObject> where TObject : Object where TInterface : class
    {
        [SerializeField] [HideInInspector] private TObject underlyingValue;

        public InterfaceReference()
        {
        }

        public InterfaceReference( TObject target )
        {
            underlyingValue = target;
        }

        public InterfaceReference( TInterface @interface )
        {
            underlyingValue = @interface as TObject;
        }

        public TInterface Value
        {
            get => underlyingValue switch
            {
                null => null,
                TInterface @interface => @interface,
                _ => throw new InvalidOperationException(
                    $"{underlyingValue} needs to implements {nameof(TInterface)}" )
            };
            set => underlyingValue = value switch
            {
                null => null,
                TObject newValue => newValue,
                _ => throw new ArgumentException( $"{value} needs to implements {nameof(TInterface)}" )
            };
        }

        public TObject UnderlyingValue
        {
            get => underlyingValue;
            set => underlyingValue = value;
        }

        public static implicit operator TInterface( InterfaceReference<TInterface, TObject> obj )
        {
            return obj.Value;
        }
    }

    [Serializable]
    public sealed class InterfaceReference<TInterface> : InterfaceReference<TInterface, Object> where TInterface : class
    {
    }
}