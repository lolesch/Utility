using UnityEngine;

namespace Submodules.Utility.Extensions
{
    public static class FloatExtensions
    {
        public static float Map( this float value, float fromMin, float fromMax, float toMin, float toMax )
        {
            // A zero-width source has no slope to follow: answer the low end of the target.
            if ( fromMax - fromMin == 0 )
                return toMin;

            return ( value - fromMin ) / ( fromMax - fromMin ) * ( toMax - toMin ) + toMin;
        }

        public static float MapTo01( this float value, float fromMin, float fromMax ) => Map( value, fromMin, fromMax, 0, 1 );

        public static float MapFrom01( this float value, float toMin, float toMax ) => Map( value, 0, 1, toMin, toMax );

        public static float Map( this float value, Vector2 from, float toMin, float toMax ) => Map( value, from.x, from.y, toMin, toMax );

        public static float Map( this float value, float fromMin, float fromMax, Vector2 to ) => Map( value, fromMin, fromMax, to.x, to.y );

        public static float Map( this float value, Vector2 from, Vector2 to ) => Map( value, from.x, from.y, to.x, to.y );

        /// <summary><see cref="Map(float,float,float,float,float)"/> that never leaves the target range, in
        /// either direction: a value outside the source range lands on the nearer end of the target.</summary>
        public static float MapClamped( this float value, float fromMin, float fromMax, float toMin, float toMax )
        {
            var mapped = Map( value, fromMin, fromMax, toMin, toMax );

            return Mathf.Clamp( mapped, Mathf.Min( toMin, toMax ), Mathf.Max( toMin, toMax ) );
        }

        public static float MapClamped( this float value, Vector2 from, Vector2 to ) => MapClamped( value, from.x, from.y, to.x, to.y );

        public static float Squared( this float value ) => value * value;

        //public static float MinutesToSeconds( this float minutes ) => minutes / 60f;
    }
}