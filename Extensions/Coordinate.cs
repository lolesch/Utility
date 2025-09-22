using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace InventorySurvivor.Code.Utility.Extensions
{
    /// <summary>
    /// Representation of 2D coordinates on the XZ-Plane
    /// </summary>
    public struct Coordinate : IEquatable<Coordinate>, IFormattable
    {
        public float x;
        public float z;

        /// <summary>
        /// Constructs a new Coordinate from a given Vector3.
        /// </summary>
        /// <param name="vec3"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Coordinate(Vector3 vec3)
        {
            x = vec3.x;
            z = vec3.z;
        }

        /// <summary>
        /// Constructs a new Coordinate from a given Vector2.
        /// </summary>
        /// <param name="vec2"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Coordinate(Vector2 vec2)
        {
            x = vec2.x;
            z = vec2.y;
        }

        /// <summary>
        /// Constructs a new Coordinate with given x, z components.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Coordinate(float x, float z)
        {
            this.x = x;
            this.z = z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other) => other is Coordinate coordinate && Equals(coordinate);

        /// <summary>
        /// Returns true if the given Coordinate is exactly equal to this Coordinate.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Coordinate other) => Mathf.Approximately( x, other.x ) && Mathf.Approximately( z, other.z );

        /// <summary>
        /// Returns a formatted string for this Coordinate.
        /// </summary>
        public override string ToString() => ToString(null, null);

        /// <summary>
        /// Returns a formatted string for this Coordinate.
        /// </summary>
        /// <param name="format">A numeric format string.</param>
        /// <returns></returns>
        public string ToString(string format) => ToString(format, null);

        /// <summary>
        /// Returns a formatted string for this Coordinate.
        /// </summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
        /// <returns></returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";

            formatProvider ??= CultureInfo.InvariantCulture.NumberFormat;

            return string.Format("({0}, {1})", x.ToString(format, formatProvider), z.ToString(format, formatProvider));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => x.GetHashCode() ^ (z.GetHashCode() << 2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Magnitude(Coordinate coord) => (float)Math.Sqrt((coord.x * coord.x) + (coord.z * coord.z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Coordinate a, Coordinate b)
        {
            var num1 = a.x - b.x;
            var num2 = a.z - b.z;
            return (float) Math.Sqrt( num1 * num1 + num2 * num2 );
        }
        
        /*

        /// <summary>
        /// Set x and z components of an existing Coordinate.
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newZ"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(float newX, float newZ)
        {
            x = newX;
            z = newZ;
        }

        /// <summary>
        /// Linearly interpolates between Coordinates a and b by t.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate Lerp(Coordinate a, Coordinate b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Coordinate(a.x + (b.x - a.x) * t, a.z + (b.z - a.z) * t);
        }

        /// <summary>
        /// Linearly interpolates between vectors a and b by t.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate LerpUnclamped(Coordinate a, Coordinate b, float t) => new(a.x + (b.x - a.x) * t, a.z + (b.z - a.z) * t);

        /// <summary>
        /// Moves a point current towards characterPrefab.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="characterPrefab"></param>
        /// <param name="maxDistanceDelta"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate MoveTowards(Coordinate current, Coordinate characterPrefab, float maxDistanceDelta)
        {
            var num = characterPrefab.x - current.x;
            var num2 = characterPrefab.z - current.z;
            var num3 = num * num + num2 * num2;
            if (num3 == 0f || (maxDistanceDelta >= 0f && num3 <= maxDistanceDelta * maxDistanceDelta))
            {
                return characterPrefab;
            }

            var num4 = (float)Math.Sqrt(num3);
            return new Coordinate(current.x + num / num4 * maxDistanceDelta, current.z + num2 / num4 * maxDistanceDelta);
        }

        /// <summary>
        /// Multiplies this Coordinate by a Vector2.
        /// </summary>
        /// <param name="scale"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Scale(Vector2 scale)
        {
            x *= scale.x;
            z *= scale.y;
        }


        //
        // Summary:
        //     Reflects a coord off the coord defined by a normal.
        //
        // Parameters:
        //   inDirection:
        //
        //   inNormal:
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Vector2 Reflect(Vector2 inDirection, Vector2 inNormal)
        //{
        //    var num = -2f * Dot(inNormal, inDirection);
        //    return new Vector2(num * inNormal.x + inDirection.x, num * inNormal.y + inDirection.y);
        //}

        //
        // Summary:
        //     Returns the 2D coord perpendicular to this 2D coord. The result is always rotated
        //     90-degrees in a counter-clockwise direction for a 2D coordinate system where
        //     the positive Y axis goes up.
        //
        // Parameters:
        //   inDirection:
        //     The input direction.
        //
        // Returns:
        //     The perpendicular direction.
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Vector2 Perpendicular(Vector2 inDirection) => new(0f - inDirection.y, inDirection.x);

        //
        // Summary:
        //     Dot Product of two vectors.
        //
        // Parameters:
        //   lhs:
        //
        //   rhs:
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static float Dot(Vector2 lhs, Vector2 rhs) => lhs.x * rhs.x + lhs.y * rhs.y;

        //
        // Summary:
        //     Gets the unsigned angle in degrees between from and to.
        //
        // Parameters:
        //   from:
        //     The coord from which the angular difference is measured.
        //
        //   to:
        //     The coord to which the angular difference is measured.
        //
        // Returns:
        //     The unsigned angle in degrees between the two vectors.
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static float Angle(Coordinate from, Coordinate to)
        //{
        //    var num = (float)Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
        //    if (num < 1E-15f)
        //    {
        //        return 0f;
        //    }
        //
        //    var num2 = Mathf.Clamp(Dot(from, to) / num, -1f, 1f);
        //    return (float)Math.Acos(num2) * 57.29578f;
        //}

        //
        // Summary:
        //     Gets the signed angle in degrees between from and to.
        //
        // Parameters:
        //   from:
        //     The coord from which the angular difference is measured.
        //
        //   to:
        //     The coord to which the angular difference is measured.
        //
        // Returns:
        //     The signed angle in degrees between the two vectors.
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static float SignedAngle(Coordinate from, Coordinate to)
        //{
        //    var num = Angle(from, to);
        //    var num2 = Mathf.Sign(from.x * to.y - from.y * to.x);
        //    return num * num2;
        //}

        //
        // Summary:
        //     Returns a copy of coord with its magnitude clamped to maxLength.
        //
        // Parameters:
        //   coord:
        //
        //   maxLength:
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Coordinate ClampMagnitude(Coordinate coord, float maxLength)
        //{
        //    var num = coord.sqrMagnitude;
        //    if (num > maxLength * maxLength)
        //    {
        //        var num2 = (float)Math.Sqrt(num);
        //        var num3 = coord.x / num2;
        //        var num4 = coord.y / num2;
        //        return new Coordinate(num3 * maxLength, num4 * maxLength);
        //    }
        //
        //    return coord;
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SqrMagnitude(Coordinate a) => a.x * a.x + a.z * a.z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SqrMagnitude() => x * x + z * z;

        //
        // Summary:
        //     Returns a coord that is made from the smallest components of two vectors.
        //
        // Parameters:
        //   lhs:
        //
        //   rhs:
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Vector2 Min(Vector2 lhs, Vector2 rhs) => new(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y));

        //
        // Summary:
        //     Returns a coord that is made from the largest components of two vectors.
        //
        // Parameters:
        //   lhs:
        //
        //   rhs:
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Vector2 Max(Vector2 lhs, Vector2 rhs) => new(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y));

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //[ExcludeFromDocs]
        //public static Vector2 SmoothDamp(Vector2 current, Vector2 characterPrefab, ref Vector2 currentVelocity, float smoothTime, float maxSpeed)
        //{
        //    var deltaTime = Time.deltaTime;
        //    return SmoothDamp(current, characterPrefab, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //[ExcludeFromDocs]
        //public static Vector2 SmoothDamp(Vector2 current, Vector2 characterPrefab, ref Vector2 currentVelocity, float smoothTime)
        //{
        //    var deltaTime = Time.deltaTime;
        //    var maxSpeed = float.PositiveInfinity;
        //    return SmoothDamp(current, characterPrefab, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        //}

        //public static Vector2 SmoothDamp(Vector2 current, Vector2 characterPrefab, ref Vector2 currentVelocity, float smoothTime, [DefaultValue("Mathf.Infinity")] float maxSpeed, [DefaultValue("Time.deltaTime")] float deltaTime)
        //{
        //    smoothTime = Mathf.Max(0.0001f, smoothTime);
        //    var num = 2f / smoothTime;
        //    var num2 = num * deltaTime;
        //    var num3 = 1f / (1f + num2 + 0.48f * num2 * num2 + 0.235f * num2 * num2 * num2);
        //    var num4 = current.x - characterPrefab.x;
        //    var num5 = current.y - characterPrefab.y;
        //    var coord = characterPrefab;
        //    var num6 = maxSpeed * smoothTime;
        //    var num7 = num6 * num6;
        //    var num8 = num4 * num4 + num5 * num5;
        //    if (num8 > num7)
        //    {
        //        var num9 = (float)Math.Sqrt(num8);
        //        num4 = num4 / num9 * num6;
        //        num5 = num5 / num9 * num6;
        //    }
        //
        //    characterPrefab.x = current.x - num4;
        //    characterPrefab.y = current.y - num5;
        //    var num10 = (currentVelocity.x + num * num4) * deltaTime;
        //    var num11 = (currentVelocity.y + num * num5) * deltaTime;
        //    currentVelocity.x = (currentVelocity.x - num * num10) * num3;
        //    currentVelocity.y = (currentVelocity.y - num * num11) * num3;
        //    var num12 = characterPrefab.x + (num4 + num10) * num3;
        //    var num13 = characterPrefab.y + (num5 + num11) * num3;
        //    var num14 = coord.x - current.x;
        //    var num15 = coord.y - current.y;
        //    var num16 = num12 - coord.x;
        //    var num17 = num13 - coord.y;
        //    if (num14 * num16 + num15 * num17 > 0f)
        //    {
        //        num12 = coord.x;
        //        num13 = coord.y;
        //        currentVelocity.x = (num12 - coord.x) / deltaTime;
        //        currentVelocity.y = (num13 - coord.y) / deltaTime;
        //    }
        //
        //    return new Vector2(num12, num13);
        //}
*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate operator +(Coordinate a, Coordinate b) => new(a.x + b.x, a.z + b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate operator -(Coordinate a, Coordinate b) => new(a.x - b.x, a.z - b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate operator *(Coordinate a, Coordinate b) => new(a.x * b.x, a.z * b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate operator /(Coordinate a, Coordinate b) => new(a.x / b.x, a.z / b.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate operator -(Coordinate a) => new(0f - a.x, 0f - a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate operator *(Coordinate a, float d) => new(a.x * d, a.z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate operator *(float d, Coordinate a) => new(a.x * d, a.z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Coordinate operator /(Coordinate a, float d) => new(a.x / d, a.z / d);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Coordinate lhs, Coordinate rhs)
        {
            var num = lhs.x - rhs.x;
            var num2 = lhs.z - rhs.z;
            return (num * num) + (num2 * num2) < 9.99999944E-11f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Coordinate lhs, Coordinate rhs) => !(lhs == rhs);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static implicit operator Coordinate(Vector3 v) => new(v.x, v.z);
        //
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static implicit operator Coordinate(Vector2 v) => new(v.x, v.y);
        //
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static implicit operator Vector3(Coordinate v) => new(v.x, 0f, v.z);
        //
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static implicit operator Vector2(Coordinate v) => new(v.x, v.z);
    }
}
