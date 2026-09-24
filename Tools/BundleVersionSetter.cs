#if UNITY_EDITOR
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
        internal enum ReleaseType
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
            Patch,
            Minor,
            Major,
            ReleaseType,
        }

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => IncreasePatchNumber();

        // major.minor.patch[_releaseType][+gitHash[-dirty]] — patch orders builds, the
        // +gitHash suffix pins the exact source; strip it before parsing the ordered core.
        internal static void SplitBundleVersion(string bundleVersion, out int major, out int minor, out int patch, out ReleaseType release)
        {
            bundleVersion = bundleVersion.Trim(); //clean up whitespace if necessary
            var core = bundleVersion.Split('+')[0];
            var parts = core.Split('.', '-', '_');

            major = 0;
            minor = 0;
            patch = 0;
            release = ReleaseType.None;

            if (parts.Length > 0)
                int.TryParse(parts[0], out major);
            if (parts.Length > 1)
                int.TryParse(parts[1], out minor);
            if (parts.Length > 2)
                int.TryParse(parts[2], out patch);
            if (parts.Length > 3)
                Enum.TryParse(parts[3], out release);
        }

        internal static string FormatVersion(int major, int minor, int patch, ReleaseType release, string gitHash)
        {
            var versionNumber = $"{major:0}.{minor:0}.{patch:0}";

            if (release is not ReleaseType.None and < ReleaseType.Release)
                versionNumber = $"{versionNumber}_{release}";

            if (!string.IsNullOrEmpty(gitHash))
                versionNumber = $"{versionNumber}+{gitHash}";

            return versionNumber;
        }

        public static string GetVersion()
        {
            SplitBundleVersion(PlayerSettings.bundleVersion, out var major, out var minor, out var patch, out var releaseType);

            var gitHash = PlayerSettings.bundleVersion.Contains('+')
                ? PlayerSettings.bundleVersion[(PlayerSettings.bundleVersion.IndexOf('+') + 1)..]
                : string.Empty;

            return FormatVersion(major, minor, patch, releaseType, gitHash);
        }

        // Shows what the next build would stamp — live git hash/dirty state — without
        // touching patch or PlayerSettings.bundleVersion.
        [MenuItem("ToolSmiths/Version/Preview", false, 800)]
        private static string PreviewVersion()
        {
            SplitBundleVersion(PlayerSettings.bundleVersion, out var major, out var minor, out var patch, out var releaseType);

            var versionNumber = FormatVersion(major, minor, patch, releaseType, GetShortCommitHash());
            Debug.LogWarning($"bundleVersion preview: {versionNumber.Colored(ColorExtensions.Orange)}");
            return versionNumber;
        }

        private static string IncrementBundleVersion(IncrementType increment)
        {
            SplitBundleVersion(PlayerSettings.bundleVersion, out var major, out var minor, out var patch, out var releaseType);

            switch (increment)
            {
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
                    throw new ArgumentOutOfRangeException(nameof(increment), increment, null);
            }

            var versionNumber = FormatVersion(major, minor, patch, releaseType, GetShortCommitHash());

            if (PlayerSettings.bundleVersion != versionNumber)
            {
                PlayerSettings.bundleVersion = versionNumber;
                AssetDatabase.SaveAssets();
            }

            // Labels the commit the build was made from — the same commit +gitHash
            // already points to, just as a human-readable ref instead of a raw hash.
            RunGit($"tag {versionNumber} HEAD");

            Debug.LogWarning($"bundleVersion: {PlayerSettings.bundleVersion.Colored(ColorExtensions.Orange)}");
            return versionNumber;
        }

        private static string GetShortCommitHash()
        {
            var hash = RunGit("rev-parse --short HEAD");

            if (string.IsNullOrEmpty(hash))
                return "N/A";

            var isDirty = !string.IsNullOrEmpty(RunGit("status --porcelain"));
            return isDirty ? $"{hash}-dirty" : hash;
        }

        private static string RunGit(string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("git")
            {
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo);
            string result = process.StandardOutput.ReadToEnd();
            return result.Trim();
        }

        // Not a menu item: patch is a build concern (bumped from OnPreprocessBuild), not
        // manual editor input. See Preview for a non-mutating look at the next version.
        private static string IncreasePatchNumber() => IncrementBundleVersion(IncrementType.Patch);

        [MenuItem("ToolSmiths/Version/Minor Update", false, 801)]
        private static string IncreaseMinorNumber() => IncrementBundleVersion(IncrementType.Minor);

        [MenuItem("ToolSmiths/Version/Major Update", false, 802)]
        private static string IncreaseMajorNumber() => IncrementBundleVersion(IncrementType.Major);

        [MenuItem("ToolSmiths/Version/Increase ReleaseType", false, 803)]
        private static string IncreaseReleaseType() => IncrementBundleVersion(IncrementType.ReleaseType);
    }
}
#endif
