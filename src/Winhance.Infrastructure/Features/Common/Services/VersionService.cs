using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.Common.Models;

namespace Winhance.Infrastructure.Features.Common.Services
{
    public class VersionService : IVersionService
    {
        private readonly ILogService _logService;
        private readonly HttpClient _httpClient;
        private readonly Func<VersionInfo>? _currentVersionProvider;
        private const string RepositoryOwner = "Ghenghis";
        private const string RepositoryName = "Winhance-FS";
        private const string LatestReleaseApiUrl = $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";
        private const string LatestReleasePageUrl = $"https://github.com/{RepositoryOwner}/{RepositoryName}/releases/latest";
        private const string UserAgent = "Winhance-FS-Update-Checker";
        private string? _latestReleaseDownloadUrl;

        public VersionService(ILogService logService)
            : this(logService, new HttpClient())
        {
        }

        public VersionService(ILogService logService, HttpClient httpClient)
            : this(logService, httpClient, null)
        {
        }

        public VersionService(ILogService logService, HttpClient httpClient, Func<VersionInfo>? currentVersionProvider)
        {
            _logService = logService;
            _httpClient = httpClient;
            _currentVersionProvider = currentVersionProvider;

            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            }
        }

        public VersionInfo GetCurrentVersion()
        {
            if (_currentVersionProvider != null)
            {
                return _currentVersionProvider();
            }

            try
            {
                // Get the assembly version
                Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                string? location = assembly.Location;

                if (string.IsNullOrEmpty(location))
                {
                    _logService.Log(LogLevel.Error, "Could not determine assembly location for version check");
                    return CreateDefaultVersion();
                }

                // Get the InformationalVersion which can include the -beta tag
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(location);
                string version = versionInfo.ProductVersion ?? versionInfo.FileVersion ?? "v0.0.0";

                // Trim any build metadata (anything after the + symbol)
                int plusIndex = version.IndexOf('+');
                if (plusIndex > 0)
                {
                    version = version.Substring(0, plusIndex);
                }

                // If the version doesn't start with 'v', add it
                if (!version.StartsWith("v", StringComparison.Ordinal))
                {
                    version = $"v{version}";
                }

                return VersionInfo.FromTag(version);
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Error getting current version: {ex.Message}", ex);
                return CreateDefaultVersion();
            }
        }

        public async Task<VersionInfo> CheckForUpdateAsync()
        {
            try
            {
                _logService.Log(LogLevel.Info, $"Checking for updates from {RepositoryOwner}/{RepositoryName}...");

                // Get the latest release information from GitHub API
                HttpResponseMessage response = await _httpClient.GetAsync(LatestReleaseApiUrl);
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logService.Log(
                        LogLevel.Info,
                        $"No published releases found for {RepositoryOwner}/{RepositoryName}.");
                    return new VersionInfo { IsUpdateAvailable = false };
                }

                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseBody);

                // Extract the tag name (version) from the response
                string tagName = doc.RootElement.GetProperty("tag_name").GetString() ?? "v0.0.0";
                string htmlUrl = doc.RootElement.GetProperty("html_url").GetString() ?? string.Empty;
                DateTime publishedAt = doc.RootElement.TryGetProperty("published_at", out JsonElement publishedElement) &&
                                      DateTime.TryParse(publishedElement.GetString(), out DateTime published)
                                      ? published
                                      : DateTime.MinValue;

                VersionInfo latestVersion = VersionInfo.FromTag(tagName);
                if (string.IsNullOrWhiteSpace(latestVersion.Version))
                {
                    _logService.Log(LogLevel.Warning, $"Latest release tag '{tagName}' could not be parsed.");
                    return new VersionInfo { IsUpdateAvailable = false };
                }

                if (publishedAt != DateTime.MinValue)
                {
                    latestVersion.ReleaseDate = publishedAt;
                }

                string? assetDownloadUrl = SelectBestReleaseAssetUrl(doc.RootElement);
                _latestReleaseDownloadUrl = assetDownloadUrl;
                latestVersion.DownloadUrl = !string.IsNullOrWhiteSpace(assetDownloadUrl)
                    ? assetDownloadUrl
                    : (!string.IsNullOrWhiteSpace(htmlUrl) ? htmlUrl : LatestReleasePageUrl);

