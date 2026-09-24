using NUnit.Framework;
using static Submodules.Utility.Tools.BundleVersionSetter;

namespace Submodules.Utility.Tests.EditMode
{
    /// <summary>
    /// <see cref="SplitBundleVersion"/> and <see cref="FormatVersion"/> are the pure core
    /// behind the version stamp — parsing/formatting
    /// <c>major.minor.patch[_releaseType][+gitHash]</c> with no PlayerSettings or git
    /// dependency, so the separator and suffix rules can be pinned down without a build.
    /// </summary>
    [TestFixture]
    public sealed class BundleVersionSetterTests
    {
        [Test]
        public void FormatVersion_WithNoReleaseOrHash_IsJustTheCoreNumbers() =>
            Assert.That(FormatVersion(1, 2, 3, ReleaseType.None, ""), Is.EqualTo("1.2.3"));

        [Test]
        public void FormatVersion_BetweenNoneAndRelease_AppendsTheReleaseTypeSuffix() =>
            Assert.That(FormatVersion(1, 2, 3, ReleaseType.Beta, ""), Is.EqualTo("1.2.3_Beta"));

        [Test]
        public void FormatVersion_AtRelease_OmitsTheSuffix_SinceGoldNeedsNoLabel() =>
            Assert.That(FormatVersion(1, 0, 0, ReleaseType.Release, ""), Is.EqualTo("1.0.0"));

        [Test]
        public void FormatVersion_WithAGitHash_AppendsItAfterAPlus() =>
            Assert.That(FormatVersion(1, 2, 3, ReleaseType.None, "abc1234"), Is.EqualTo("1.2.3+abc1234"));

        [Test]
        public void FormatVersion_WithReleaseAndHash_OrdersReleaseBeforeHash() =>
            Assert.That(FormatVersion(1, 2, 3, ReleaseType.Beta, "abc1234-dirty"), Is.EqualTo("1.2.3_Beta+abc1234-dirty"));

        [Test]
        public void SplitBundleVersion_ParsesEachPartOfAFullVersionString()
        {
            SplitBundleVersion("1.2.3_Beta+abc1234-dirty", out var major, out var minor, out var patch, out var release);

            Assert.That(major, Is.EqualTo(1));
            Assert.That(minor, Is.EqualTo(2));
            Assert.That(patch, Is.EqualTo(3));
            Assert.That(release, Is.EqualTo(ReleaseType.Beta));
        }

        [Test]
        public void SplitBundleVersion_StripsTheGitHashSuffix_BeforeParsingTheCore()
        {
            SplitBundleVersion("1.2.3+abc1234-dirty", out _, out _, out var patch, out var release);

            Assert.That(patch, Is.EqualTo(3));
            Assert.That(release, Is.EqualTo(ReleaseType.None));
        }

        [Test]
        public void SplitBundleVersion_DefaultsMissingParts_ToZeroAndNone()
        {
            SplitBundleVersion("1", out var major, out var minor, out var patch, out var release);

            Assert.That(major, Is.EqualTo(1));
            Assert.That(minor, Is.EqualTo(0));
            Assert.That(patch, Is.EqualTo(0));
            Assert.That(release, Is.EqualTo(ReleaseType.None));
        }

        [Test]
        public void SplitBundleVersion_OnAnEmptyString_DefaultsEverythingToZero()
        {
            SplitBundleVersion("", out var major, out var minor, out var patch, out var release);

            Assert.That(major, Is.EqualTo(0));
            Assert.That(minor, Is.EqualTo(0));
            Assert.That(patch, Is.EqualTo(0));
            Assert.That(release, Is.EqualTo(ReleaseType.None));
        }

        [Test]
        public void SplitBundleVersion_TrimsSurroundingWhitespace()
        {
            SplitBundleVersion(" 1.2.3 ", out var major, out var minor, out var patch, out _);

            Assert.That(major, Is.EqualTo(1));
            Assert.That(minor, Is.EqualTo(2));
            Assert.That(patch, Is.EqualTo(3));
        }

        [Test]
        public void SplitBundleVersion_OnAnUnrecognisedReleaseToken_FallsBackToNone()
        {
            SplitBundleVersion("1.2.3.NotAReleaseType", out _, out _, out _, out var release);

            Assert.That(release, Is.EqualTo(ReleaseType.None));
        }

        [Test]
        public void FormatVersion_ThenSplit_RoundTripsTheCoreNumbers()
        {
            var formatted = FormatVersion(3, 7, 12, ReleaseType.ReleaseCandidate, "deadbee-dirty");

            SplitBundleVersion(formatted, out var major, out var minor, out var patch, out var release);

            Assert.That(major, Is.EqualTo(3));
            Assert.That(minor, Is.EqualTo(7));
            Assert.That(patch, Is.EqualTo(12));
            Assert.That(release, Is.EqualTo(ReleaseType.ReleaseCandidate));
        }
    }
}
