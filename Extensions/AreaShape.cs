using System;
using UnityEngine;

namespace Submodules.Utility.Extensions
{
    /// <summary>
    /// Where an <see cref="AreaShape"/> starts when a skill is aimed at a target.
    /// </summary>
    public enum AreaAnchor
    {
        /// <summary>The shape starts on the target.</summary>
        Target,
        /// <summary>The shape starts on the caster and points at the target.</summary>
        Origin,
    }

    public enum AreaShapeKind
    {
        Disk,
        Sector,
        Rectangle,
    }

    /// <summary>
    /// A pure description of an area on the XZ ground plane that answers whether a point lies inside it, so a
    /// skill can define its area as data. Build one with <see cref="Disk"/>, <see cref="Sector"/> or
    /// <see cref="Rectangle"/>; an annulus or an arc is the inner radius of a disk or sector, not a shape of its
    /// own. Every boundary counts as inside. Angles are degrees, positive from +x toward +z, as in
    /// <see cref="Coordinate"/>.
    /// </summary>
    [Serializable]
    public struct AreaShape
    {
        /// <summary>Degrees of slack on a sector's edge, which trigonometry does not hit exactly.</summary>
        private const float EdgeMargin = 1e-3f;

        [field: SerializeField] public AreaShapeKind Kind { get; private set; }
        [field: SerializeField] public float Radius { get; private set; }
        [field: SerializeField] public float InnerRadius { get; private set; }
        [field: SerializeField] public float Angle { get; private set; }
        [field: SerializeField] public float Length { get; private set; }
        [field: SerializeField] public float Width { get; private set; }
        [field: SerializeField] public float PivotAlong { get; private set; }
        [field: SerializeField] public float PivotAcross { get; private set; }

        /// <summary>
        /// Everything within <paramref name="radius"/> of the origin, minus the middle within
        /// <paramref name="innerRadius"/> when that is above zero.
        /// </summary>
        public static AreaShape Disk(float radius, float innerRadius = 0f) => new()
        {
            Kind = AreaShapeKind.Disk,
            Radius = Math.Max(radius, 0f),
            InnerRadius = Math.Max(innerRadius, 0f),
        };

        /// <summary>
        /// A <paramref name="angle"/>-degree wedge of a disk, centred on the facing, minus the middle within
        /// <paramref name="innerRadius"/> when that is above zero. The angle is clamped to 0..360.
        /// </summary>
        public static AreaShape Sector(float radius, float angle, float innerRadius = 0f) => new()
        {
            Kind = AreaShapeKind.Sector,
            Radius = Math.Max(radius, 0f),
            InnerRadius = Math.Max(innerRadius, 0f),
            Angle = Math.Clamp(angle, 0f, 360f),
        };

        /// <summary>
        /// A box <paramref name="length"/> long along the facing and <paramref name="width"/> wide across it. The
        /// pivots are fractions (0..1) of the box that lie behind the origin along the facing and on the -side of it
        /// across the facing, which is toward -z when facing +x: the default 0 and 0.5 start the box at the origin
        /// and centre it on the facing line, a beam; 0.5 and 0.5 centre it on the origin.
        /// </summary>
        public static AreaShape Rectangle(float length, float width, float pivotAlong = 0f, float pivotAcross = 0.5f) => new()
        {
            Kind = AreaShapeKind.Rectangle,
            Length = Math.Max(length, 0f),
            Width = Math.Max(width, 0f),
            PivotAlong = Math.Clamp(pivotAlong, 0f, 1f),
            PivotAcross = Math.Clamp(pivotAcross, 0f, 1f),
        };

        /// <summary>
        /// Whether <paramref name="point"/> lies in the shape placed at <paramref name="origin"/> and pointing along
        /// <paramref name="facing"/>, which needs no unit length. A zero facing counts as +x.
        /// </summary>
        public bool Contains(Coordinate point, Coordinate origin, Coordinate facing)
        {
            var offset = point - origin;

            return Kind == AreaShapeKind.Rectangle
                ? ContainsInBox(offset, FacingOrEast(facing))
                : ContainsInRing(offset, FacingOrEast(facing));
        }

        /// <summary>
        /// Whether <paramref name="point"/> lies in the shape of a skill that <paramref name="caster"/> aims at
        /// <paramref name="target"/>. The shape starts on the target or on the caster per <paramref name="anchor"/>,
        /// and points along the line from the caster to the target either way.
        /// </summary>
        public bool Contains(Coordinate point, AreaAnchor anchor, Coordinate caster, Coordinate target) =>
            Contains(point, anchor == AreaAnchor.Target ? target : caster, target - caster);

        /// <summary>
        /// The shape with its area multiplied by <paramref name="areaMultiplier"/>: every length scales by its
        /// square root, and an angle or pivot stays. A multiplier of zero or less leaves a shape of no size.
        /// </summary>
        public AreaShape Scaled(float areaMultiplier)
        {
            var lengthFactor = (float)Math.Sqrt(Math.Max(areaMultiplier, 0f));

            return new AreaShape
            {
                Kind = Kind,
                Radius = Radius * lengthFactor,
                InnerRadius = InnerRadius * lengthFactor,
                Angle = Angle,
                Length = Length * lengthFactor,
                Width = Width * lengthFactor,
                PivotAlong = PivotAlong,
                PivotAcross = PivotAcross,
            };
        }

        private bool ContainsInRing(Coordinate offset, Coordinate facing)
        {
            var distanceSquared = Coordinate.SqrMagnitude(offset);

            if (distanceSquared > Radius * Radius || distanceSquared < InnerRadius * InnerRadius)
                return false;

            return Kind != AreaShapeKind.Sector
                   || Math.Abs(Coordinate.SignedAngle(facing, offset)) <= Angle * 0.5f + EdgeMargin;
        }

        private bool ContainsInBox(Coordinate offset, Coordinate facing)
        {
            var unit = Coordinate.Normalize(facing);
            var along = Coordinate.Dot(offset, unit);
            var across = unit.x * offset.z - unit.z * offset.x;

            return along >= -PivotAlong * Length && along <= (1f - PivotAlong) * Length
                   && across >= -PivotAcross * Width && across <= (1f - PivotAcross) * Width;
        }

        private static Coordinate FacingOrEast(Coordinate facing) =>
            Coordinate.SqrMagnitude(facing) > 0f ? facing : new Coordinate(1f, 0f);
    }
}
