using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Submodules.Utility.Extensions
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
        /// Returns true if the given Coordinate is exactly equal to this Coordinate. Exact, so that equal
        /// coordinates always hash equal: an approximate comparison cannot have a matching hash and is not
        /// transitive, which is why a Coordinate is not used as a dictionary key either.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Coordinate other) => x.Equals( other.x ) && z.Equals( other.z );

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
        public override int GetHashCode() => HashCode.Combine( x + 0f, z + 0f ); // + 0f folds -0 into +0, which Equals treats as one

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Magnitude(Coordinate coord) => (float)Math.Sqrt((coord.x * coord.x) + (coord.z * coord.z));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Coordinate a, Coordinate b)
        {
            var num1 = a.x - b.x;
            var num2 = a.z - b.z;
            return (float) Math.Sqrt( num1 * num1 + num2 * num2 );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SqrMagnitude(Coordinate a) => a.x * a.x + a.z * a.z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Coordinate lhs, Coordinate rhs) => lhs.x * rhs.x + lhs.z * rhs.z;

        /// <summary>
        /// Returns the coordinate scaled to length 1, or zero for the zero coordinate.
        /// </summary>
        public static Coordinate Normalize(Coordinate coord)
        {
            var magnitude = Magnitude(coord);

            return magnitude > 0f ? coord / magnitude : default;
        }

        /// <summary>
        /// Moves <paramref name="current"/> toward <paramref name="target"/> by at most
        /// <paramref name="maxDistanceDelta"/>, landing on the target rather than past it. A negative delta
        /// does not move away from the target.
        /// </summary>
        public static Coordinate MoveTowards(Coordinate current, Coordinate target, float maxDistanceDelta)
        {
            var toTarget = target - current;
            var distance = Magnitude(toTarget);

            if (distance <= maxDistanceDelta)
                return target;

            if (maxDistanceDelta <= 0f)
                return current;

            return current + toTarget / distance * maxDistanceDelta;
        }

        /// <summary>
        /// Returns the coordinate with its length limited to <paramref name="maxLength"/>, keeping its direction.
        /// A negative limit counts as zero.
        /// </summary>
        public static Coordinate ClampMagnitude(Coordinate coord, float maxLength)
        {
            maxLength = Mathf.Max(maxLength, 0f);

            var magnitude = Magnitude(coord);

            return magnitude > maxLength ? coord / magnitude * maxLength : coord;
        }

        /// <summary>
        /// Linearly interpolates between <paramref name="a"/> and <paramref name="b"/> by <paramref name="t"/>,
        /// which is clamped to 0..1.
        /// </summary>
        public static Coordinate Lerp(Coordinate a, Coordinate b, float t)
        {
            t = Mathf.Clamp01(t);

            return new Coordinate(a.x + (b.x - a.x) * t, a.z + (b.z - a.z) * t);
        }

        /// <summary>
        /// Rotates the coordinate about the origin by <paramref name="degrees"/>. Positive turns from +x toward +z.
        /// </summary>
        public static Coordinate Rotate(Coordinate coord, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            var cos = (float)Math.Cos(radians);
            var sin = (float)Math.Sin(radians);

            return new Coordinate(coord.x * cos - coord.z * sin, coord.x * sin + coord.z * cos);
        }

        /// <summary>
        /// The angle in degrees, in -180..180, that turns <paramref name="from"/> onto the direction of
        /// <paramref name="to"/>; positive turns from +x toward +z, as <see cref="Rotate"/> does. Zero when
        /// either coordinate is the zero coordinate.
        /// </summary>
        public static float SignedAngle(Coordinate from, Coordinate to)
        {
            if (SqrMagnitude(from) == 0f || SqrMagnitude(to) == 0f)
                return 0f;

            var cross = from.x * to.z - from.z * to.x;

            return (float)Math.Atan2(cross, Dot(from, to)) * Mathf.Rad2Deg;
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
        public static bool operator ==(Coordinate lhs, Coordinate rhs) => lhs.Equals(rhs);

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
