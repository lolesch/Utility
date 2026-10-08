using NUnit.Framework;
using Submodules.Utility.Extensions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// Pins <see cref="FloatExtensions.Map(float,float,float,float,float)"/> which never leaves the target range,
    /// including the zero-width source range, which has no slope to follow and so answers the low end
    /// of the target range.
    /// </summary>
    [TestFixture]
    public sealed class FloatExtensionsTests
    {
        private const float Tolerance = 1e-4f;

        // A zero-width source is a caller bug, so Map says so; the tests that pin the answer expect the warning.
        private static void ExpectZeroWidthWarning(float bound, int times = 1)
        {
            for (var i = 0; i < times; i++)
                LogAssert.Expect(LogType.Warning, $"{bound} should differ from {bound}");
        }

        // --- Map ---

        [Test]
        public void Map_ScalesAcrossTheRanges() =>
            Assert.That(5f.Map(0f, 10f, 0f, 100f), Is.EqualTo(50f).Within(Tolerance));

        [Test]
        public void Map_AboveTheSource_StopsAtTheHighEnd() =>
            Assert.That(15f.Map(0f, 10f, 0f, 100f), Is.EqualTo(100f).Within(Tolerance));

        [Test]
        public void Map_BelowTheSource_StopsAtTheLowEnd() =>
            Assert.That((-5f).Map(0f, 10f, 0f, 100f), Is.EqualTo(0f).Within(Tolerance));

        [Test]
        public void Map_FollowsADescendingTargetRange() =>
            Assert.That(2.5f.Map(0f, 10f, 100f, 0f), Is.EqualTo(75f).Within(Tolerance));

        [Test]
        public void Map_ZeroWidthSource_ReturnsTheLowEndOfTheTarget()
        {
            ExpectZeroWidthWarning(3f, 3);

            Assert.That(3f.Map(3f, 3f, 10f, 20f), Is.EqualTo(10f).Within(Tolerance));
            Assert.That(7f.Map(3f, 3f, 10f, 20f), Is.EqualTo(10f).Within(Tolerance));
            Assert.That((-7f).Map(3f, 3f, 10f, 20f), Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void Map_ZeroWidthSource_DoesNotShiftTheBound()
        {
            // The old fallback moved fromMin down by one, so a value on the bound landed on the high end.
            ExpectZeroWidthWarning(3f);

            Assert.That(3f.Map(3f, 3f, 10f, 20f), Is.Not.EqualTo(20f).Within(Tolerance));
        }

        [Test]
        public void Map_ZeroWidthSource_WarnsOfTheCallerBug()
        {
            ExpectZeroWidthWarning(3f);

            3f.Map(3f, 3f, 10f, 20f);

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Map_ZeroWidthSource_DescendingTarget_ReturnsTheFirstTargetValue()
        {
            ExpectZeroWidthWarning(3f);

            Assert.That(3f.Map(3f, 3f, 20f, 10f), Is.EqualTo(20f).Within(Tolerance));
        }

        [Test]
        public void Map_Vector2Overload_UsesTheSameZeroWidthRule()
        {
            ExpectZeroWidthWarning(3f);

            Assert.That(3f.Map(new Vector2(3f, 3f), new Vector2(10f, 20f)), Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void MapTo01_ZeroWidthSource_IsZero()
        {
            ExpectZeroWidthWarning(4f);

            Assert.That(4f.MapTo01(4f, 4f), Is.EqualTo(0f).Within(Tolerance));
        }

        // --- Map never leaves the target range ---

        [Test]
        public void Map_DescendingTarget_NeverLeavesTheTargetRange()
        {
            Assert.That(15f.Map(0f, 10f, 100f, 0f), Is.EqualTo(0f).Within(Tolerance));
            Assert.That((-5f).Map(0f, 10f, 100f, 0f), Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void Map_DescendingSource_NeverLeavesTheTargetRange()
        {
            Assert.That(15f.Map(10f, 0f, 0f, 100f), Is.EqualTo(0f).Within(Tolerance));
            Assert.That((-5f).Map(10f, 0f, 0f, 100f), Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void Map_ZeroWidthSource_ClampedValueStillReturnsTheLowEnd()
        {
            ExpectZeroWidthWarning(3f, 2);

            Assert.That(9f.Map(3f, 3f, 10f, 20f), Is.EqualTo(10f).Within(Tolerance));
            Assert.That(3f.Map(3f, 3f, 10f, 20f), Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void Map_Vector2Overload_Clamps() =>
            Assert.That(99f.Map(new Vector2(0f, 10f), new Vector2(5f, 6f)), Is.EqualTo(6f).Within(Tolerance));
    }
}
