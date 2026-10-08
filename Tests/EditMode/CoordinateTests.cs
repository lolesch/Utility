using NUnit.Framework;
using Submodules.Utility.Extensions;
using UnityEngine;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// Pins <see cref="Coordinate"/>'s ground-plane operations. Angles are degrees and positive turns from
    /// +x toward +z (counterclockwise seen from above), so <c>Rotate(from, SignedAngle(from, to))</c> points at
    /// <c>to</c>.
    /// </summary>
    [TestFixture]
    public sealed class CoordinateTests
    {
        private const float Tolerance = 1e-4f;

        private static void AssertAt(Coordinate actual, float x, float z)
        {
            Assert.That(actual.x, Is.EqualTo(x).Within(Tolerance), $"x of {actual}");
            Assert.That(actual.z, Is.EqualTo(z).Within(Tolerance), $"z of {actual}");
        }

        // --- Dot ---

        [Test]
        public void Dot_MultipliesAndSumsTheComponents() =>
            Assert.That(Coordinate.Dot(new Coordinate(1f, 2f), new Coordinate(3f, 4f)), Is.EqualTo(11f).Within(Tolerance));

        [Test]
        public void Dot_OfPerpendicularCoordinates_IsZero() =>
            Assert.That(Coordinate.Dot(new Coordinate(1f, 0f), new Coordinate(0f, 5f)), Is.EqualTo(0f).Within(Tolerance));

        // --- Normalize ---

        [Test]
        public void Normalize_ScalesToUnitLengthKeepingDirection() =>
            AssertAt(Coordinate.Normalize(new Coordinate(3f, 4f)), 0.6f, 0.8f);

        [Test]
        public void Normalize_OfZero_StaysZero()
        {
            var result = Coordinate.Normalize(new Coordinate(0f, 0f));

            AssertAt(result, 0f, 0f);
            Assert.That(float.IsNaN(result.x) || float.IsNaN(result.z), Is.False);
        }

        // --- MoveTowards ---

        [Test]
        public void MoveTowards_StepsTheGivenDistanceAlongTheLine() =>
            AssertAt(Coordinate.MoveTowards(new Coordinate(0f, 0f), new Coordinate(10f, 0f), 3f), 3f, 0f);

        [Test]
        public void MoveTowards_StepsAlongADiagonal() =>
            AssertAt(Coordinate.MoveTowards(new Coordinate(0f, 0f), new Coordinate(3f, 4f), 2.5f), 1.5f, 2f);

        [Test]
        public void MoveTowards_WithMoreStepThanDistance_LandsOnTheTarget() =>
            AssertAt(Coordinate.MoveTowards(new Coordinate(0f, 0f), new Coordinate(10f, 0f), 99f), 10f, 0f);

        [Test]
        public void MoveTowards_WithExactlyTheDistance_LandsOnTheTarget() =>
            AssertAt(Coordinate.MoveTowards(new Coordinate(0f, 0f), new Coordinate(3f, 4f), 5f), 3f, 4f);

        [Test]
        public void MoveTowards_WhenAlreadyThere_StaysPut() =>
            AssertAt(Coordinate.MoveTowards(new Coordinate(2f, 2f), new Coordinate(2f, 2f), 1f), 2f, 2f);

        [Test]
        public void MoveTowards_WithZeroStep_StaysPut() =>
            AssertAt(Coordinate.MoveTowards(new Coordinate(1f, 1f), new Coordinate(9f, 9f), 0f), 1f, 1f);

        [Test]
        public void MoveTowards_WithNegativeStep_DoesNotMoveAway() =>
            AssertAt(Coordinate.MoveTowards(new Coordinate(1f, 1f), new Coordinate(9f, 9f), -3f), 1f, 1f);

        // --- ClampMagnitude ---

        [Test]
        public void ClampMagnitude_LeavesAShorterCoordinateAlone() =>
            AssertAt(Coordinate.ClampMagnitude(new Coordinate(3f, 4f), 10f), 3f, 4f);

        [Test]
        public void ClampMagnitude_ShortensALongerCoordinateKeepingDirection() =>
            AssertAt(Coordinate.ClampMagnitude(new Coordinate(6f, 8f), 5f), 3f, 4f);

        [Test]
        public void ClampMagnitude_AtExactlyTheLimit_LeavesItAlone() =>
            AssertAt(Coordinate.ClampMagnitude(new Coordinate(3f, 4f), 5f), 3f, 4f);

        [Test]
        public void ClampMagnitude_WithZeroOrNegativeLimit_IsZero()
        {
            AssertAt(Coordinate.ClampMagnitude(new Coordinate(3f, 4f), 0f), 0f, 0f);
            AssertAt(Coordinate.ClampMagnitude(new Coordinate(3f, 4f), -2f), 0f, 0f);
        }

        // --- Lerp ---

        [Test]
        public void Lerp_AtTheEnds_ReturnsTheEnds()
        {
            AssertAt(Coordinate.Lerp(new Coordinate(2f, 4f), new Coordinate(6f, 8f), 0f), 2f, 4f);
            AssertAt(Coordinate.Lerp(new Coordinate(2f, 4f), new Coordinate(6f, 8f), 1f), 6f, 8f);
        }

        [Test]
        public void Lerp_AtHalf_IsTheMidpoint() =>
            AssertAt(Coordinate.Lerp(new Coordinate(2f, 4f), new Coordinate(6f, 8f), 0.5f), 4f, 6f);

        [Test]
        public void Lerp_ClampsTOutsideZeroToOne()
        {
            AssertAt(Coordinate.Lerp(new Coordinate(2f, 4f), new Coordinate(6f, 8f), 2f), 6f, 8f);
            AssertAt(Coordinate.Lerp(new Coordinate(2f, 4f), new Coordinate(6f, 8f), -1f), 2f, 4f);
        }

        // --- Rotate ---

        [Test]
        public void Rotate_ByNinety_TurnsFromXTowardZ() =>
            AssertAt(Coordinate.Rotate(new Coordinate(1f, 0f), 90f), 0f, 1f);

        [Test]
        public void Rotate_ByNegativeNinety_TurnsFromZTowardX() =>
            AssertAt(Coordinate.Rotate(new Coordinate(0f, 1f), -90f), 1f, 0f);

        [Test]
        public void Rotate_ByOneEighty_Flips() =>
            AssertAt(Coordinate.Rotate(new Coordinate(2f, 3f), 180f), -2f, -3f);

        [Test]
        public void Rotate_ByAFullTurn_ComesBack() =>
            AssertAt(Coordinate.Rotate(new Coordinate(2f, 3f), 360f), 2f, 3f);

        [Test]
        public void Rotate_KeepsTheLength() =>
            Assert.That(Coordinate.Magnitude(Coordinate.Rotate(new Coordinate(3f, 4f), 37f)), Is.EqualTo(5f).Within(Tolerance));

        [Test]
        public void Rotate_OfZero_StaysZero() =>
            AssertAt(Coordinate.Rotate(new Coordinate(0f, 0f), 45f), 0f, 0f);

        // --- SignedAngle ---

        [Test]
        public void SignedAngle_TurningTowardZ_IsPositive() =>
            Assert.That(Coordinate.SignedAngle(new Coordinate(1f, 0f), new Coordinate(0f, 1f)), Is.EqualTo(90f).Within(Tolerance));

        [Test]
        public void SignedAngle_TurningTowardX_IsNegative() =>
            Assert.That(Coordinate.SignedAngle(new Coordinate(0f, 1f), new Coordinate(1f, 0f)), Is.EqualTo(-90f).Within(Tolerance));

        [Test]
        public void SignedAngle_OfTheSameDirection_IsZero() =>
            Assert.That(Coordinate.SignedAngle(new Coordinate(1f, 1f), new Coordinate(5f, 5f)), Is.EqualTo(0f).Within(Tolerance));

        [Test]
        public void SignedAngle_OfOpposites_IsHalfATurn() =>
            Assert.That(Mathf.Abs(Coordinate.SignedAngle(new Coordinate(1f, 0f), new Coordinate(-3f, 0f))), Is.EqualTo(180f).Within(Tolerance));

        [Test]
        public void SignedAngle_IgnoresLength() =>
            Assert.That(Coordinate.SignedAngle(new Coordinate(100f, 0f), new Coordinate(0f, 0.01f)), Is.EqualTo(90f).Within(Tolerance));

        [Test]
        public void SignedAngle_WithAZeroCoordinate_IsZero()
        {
            Assert.That(Coordinate.SignedAngle(new Coordinate(0f, 0f), new Coordinate(0f, 1f)), Is.EqualTo(0f));
            Assert.That(Coordinate.SignedAngle(new Coordinate(1f, 0f), new Coordinate(0f, 0f)), Is.EqualTo(0f));
        }

        [Test]
        public void RotatingBySignedAngle_PointsAtTheOther()
        {
            var from = new Coordinate(2f, -1f);
            var to = new Coordinate(-3f, 4f);

            var rotated = Coordinate.Rotate(from, Coordinate.SignedAngle(from, to));

            var expected = Coordinate.Normalize(to) * Coordinate.Magnitude(from);
            AssertAt(rotated, expected.x, expected.z);
        }

        // --- Equality ---

        [Test]
        public void EqualCoordinates_HashEqual()
        {
            var a = new Coordinate(1.5f, -2f);
            var b = new Coordinate(1.5f, -2f);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ZeroAndNegativeZero_AreEqualAndHashEqual()
        {
            var a = new Coordinate(0f, 0f);
            var b = new Coordinate(-0f, -0f);

            Assert.That(a.Equals(b), Is.True);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void ANearMiss_ThatEqualsMustHashEqual()
        {
            // Whatever "equal" means, the hash may not disagree with it.
            var a = new Coordinate(1f, 1f);
            var b = new Coordinate(1f + 9.5e-7f, 1f);

            if (a.Equals(b))
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        }

        [Test]
        public void EqualsAndTheEqualityOperators_Agree()
        {
            var a = new Coordinate(1f, 1f);
            var near = new Coordinate(1f + 9.5e-7f, 1f);
            var far = new Coordinate(2f, 1f);
            var same = new Coordinate(1f, 1f);

            Assert.That(a == same, Is.EqualTo(a.Equals(same)));
            Assert.That(a == near, Is.EqualTo(a.Equals(near)));
            Assert.That(a == far, Is.EqualTo(a.Equals(far)));
            Assert.That(a != far, Is.EqualTo(!a.Equals(far)));
            Assert.That(a.Equals((object)same), Is.EqualTo(a.Equals(same)));
        }

        [Test]
        public void DifferentCoordinates_AreNotEqual()
        {
            Assert.That(new Coordinate(1f, 2f).Equals(new Coordinate(2f, 1f)), Is.False);
            Assert.That(new Coordinate(1f, 2f) == new Coordinate(1f, 3f), Is.False);
        }
    }
}
