using System;
using Submodules.Utility.Extensions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Submodules.Utility
{
    public sealed class BundleVersionSetter : IPreprocessBuildWithReport
    {
        private enum ReleaseType
        {
            None = 0,
            PreAlpha = 1, // Prototype
            Alpha = 2,
            Beta = 3,
            ReleaseCandidate = 4,
            Release = 5, // Gold
        }

        private enum IncrementType
        {
            TimeStamp,
            Patch,
            Minor,
            Major,
            ReleaseType,
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => IncreasePatchNumber();

        private static void SplitBundleVersion(out int major, out int minor, out int patch, out ReleaseType release, out string timeStamp)
        {
            var bundleVersion = PlayerSettings.bundleVersion;

            bundleVersion = bundleVersion.Trim(); //clean up whitespace if necessary
            var parts = bundleVersion.Split('.', '-', '_');

            major = 0;
            minor = 0;
            patch = 0;
            release = ReleaseType.None;
            timeStamp = string.Empty;

            if (parts.Length > 0)
                int.TryParse(parts[0], out major);
            if (parts.Length > 1)
                int.TryParse(parts[1], out minor);
            if (parts.Length > 2)
                int.TryParse(parts[2], out patch);
            if (parts.Length > 3)
                Enum.TryParse(parts[3], out release);
            if (parts.Length > 4)
                timeStamp = parts[4];
        }

        public static string GetVersion()
        {
            SplitBundleVersion(out var major, out var minor, out var patch, out var releaseType, out var timeStamp);
            
            var versionNumber = $"{major:0}.{minor:0}.{patch:0}";
            
            if (releaseType is not ReleaseType.None and < ReleaseType.Release)
                versionNumber = $"{versionNumber}_{releaseType}";
            
            return versionNumber;
        }

        private static string IncrementBundleVersion( IncrementType increment )
        {
            SplitBundleVersion(out var major, out var minor, out var patch, out var releaseType, out var timeStamp);

            switch ( increment )
            {
                case IncrementType.TimeStamp:
                    break;
                case IncrementType.Patch:
                    patch++;
                    break;
                case IncrementType.Minor:
                    minor++;
                    patch = 0;
                    break;
                case IncrementType.Major:
                    major++;
                    minor = 0;
                    patch = 0;
                    break;
                case IncrementType.ReleaseType:
                    releaseType++;
                    major = 0;
                    minor = 0;
                    patch = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException( nameof(increment), increment, null );
            }

            var versionNumber = $"{major:0}.{minor:0}.{patch:0}";

            if (releaseType is not ReleaseType.None and < ReleaseType.Release)
                versionNumber = $"{versionNumber}_{releaseType}";

            timeStamp = DateTime.UtcNow.ToString("yyMMddHHmm");
            
            if (PlayerSettings.bundleVersion != $"{versionNumber}_{timeStamp}")
            {
                PlayerSettings.bundleVersion = $"{versionNumber}_{timeStamp}";
                Debug.LogWarning($"bundleVersion: {PlayerSettings.bundleVersion.Colored(ColorExtensions.Orange)}");
            }
            return versionNumber;
        }

        [MenuItem("VersionNumber/Increase Patch Number", false, 800)]
        private static string IncreasePatchNumber() => IncrementBundleVersion( IncrementType.Patch);

        [MenuItem("VersionNumber/Increase Minor Number", false, 801)]
        private static string IncreaseMinorNumber() => IncrementBundleVersion( IncrementType.Minor);

        [MenuItem("VersionNumber/Increase Major Number", false, 802)]
        private static string IncreaseMajorNumber() => IncrementBundleVersion( IncrementType.Major);

        [MenuItem("VersionNumber/Increase ReleaseType", false, 803)]
        private static string IncreaseReleaseType() => IncrementBundleVersion( IncrementType.ReleaseType);

        [MenuItem("VersionNumber/Update TimeStamp", false, 804)]
        private static string UpdateTimeStamp() => IncrementBundleVersion( IncrementType.TimeStamp);
    }
}
