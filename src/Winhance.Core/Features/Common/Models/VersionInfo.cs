using System;

namespace Winhance.Core.Features.Common.Models
{
    public class VersionInfo
    {
        public string Version { get; set; } = string.Empty;

        public DateTime ReleaseDate { get; set; }

        public string DownloadUrl { get; set; } = string.Empty;

        public bool IsUpdateAvailable { get; set; }

        public static VersionInfo FromTag(string tag)
        {
            // Supports date-style tags such as v25.05.02 and semantic tags such as v1.0.0-alpha.
            if (string.IsNullOrWhiteSpace(tag))
            {
                return new VersionInfo();
            }

            string normalizedTag = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? tag
                : $"v{tag}";
            string versionString = normalizedTag.Substring(1); // Remove 'v' prefix

            // Strip prerelease and build metadata before parsing numeric components.
            versionString = versionString.Split('-')[0].Split('+')[0];

            string[] parts = versionString.Split('.');

            if (parts.Length < 2 || parts.Length > 4)
            {
                return new VersionInfo();
            }

            foreach (string part in parts)
            {
                if (!int.TryParse(part, out _))
                {
                    return new VersionInfo();
                }
            }

            DateTime releaseDate = DateTime.MinValue;
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int year) &&
                int.TryParse(parts[1], out int month) &&
                int.TryParse(parts[2], out int day) &&
                year >= 20)
            {
                try
                {
                    releaseDate = new DateTime(2000 + year, month, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Semantic versions such as v1.0.0 are still valid; they just do not encode a date.
                }
            }

            return new VersionInfo
            {
                Version = normalizedTag, // Keep prerelease suffixes such as -alpha or -beta.
                ReleaseDate = releaseDate,
            };
        }

        public bool IsNewerThan(VersionInfo other)
        {
            if (other == null)
            {
                return true;
            }

            if (string.Equals(
                NormalizeVersion(Version),
                NormalizeVersion(other.Version),
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (ReleaseDate != DateTime.MinValue && other.ReleaseDate != DateTime.MinValue)
            {
                return ReleaseDate > other.ReleaseDate;
            }

            if (TryParseComparableVersion(Version, out Version? thisVersion) &&
                TryParseComparableVersion(other.Version, out Version? otherVersion))
            {
                return thisVersion > otherVersion;
            }

            if (ReleaseDate != DateTime.MinValue || other.ReleaseDate != DateTime.MinValue)
            {
                return ReleaseDate > other.ReleaseDate;
            }

            return false;
        }

        private static string NormalizeVersion(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return string.Empty;
            }

            string versionString = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? tag.Substring(1)
                : tag;

            return versionString.Split('+')[0];
        }

        private static bool TryParseComparableVersion(string tag, out Version? version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            string versionString = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? tag.Substring(1)
                : tag;
            versionString = versionString.Split('-')[0].Split('+')[0];

            string[] parts = versionString.Split('.');
            if (parts.Length == 2)
            {
                versionString += ".0";
            }

            return System.Version.TryParse(versionString, out version);
        }

        public override string ToString()
        {
            return Version;
        }
    }
}
