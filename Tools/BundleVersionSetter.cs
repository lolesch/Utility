using System;
using System.Diagnostics;
using Submodules.Utility.Extensions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace Submodules.Utility.Tools
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
            GitHash,
            Minor,
            Major,
            ReleaseType,
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => UpdateGitHash();

        private static void SplitBundleVersion(out int major, out int minor, out string patch, out ReleaseType release)
        {
            var bundleVersion = PlayerSettings.bundleVersion;

            bundleVersion = bundleVersion.Trim(); //clean up whitespace if necessary
            var parts = bundleVersion.Split('.', '-', '_');

            major = 0;
            minor = 0;
            patch = "N/A";
            release = ReleaseType.None;

            if (parts.Length > 0)
                int.TryParse(parts[0], out major);
            if (parts.Length > 1)
                int.TryParse(parts[1], out minor);
            if (parts.Length > 2)
                patch = parts[2];
            if (parts.Length > 3)
                Enum.TryParse(parts[3], out release);
        }

        public static string GetVersion()
        {
            SplitBundleVersion(out var major, out var minor, out var patch, out var releaseType);
            
            var versionNumber = $"{major:0}.{minor:0}.{patch}";
            
            if (releaseType is not ReleaseType.None and < ReleaseType.Release)
                versionNumber = $"{versionNumber}_{releaseType}";
            
            return versionNumber;
        }

        private static string IncrementBundleVersion( IncrementType increment )
        {
            SplitBundleVersion(out var major, out var minor, out var patch, out var releaseType);

            switch ( increment )
            {
                case IncrementType.GitHash:
                    break;
                case IncrementType.Minor:
                    minor++;
                    break;
                case IncrementType.Major:
                    major++;
                    minor = 0;
                    break;
                case IncrementType.ReleaseType:
                    releaseType++;
                    major = 0;
                    minor = 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException( nameof(increment), increment, null );
            }
            
            patch = GetShortCommitHash();

            var versionNumber = $"{major:0}.{minor:0}.{patch}";

            if (releaseType is not ReleaseType.None and < ReleaseType.Release)
                versionNumber = $"{versionNumber}_{releaseType}";

            if( PlayerSettings.bundleVersion != versionNumber )
            {
                PlayerSettings.bundleVersion = versionNumber;
                AssetDatabase.SaveAssets();
            }
            
            Debug.LogWarning($"bundleVersion: {PlayerSettings.bundleVersion.Colored(ColorExtensions.Orange)}");
            return versionNumber;
        }
        
        private static string GetShortCommitHash()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("git")
            {
                Arguments = "rev-parse --short HEAD", 
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo);
            string result = process.StandardOutput.ReadToEnd();
            result.Trim(); // returns something like "734713b"
            
            return string.IsNullOrEmpty( result ) ? "N/A" : result;
        }


        [MenuItem("VersionNumber/Update GitHash", false, 800)]
        private static string UpdateGitHash() => IncrementBundleVersion( IncrementType.GitHash);

        [MenuItem("VersionNumber/Increase Minor Number", false, 801)]
        private static string IncreaseMinorNumber() => IncrementBundleVersion( IncrementType.Minor);

        [MenuItem("VersionNumber/Increase Major Number", false, 802)]
        private static string IncreaseMajorNumber() => IncrementBundleVersion( IncrementType.Major);

        [MenuItem("VersionNumber/Increase ReleaseType", false, 803)]
        private static string IncreaseReleaseType() => IncrementBundleVersion( IncrementType.ReleaseType);
    }
}
