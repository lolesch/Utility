using System;
using UnityEngine;

namespace Submodules.Utility.SerializeInterface
{
    [AttributeUsage( AttributeTargets.Field )]
    public sealed class RequireInterfaceAttribute : PropertyAttribute
    {
        public readonly Type InterfaceType;

        public RequireInterfaceAttribute( Type interfaceType )
        {
            Debug.Assert( interfaceType.IsInterface, $"{nameof(interfaceType)} needs to be an interface." );
            InterfaceType = interfaceType;
        }
    }
}