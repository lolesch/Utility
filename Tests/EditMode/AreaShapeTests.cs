using NUnit.Framework;
using Submodules.Utility.Extensions;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// Pins <see cref="AreaShape"/>: each shape answers inside or outside for a point given its origin and
    /// facing, a boundary counts as inside, and an area multiplier scales every length by its square root.
    /// Angles are degrees, positive from +x toward +z, as in <see cref="Coordinate"/>.
    /// </summary>
    [TestFixture]
    public sealed class AreaShapeTests
    {
        private static readonly Coordinate Zero = new(0f, 0f);
        private static readonly Coordinate East = new(1f, 0f);

        private static Coordinate At(float x, float z) => new(x, z);

        // --- Disk ---

        [Test]
        public void Disk_ContainsItsOrigin() =>
            Assert.That(AreaShape.Disk(5f).Contains(Zero, Zero, East), Is.True);

        [Test]
        public void Disk_ContainsAPointJustInsideItsRadius() =>
            Assert.That(AreaShape.Disk(5f).Contains(At(4.99f, 0f), Zero, East), Is.True);

        [Test]
        public void Disk_ContainsAPointOnItsRadius() =>
            Assert.That(AreaShape.Disk(5f).Contains(At(3f, 4f), Zero, East), Is.True);

        [Test]
        public void Disk_ExcludesAPointJustOutsideItsRadius() =>
            Assert.That(AreaShape.Disk(5f).Contains(At(5.01f, 0f), Zero, East), Is.False);

        [Test]
        public void Disk_IsMeasuredFromItsOrigin_NotFromTheWorldOrigin()
        {
            var disk = AreaShape.Disk(2f);

            Assert.That(disk.Contains(At(11f, 10f), At(10f, 10f), East), Is.True);
            Assert.That(disk.Contains(At(1f, 0f), At(10f, 10f), East), Is.False);
        }

        [Test]
        public void Disk_IgnoresFacing() =>
            Assert.That(AreaShape.Disk(5f).Contains(At(-3f, 0f), Zero, East), Is.True);

        // --- Disk with an inner radius ---

        [Test]
        public void HollowDisk_ExcludesItsOriginAndAPointJustInsideTheInnerRadius()
        {
            var ring = AreaShape.Disk(5f, innerRadius: 2f);

            Assert.That(ring.Contains(Zero, Zero, East), Is.False);
            Assert.That(ring.Contains(At(1.99f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void HollowDisk_ContainsAPointOnTheInnerRadiusAndOneJustBeyondIt()
        {
            var ring = AreaShape.Disk(5f, innerRadius: 2f);

            Assert.That(ring.Contains(At(2f, 0f), Zero, East), Is.True);
            Assert.That(ring.Contains(At(2.01f, 0f), Zero, East), Is.True);
        }

        [Test]
        public void HollowDisk_StillEndsAtItsOuterRadius()
        {
            var ring = AreaShape.Disk(5f, innerRadius: 2f);

            Assert.That(ring.Contains(At(5f, 0f), Zero, East), Is.True);
            Assert.That(ring.Contains(At(5.01f, 0f), Zero, East), Is.False);
        }

        // --- Sector ---

        // A 90 degree sector of radius 5 facing +x spans -45..45 degrees.

        [Test]
        public void Sector_ContainsAPointAheadOfItAndExcludesOneBehind()
        {
            var cone = AreaShape.Sector(5f, 90f);

            Assert.That(cone.Contains(At(3f, 0f), Zero, East), Is.True);
            Assert.That(cone.Contains(At(-3f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Sector_ContainsItsOrigin() =>
            Assert.That(AreaShape.Sector(5f, 90f).Contains(Zero, Zero, East), Is.True);

        [Test]
        public void Sector_EndsAtItsRadius()
        {
            var cone = AreaShape.Sector(5f, 90f);

            Assert.That(cone.Contains(At(5f, 0f), Zero, East), Is.True);
            Assert.That(cone.Contains(At(5.01f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Sector_ContainsAPointOnEitherEdgeAndExcludesOneJustPastIt()
        {
            var cone = AreaShape.Sector(5f, 90f);

            Assert.That(cone.Contains(At(2f, 2f), Zero, East), Is.True);
            Assert.That(cone.Contains(At(2f, -2f), Zero, East), Is.True);
            Assert.That(cone.Contains(At(2f, 2.1f), Zero, East), Is.False);
            Assert.That(cone.Contains(At(2f, -2.1f), Zero, East), Is.False);
        }

        [Test]
        public void Sector_FollowsItsFacing()
        {
            var cone = AreaShape.Sector(5f, 90f);
            var north = At(0f, 1f);

            Assert.That(cone.Contains(At(0f, 3f), Zero, north), Is.True);
            Assert.That(cone.Contains(At(3f, 0f), Zero, north), Is.False);
        }

        [Test]
        public void Sector_FacingDoesNotNeedToBeUnitLength() =>
            Assert.That(AreaShape.Sector(5f, 90f).Contains(At(0f, 3f), Zero, At(0f, 40f)), Is.True);

        [Test]
        public void Sector_WrapsAcrossTheSeamBehindTheXAxis()
        {
            // Facing -x, the cone spans 135..225 degrees, which crosses the +-180 seam of SignedAngle.
            var cone = AreaShape.Sector(5f, 90f);
            var west = At(-1f, 0f);

            Assert.That(cone.Contains(At(-3f, 1f), Zero, west), Is.True);
            Assert.That(cone.Contains(At(-3f, -1f), Zero, west), Is.True);
            Assert.That(cone.Contains(At(-1f, 3f), Zero, west), Is.False);
            Assert.That(cone.Contains(At(-1f, -3f), Zero, west), Is.False);
        }

        [Test]
        public void Sector_OfAHalfTurn_ContainsEverythingAheadOfItsFacingLine()
        {
            var half = AreaShape.Sector(5f, 180f);

            Assert.That(half.Contains(At(0f, 3f), Zero, East), Is.True);
            Assert.That(half.Contains(At(0f, -3f), Zero, East), Is.True);
            Assert.That(half.Contains(At(-0.1f, 3f), Zero, East), Is.False);
        }

        [Test]
        public void Sector_OfAFullTurn_IsADisk()
        {
            var full = AreaShape.Sector(5f, 360f);

            Assert.That(full.Contains(At(-3f, 0f), Zero, East), Is.True);
            Assert.That(full.Contains(At(-5.01f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Sector_WithAZeroFacing_FacesPlusX()
        {
            var cone = AreaShape.Sector(5f, 90f);

            Assert.That(cone.Contains(At(3f, 0f), Zero, Zero), Is.True);
            Assert.That(cone.Contains(At(-3f, 0f), Zero, Zero), Is.False);
        }

        // --- Sector with an inner radius ---

        [Test]
        public void HollowSector_ExcludesItsOriginAndAPointJustInsideTheInnerRadius()
        {
            var arc = AreaShape.Sector(5f, 90f, innerRadius: 2f);

            Assert.That(arc.Contains(Zero, Zero, East), Is.False);
            Assert.That(arc.Contains(At(1.99f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void HollowSector_ContainsAPointOnTheInnerRadiusAndStillHonoursTheAngle()
        {
            var arc = AreaShape.Sector(5f, 90f, innerRadius: 2f);

            Assert.That(arc.Contains(At(2f, 0f), Zero, East), Is.True);
            Assert.That(arc.Contains(At(-3f, 0f), Zero, East), Is.False);
        }

        // --- Rectangle ---

        // Rectangle(length, width, pivotAlong, pivotAcross): length runs along the facing, width across it, and
        // the pivot is the fraction of each that lies behind / to the right of the origin. The default pivot puts
        // the origin in the middle of the near short edge, so the rectangle is a beam from the origin.

        [Test]
        public void Rectangle_ContainsPointsAlongItsLengthAndEndsAtIt()
        {
            var beam = AreaShape.Rectangle(4f, 2f);

            Assert.That(beam.Contains(Zero, Zero, East), Is.True);
            Assert.That(beam.Contains(At(4f, 0f), Zero, East), Is.True);
            Assert.That(beam.Contains(At(4.01f, 0f), Zero, East), Is.False);
            Assert.That(beam.Contains(At(-0.01f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Rectangle_ContainsPointsAcrossItsWidthAndEndsAtIt()
        {
            var beam = AreaShape.Rectangle(4f, 2f);

            Assert.That(beam.Contains(At(2f, 1f), Zero, East), Is.True);
            Assert.That(beam.Contains(At(2f, -1f), Zero, East), Is.True);
            Assert.That(beam.Contains(At(2f, 1.01f), Zero, East), Is.False);
            Assert.That(beam.Contains(At(2f, -1.01f), Zero, East), Is.False);
        }

        [Test]
        public void Rectangle_ContainsItsCorners()
        {
            var beam = AreaShape.Rectangle(4f, 2f);

            Assert.That(beam.Contains(At(4f, 1f), Zero, East), Is.True);
            Assert.That(beam.Contains(At(4f, -1f), Zero, East), Is.True);
            Assert.That(beam.Contains(At(4.01f, 1.01f), Zero, East), Is.False);
        }

        [Test]
        public void Rectangle_FollowsItsFacing()
        {
            var beam = AreaShape.Rectangle(4f, 2f);
            var north = At(0f, 3f);

            Assert.That(beam.Contains(At(0f, 4f), Zero, north), Is.True);
            Assert.That(beam.Contains(At(1f, 2f), Zero, north), Is.True);
            Assert.That(beam.Contains(At(0f, 4.01f), Zero, north), Is.False);
            Assert.That(beam.Contains(At(2f, 2f), Zero, north), Is.False);
            Assert.That(beam.Contains(At(4f, 0f), Zero, north), Is.False);
        }

        [Test]
        public void Rectangle_FacingADiagonal_RotatesTheBox()
        {
            var beam = AreaShape.Rectangle(4f, 2f);
            var diagonal = At(1f, 1f);

            Assert.That(beam.Contains(At(1f, 1f), Zero, diagonal), Is.True);
            Assert.That(beam.Contains(At(1.5f, 0.5f), Zero, diagonal), Is.True);
            Assert.That(beam.Contains(At(2f, 0f), Zero, diagonal), Is.False);
            Assert.That(beam.Contains(At(2.7f, 2.7f), Zero, diagonal), Is.True);
            Assert.That(beam.Contains(At(2.9f, 2.9f), Zero, diagonal), Is.False);
        }

        [Test]
        public void Rectangle_IsMeasuredFromItsOrigin() =>
            Assert.That(AreaShape.Rectangle(4f, 2f).Contains(At(13f, 10.5f), At(10f, 10f), East), Is.True);

        [Test]
        public void Rectangle_PivotAlong_MovesTheOriginAlongTheLength()
        {
            var centred = AreaShape.Rectangle(4f, 2f, pivotAlong: 0.5f);

            Assert.That(centred.Contains(At(-2f, 0f), Zero, East), Is.True);
            Assert.That(centred.Contains(At(2f, 0f), Zero, East), Is.True);
            Assert.That(centred.Contains(At(-2.01f, 0f), Zero, East), Is.False);
            Assert.That(centred.Contains(At(2.01f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Rectangle_PivotAcross_MovesTheOriginAcrossTheWidth()
        {
            // Facing +x, the width runs toward +z; a pivot of 0 puts the origin on the -z long edge.
            var edge = AreaShape.Rectangle(4f, 2f, pivotAcross: 0f);

            Assert.That(edge.Contains(At(2f, 2f), Zero, East), Is.True);
            Assert.That(edge.Contains(At(2f, 2.01f), Zero, East), Is.False);
            Assert.That(edge.Contains(At(2f, -0.01f), Zero, East), Is.False);
        }

        // --- Area multiplier ---

        [Test]
        public void Scaled_ByFour_DoublesADisksRadius()
        {
            var disk = AreaShape.Disk(5f).Scaled(4f);

            Assert.That(disk.Contains(At(9.99f, 0f), Zero, East), Is.True);
            Assert.That(disk.Contains(At(10.01f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Scaled_ByOne_ChangesNothing()
        {
            var disk = AreaShape.Disk(5f, innerRadius: 2f).Scaled(1f);

            Assert.That(disk.Contains(At(1.99f, 0f), Zero, East), Is.False);
            Assert.That(disk.Contains(At(2f, 0f), Zero, East), Is.True);
            Assert.That(disk.Contains(At(5f, 0f), Zero, East), Is.True);
            Assert.That(disk.Contains(At(5.01f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Scaled_BelowOne_ShrinksTheShape()
        {
            var disk = AreaShape.Disk(10f).Scaled(0.25f);

            Assert.That(disk.Contains(At(4.99f, 0f), Zero, East), Is.True);
            Assert.That(disk.Contains(At(5.01f, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Scaled_ScalesTheInnerRadiusToo()
        {
            var ring = AreaShape.Disk(5f, innerRadius: 2f).Scaled(4f);

            Assert.That(ring.Contains(At(3.99f, 0f), Zero, East), Is.False);
            Assert.That(ring.Contains(At(4f, 0f), Zero, East), Is.True);
        }

        [Test]
        public void Scaled_ScalesASectorsRadiusAndInnerRadiusButNotItsAngle()
        {
            var arc = AreaShape.Sector(5f, 90f, innerRadius: 1f).Scaled(4f);

            Assert.That(arc.Contains(At(1.99f, 0f), Zero, East), Is.False);
            Assert.That(arc.Contains(At(2f, 0f), Zero, East), Is.True);
            Assert.That(arc.Contains(At(10f, 0f), Zero, East), Is.True);
            Assert.That(arc.Contains(At(10.01f, 0f), Zero, East), Is.False);
            // Still 45 degrees off the facing at the edge: (6,6) is on it, (6,6.2) is past it.
            Assert.That(arc.Contains(At(6f, 6f), Zero, East), Is.True);
            Assert.That(arc.Contains(At(6f, 6.2f), Zero, East), Is.False);
        }

        [Test]
        public void Scaled_ScalesARectanglesLengthAndWidthAndKeepsItsPivot()
        {
            var box = AreaShape.Rectangle(4f, 2f, pivotAlong: 0.5f).Scaled(4f);

            Assert.That(box.Contains(At(3.99f, 1.99f), Zero, East), Is.True);
            Assert.That(box.Contains(At(-3.99f, -1.99f), Zero, East), Is.True);
            Assert.That(box.Contains(At(4.01f, 0f), Zero, East), Is.False);
            Assert.That(box.Contains(At(0f, 2.01f), Zero, East), Is.False);
        }

        [Test]
        public void Scaled_AMultiplierOfZeroOrLess_LeavesNothingButThePoint()
        {
            foreach (var multiplier in new[] { 0f, -3f })
            {
                var disk = AreaShape.Disk(5f).Scaled(multiplier);

                Assert.That(disk.Contains(At(0.01f, 0f), Zero, East), Is.False, $"multiplier {multiplier}");
            }
        }

        [Test]
        public void Scaled_DoesNotChangeTheShapeItWasCalledOn()
        {
            var disk = AreaShape.Disk(5f);

            disk.Scaled(4f);

            Assert.That(disk.Contains(At(5.01f, 0f), Zero, East), Is.False);
        }

        // --- Boundary margin ---

        // A boundary counts as inside with LinearMargin (1e-3 ground units) of slack, the same slack as the
        // simulation's range checks: on it and within the margin is in, beyond the margin is out.

        private const float Beyond = AreaShape.LinearMargin * 3f;
        private const float Within = AreaShape.LinearMargin * 0.5f;

        [Test]
        public void Disk_OuterRadius_HoldsPointsWithinTheMarginAndNotBeyondIt()
        {
            var disk = AreaShape.Disk(5f);

            Assert.That(disk.Contains(At(5f, 0f), Zero, East), Is.True);
            Assert.That(disk.Contains(At(5f + Within, 0f), Zero, East), Is.True);
            Assert.That(disk.Contains(At(5f + Beyond, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Disk_InnerRadius_HoldsPointsWithinTheMarginAndNotBeyondIt()
        {
            var ring = AreaShape.Disk(5f, innerRadius: 2f);

            Assert.That(ring.Contains(At(2f, 0f), Zero, East), Is.True);
            Assert.That(ring.Contains(At(2f - Within, 0f), Zero, East), Is.True);
            Assert.That(ring.Contains(At(2f - Beyond, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Disk_WithoutAnInnerRadius_KeepsItsOrigin() =>
            Assert.That(AreaShape.Disk(5f).Contains(Zero, Zero, East), Is.True);

        [Test]
        public void Disk_InnerRadiusBelowTheMargin_StillExcludesNothingNegative() =>
            Assert.That(AreaShape.Disk(5f, innerRadius: Within).Contains(Zero, Zero, East), Is.True);

        [Test]
        public void Sector_RadiiShareTheLinearMargin()
        {
            var arc = AreaShape.Sector(5f, 90f, innerRadius: 2f);

            Assert.That(arc.Contains(At(5f + Within, 0f), Zero, East), Is.True);
            Assert.That(arc.Contains(At(5f + Beyond, 0f), Zero, East), Is.False);
            Assert.That(arc.Contains(At(2f - Within, 0f), Zero, East), Is.True);
            Assert.That(arc.Contains(At(2f - Beyond, 0f), Zero, East), Is.False);
        }

        [Test]
        public void Rectangle_EveryEdge_HoldsPointsWithinTheMarginAndNotBeyondIt()
        {
            var box = AreaShape.Rectangle(4f, 2f);

            Assert.That(box.Contains(At(4f + Within, 0f), Zero, East), Is.True, "far short edge");
            Assert.That(box.Contains(At(-Within, 0f), Zero, East), Is.True, "near short edge");
            Assert.That(box.Contains(At(2f, 1f + Within), Zero, East), Is.True, "+side long edge");
            Assert.That(box.Contains(At(2f, -1f - Within), Zero, East), Is.True, "-side long edge");

            Assert.That(box.Contains(At(4f + Beyond, 0f), Zero, East), Is.False, "far short edge");
            Assert.That(box.Contains(At(-Beyond, 0f), Zero, East), Is.False, "near short edge");
            Assert.That(box.Contains(At(2f, 1f + Beyond), Zero, East), Is.False, "+side long edge");
            Assert.That(box.Contains(At(2f, -1f - Beyond), Zero, East), Is.False, "-side long edge");
        }

        [Test]
        public void Rectangle_Margin_IsMeasuredInGroundUnits_NotInFacingLengths()
        {
            // A long facing vector must not stretch the slack: the box normalises it first.
            var box = AreaShape.Rectangle(4f, 2f);
            var far = At(1000f, 0f);

            Assert.That(box.Contains(At(4f + Within, 0f), Zero, far), Is.True);
            Assert.That(box.Contains(At(4f + Beyond, 0f), Zero, far), Is.False);
        }

        // --- Anchor ---

        [Test]
        public void TargetAnchor_StartsTheShapeOnTheTarget()
        {
            var blast = AreaShape.Disk(2f);
            var caster = Zero;
            var target = At(10f, 0f);

            Assert.That(blast.Contains(At(10f, 1f), AreaAnchor.Target, caster, target), Is.True);
            Assert.That(blast.Contains(At(1f, 0f), AreaAnchor.Target, caster, target), Is.False);
        }

        [Test]
        public void OriginAnchor_StartsTheShapeOnTheCaster()
        {
            var blast = AreaShape.Disk(2f);
            var caster = Zero;
            var target = At(10f, 0f);

            Assert.That(blast.Contains(At(1f, 0f), AreaAnchor.Origin, caster, target), Is.True);
            Assert.That(blast.Contains(At(10f, 1f), AreaAnchor.Origin, caster, target), Is.False);
        }

        [Test]
        public void OriginAnchor_PointsTheShapeFromTheCasterAtTheTarget()
        {
            var cone = AreaShape.Sector(5f, 90f);
            var caster = At(1f, 1f);
            var target = At(1f, 11f);

            Assert.That(cone.Contains(At(1f, 4f), AreaAnchor.Origin, caster, target), Is.True);
            Assert.That(cone.Contains(At(4f, 1f), AreaAnchor.Origin, caster, target), Is.False);
        }

        [Test]
        public void TargetAnchor_PointsTheShapeAlongTheLineFromTheCasterToTheTarget()
        {
            var cone = AreaShape.Sector(5f, 90f);
            var caster = Zero;
            var target = At(0f, 10f);

            Assert.That(cone.Contains(At(0f, 13f), AreaAnchor.Target, caster, target), Is.True);
            Assert.That(cone.Contains(At(0f, 7f), AreaAnchor.Target, caster, target), Is.False);
        }

        [Test]
        public void Anchor_WithTheCasterOnTheTarget_FacesPlusX()
        {
            var cone = AreaShape.Sector(5f, 90f);
            var spot = At(2f, 2f);

            Assert.That(cone.Contains(At(5f, 2f), AreaAnchor.Origin, spot, spot), Is.True);
            Assert.That(cone.Contains(At(-1f, 2f), AreaAnchor.Origin, spot, spot), Is.False);
        }
    }
}
