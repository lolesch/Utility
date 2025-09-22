using UnityEngine;

namespace Submodules.Utility.Extensions
{
    public static class VectorExtensions
    {
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>The normalized direction (<paramref name="from" /> -> <paramref name="to" />).</returns>
        public static Vector3 DirectionTo( this Vector3 from, Vector3 to ) => Vector3.Normalize( to - from );

        /// <param name="point">The vector you want to convert</param>
        /// <param name="center">The relative coordinate center</param>
        /// <returns>The vector from <paramref name="center" /> to <paramref name="position" /></returns>
        public static Vector2 RelativeTo( this Vector2 point, Vector2 center ) => point - center;

        /// <param name="position">The vector you want to convert</param>
        /// <param name="center">The relative coordinate center</param>
        /// <returns>The vector from <paramref name="center" /> to <paramref name="position" /></returns>
        public static Vector3 RelativeTo( this Vector3 position, Vector3 center ) => position - center;

        public static Coordinate RelativeTo( this Coordinate coordinate, Coordinate center ) => coordinate - center;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>The normalized direction (<paramref name="from"/> -> <paramref name="to"/>).</returns>
        public static Coordinate Direction(this Coordinate from, Coordinate to)
        {
            var distance = to - from;
            var num = Coordinate.Magnitude(distance);
            return num > 1E-05f ? distance / num : new Coordinate();
        }

        /// <summary>
        ///     Project a vector on a rotated cartesian coordinate system.
        /// </summary>
        /// <param name="vector3"></param>
        /// <param name="yDegrees"></param>
        /// <returns><see cref="Vector3" /> rotated by <paramref name="yDegrees" /> around the global upAxis.</returns>
        public static Vector3 ToIsometric( this Vector3 vector3, float yDegrees = 45f )
        {
            var isoMatrix = Matrix4x4.Rotate( Quaternion.Euler( 0f, yDegrees, 0f ) );

            return isoMatrix.MultiplyPoint3x4( vector3 );
        }

        public static Vector2 Normal( this Vector2 vector ) => new( -vector.y, vector.x );

        /// <summary>
        ///     Converts a <see cref="Vector2" /> into a <see cref="Vector3" /> with settable <paramref name="y" /> value.
        /// </summary>
        /// <param name="vec2"></param>
        /// <param name="y"></param>
        /// <returns>new(<paramref name="vec2" />.x, <paramref name="y" />, <paramref name="vec2" />.y)</returns>
        public static Vector3 ToXZ( this Vector2 vec2, float y = 0f ) => new( vec2.x, y, vec2.y );
        public static Vector2 AsXZ( this Vector3 vec3 ) => new( vec3.x, vec3.z );

        #region Conversions

        /// <summary>
        /// Converts a <see cref="Coordinate"/> into a <see cref="Vector3"/> with settable <paramref name="y"/> value.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="y"></param>
        /// <returns><see cref="Vector3"/>(<paramref name="coordinate"/>.x, <paramref name="y"/>, <paramref name="coordinate"/>.z)</returns>
        public static Vector3 ToVector3(this Coordinate coordinate, float y = 0f) => new(coordinate.x, y, coordinate.z);
        public static Vector2 ToVector2(this Coordinate coordinate) => new(coordinate.x, coordinate.z);
        
        /// <summary>
        /// Converts a <see cref="Vector3"/> into a <see cref="Coordinate"/> dropping the <paramref name="y"/> value.
        /// </summary>
        /// <param name="vector3"></param>
        /// <param name="y"></param>
        /// <returns><see cref="Coordinate"/>(<paramref name="vector3"/>.x, <paramref name="vector3"/>.z)</returns>
        public static Coordinate ToCoordinate(this Vector3 vector3) => new(vector3.x, vector3.z);
        public static Coordinate ToCoordinate(this Vector2 vector2) => new(vector2.x, vector2.y);

        // convert a vector from coordinate space to another coordinate space
        /* spaces:
         * - screenSpace
         * - windowSpace
         * - worldSpace
         * - cameraSpace
         * - viewport?
         * - ...
         */

        #endregion Conversions
    }
}