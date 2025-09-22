using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace InventorySurvivor.Code.Utility.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///     An inline richtext color converter
        /// </summary>
        public static string Colored( this string text, Color color )
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA( color )}>{text}</color>";
        }

        public static string ColoredComponent( this string text )
        {
            return Colored( text, ColorExtensions.Prefab );
        }

        public static string ColoredComponent( this GameObject gameObject )
        {
            return ColoredComponent( gameObject.name );
        }

        private static string SplitCamelCase( this object obj )
        {
            return Regex.Replace( obj.ToString(), "([A-Z])", " $1",
                RegexOptions.Compiled ).Trim();
        }

        /// <param name="obj"></param>
        /// <returns>
        ///     The <see cref="DescriptionAttribute" /> of <paramref name="obj" />. If none was found, returns
        ///     <see cref="SplitCamelCase(object)" /> instead.
        /// </returns>
        public static string ToDescription( this object obj )
        {
            var description = obj.GetAttributeOfType<DescriptionAttribute>();

            return description != null ? description.Description : obj.SplitCamelCase();
        }

        /// <summary>
        ///     Gets an attribute on an object field value
        /// </summary>
        /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
        /// <param name="obj">The object value</param>
        /// <returns>The attribute of type T that exists on the object value</returns>
        public static T GetAttributeOfType<T>( this object obj ) where T : Attribute
        {
            return (T) obj.GetType()?.GetField( obj.ToString() )?.GetCustomAttribute( typeof(T), false );
        }

        public static Enum ToEnum<T>( this string enumDescription ) where T : Enum
        {
            var type = typeof(T);

            foreach ( var val in Enum.GetValues( type ) )
                if ( ToDescription( (T) val ) == enumDescription )
                    return (T) val;

            throw new ArgumentException( "ToEnum<T>(): Invalid description for enum " + nameof(T), "enumDescription" );
        }
    }
}