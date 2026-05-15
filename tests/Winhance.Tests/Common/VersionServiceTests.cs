using System.Net;
using System.Net.Http;
using FluentAssertions;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.Common.Models;
using Winhance.Infrastructure.Features.Common.Services;

namespace Winhance.Tests.Common;

public class VersionServiceTests
{
    [Fact]
    public async Task CheckForUpdateAsync_UsesWinhanceFsRepositoryAndReleaseAsset()
    {
        const string releaseJson = """
            {
              "tag_name": "v99.0.0",
              "html_url": "https://github.com/Ghenghis/Winhance-FS/releases/tag/v99.0.0",
              "published_at": "2026-05-15T12:00:00Z",
              "assets": [
                {
                  "name": "Winhance-x86-v99.0.0.zip",
                  "browser_download_url": "https://github.com/Ghenghis/Winhance-FS/releases/download/v99.0.0/Winhance-x86-v99.0.0.zip"
                },
                {
                  "name": "Winhance-x64-v99.0.0.zip",
                  "browser_download_url": "https://github.com/Ghenghis/Winhance-FS/releases/download/v99.0.0/Winhance-x64-v99.0.0.zip"
                }
              ]
            }
            """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(releaseJson),
        });
        using var httpClient = new HttpClient(handler);
        var service = new VersionService(new TestLogService(), httpClient, () => VersionInfo.FromTag("v0.0.0"));

        var latestVersion = await service.CheckForUpdateAsync();

        handler.RequestUris.Should().ContainSingle(uri =>
            uri == new Uri("https://api.github.com/repos/Ghenghis/Winhance-FS/releases/latest"));
        latestVersion.IsUpdateAvailable.Should().BeTrue();
        latestVersion.Version.Should().Be("v99.0.0");
        latestVersion.DownloadUrl.Should().Be("https://github.com/Ghenghis/Winhance-FS/releases/download/v99.0.0/Winhance-x64-v99.0.0.zip");
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenRepositoryHasNoReleases_ReturnsNoUpdate()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        using var httpClient = new HttpClient(handler);
        var service = new VersionService(new TestLogService(), httpClient);

        var latestVersion = await service.CheckForUpdateAsync();

        latestVersion.IsUpdateAvailable.Should().BeFalse();
        latestVersion.Version.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenLatestReleaseHasNoAssets_DoesNotShowBrokenUpdate()
    {
        const string releaseJson = """
            {
              "tag_name": "v99.0.0",
              "html_url": "https://github.com/Ghenghis/Winhance-FS/releases/tag/v99.0.0",
              "published_at": "2026-05-15T12:00:00Z",
              "assets": []
            }
            """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(releaseJson),
        });
        using var httpClient = new HttpClient(handler);
        var service = new VersionService(new TestLogService(), httpClient);

        var latestVersion = await service.CheckForUpdateAsync();

        latestVersion.IsUpdateAvailable.Should().BeFalse();
        latestVersion.DownloadUrl.Should().Be("https://github.com/Ghenghis/Winhance-FS/releases/tag/v99.0.0");
    }

    [Fact]
    public async Task CheckForUpdateAsync_WhenInstallerAndZipExist_PrefersSetupInstaller()
    {
        const string releaseJson = """
            {
              "tag_name": "v1.0.0",
              "html_url": "https://github.com/Ghenghis/Winhance-FS/releases/tag/v1.0.0",
              "published_at": "2026-05-15T12:00:00Z",
              "assets": [
                {
                  "name": "Winhance-x64-v99.0.0.zip",
                  "browser_download_url": "https://github.com/Ghenghis/Winhance-FS/releases/download/v99.0.0/Winhance-x64-v99.0.0.zip"
                },
                {
                  "name": "Winhance-Setup-v99.0.0.exe",
                  "browser_download_url": "https://github.com/Ghenghis/Winhance-FS/releases/download/v99.0.0/Winhance-Setup-v99.0.0.exe"
                }
              ]
            }
            """;
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(releaseJson),
        });
        using var httpClient = new HttpClient(handler);
        var service = new VersionService(new TestLogService(), httpClient, () => VersionInfo.FromTag("v0.0.0"));

        var latestVersion = await service.CheckForUpdateAsync();

        latestVersion.IsUpdateAvailable.Should().BeTrue();
        latestVersion.DownloadUrl.Should().Be("https://github.com/Ghenghis/Winhance-FS/releases/download/v99.0.0/Winhance-Setup-v99.0.0.exe");
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        public List<Uri> RequestUris { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri != null)
            {
                RequestUris.Add(request.RequestUri);
            }

            return Task.FromResult(send(request));
        }
    }

    private sealed class TestLogService : ILogService
    {
        public event EventHandler<LogMessageEventArgs>? LogMessageGenerated;

        public string GetLogPath() => string.Empty;

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            LogMessageGenerated?.Invoke(
                this,
                new LogMessageEventArgs(level, message, exception));
        }

        public void LogError(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);

        public void LogInformation(string message) => Log(LogLevel.Info, message);

        public Task LogPerformanceAsync(string operation, TimeSpan duration, string? source = null) => Task.CompletedTask;

        public void LogSuccess(string message) => Log(LogLevel.Success, message);

        public void LogWarning(string message) => Log(LogLevel.Warning, message);

        public void StartLog()
        {
        }

        public void StopLog()
        {
        }
    }
}