                // Compare with current version
                VersionInfo currentVersion = GetCurrentVersion();
                latestVersion.IsUpdateAvailable =
                    latestVersion.IsNewerThan(currentVersion) &&
                    !string.IsNullOrWhiteSpace(assetDownloadUrl);

                if (latestVersion.IsNewerThan(currentVersion) && string.IsNullOrWhiteSpace(assetDownloadUrl))
                {
                    _logService.Log(
                        LogLevel.Warning,
                        $"Release {latestVersion.Version} exists on {RepositoryOwner}/{RepositoryName}, but it has no downloadable assets.");
                }

                _logService.Log(LogLevel.Info, $"Current version: {currentVersion.Version}, Latest version: {latestVersion.Version}, Update available: {latestVersion.IsUpdateAvailable}");

                return latestVersion;
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Error checking for updates: {ex.Message}", ex);
                return new VersionInfo { IsUpdateAvailable = false };
            }
        }

        public async Task DownloadAndInstallUpdateAsync()
        {
            try
            {
                _logService.Log(LogLevel.Info, "Downloading update...");

                string downloadUrl = _latestReleaseDownloadUrl ?? await GetLatestReleaseAssetDownloadUrlAsync();
                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    throw new InvalidOperationException(
                        $"No downloadable release asset was found for {RepositoryOwner}/{RepositoryName}. Publish an installer or portable ZIP on GitHub Releases first.");
                }

                // Create a temporary file to download the installer
                string fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "Winhance-FS.Update";
                }

                string tempPath = Path.Combine(Path.GetTempPath(), fileName);

                // Download the installer
                byte[] installerBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(tempPath, installerBytes);

                _logService.Log(LogLevel.Info, $"Update downloaded to {tempPath}, launching package...");

                // Launch the installer or open the downloaded package.
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true,
                });

                _logService.Log(LogLevel.Info, "Update package launched successfully");
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Error downloading or installing update: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<string> GetLatestReleaseAssetDownloadUrlAsync()
        {
            HttpResponseMessage response = await _httpClient.GetAsync(LatestReleaseApiUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return string.Empty;
            }

            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseBody);

            return SelectBestReleaseAssetUrl(doc.RootElement) ?? string.Empty;
        }

        private static string? SelectBestReleaseAssetUrl(JsonElement release)
        {
            if (!release.TryGetProperty("assets", out JsonElement assets) ||
                assets.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            string architecture = RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => "arm64",
                Architecture.X86 => "x86",
                _ => "x64",
            };

            string? bestUrl = null;
            int bestScore = int.MinValue;

            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string name = asset.TryGetProperty("name", out JsonElement nameElement)
                    ? nameElement.GetString() ?? string.Empty
                    : string.Empty;
                string url = asset.TryGetProperty("browser_download_url", out JsonElement urlElement)
                    ? urlElement.GetString() ?? string.Empty
                    : string.Empty;

                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                string normalizedName = name.ToLowerInvariant();
                int score = 0;

                if (normalizedName.Contains("winhance", StringComparison.Ordinal))
                {
                    score += 30;
                }

                if (normalizedName.Contains(architecture, StringComparison.Ordinal))
                {
                    score += 20;
                }

                if (normalizedName.Contains("setup", StringComparison.Ordinal) ||
                    normalizedName.Contains("installer", StringComparison.Ordinal))
                {
                    score += 40;
                }

                if (normalizedName.EndsWith(".exe", StringComparison.Ordinal))
                {
                    score += 12;
                }
                else if (normalizedName.EndsWith(".msi", StringComparison.Ordinal))
                {
                    score += 10;
                }
                else if (normalizedName.EndsWith(".zip", StringComparison.Ordinal))
                {
                    score += 8;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestUrl = url;
                }
            }

            return bestUrl;
        }

        private VersionInfo CreateDefaultVersion()
        {
            // Create a default version based on the current date
            DateTime now = DateTime.Now;
            string versionTag = $"v{now.Year - 2000:D2}.{now.Month:D2}.{now.Day:D2}";

            return new VersionInfo
            {
                Version = versionTag,
                ReleaseDate = now,
            };
        }
    }
}
