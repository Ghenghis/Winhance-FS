using FluentAssertions;
using Winhance.Core.Features.Common.Models;

namespace Winhance.Tests.Common;

public class VersionInfoTests
{
    [Theory]
    [InlineData("v1.0.0", "v1.0.0")]
    [InlineData("v0.1.0-alpha", "v0.1.0-alpha")]
    [InlineData("25.12.12", "v25.12.12")]
    public void FromTag_ParsesSemanticAndDateStyleTags(string tag, string expectedVersion)
    {
        var version = VersionInfo.FromTag(tag);

        version.Version.Should().Be(expectedVersion);
    }

    [Fact]
    public void IsNewerThan_UsesReleaseDateWhenAvailable()
    {
        var current = VersionInfo.FromTag("v25.12.12");
        var latest = VersionInfo.FromTag("v1.0.0");
        latest.ReleaseDate = new DateTime(2026, 5, 15);

        latest.IsNewerThan(current).Should().BeTrue();
    }

    [Fact]
    public void IsNewerThan_FallsBackToSemanticVersionComparison()
    {
        var current = VersionInfo.FromTag("v1.2.0");
        var latest = VersionInfo.FromTag("v1.3.0");

        latest.IsNewerThan(current).Should().BeTrue();
    }

    [Fact]
    public void IsNewerThan_WhenVersionsMatch_ReturnsFalseEvenWithPublishDate()
    {
        var current = VersionInfo.FromTag("v1.0.0");
        var latest = VersionInfo.FromTag("v1.0.0");
        latest.ReleaseDate = new DateTime(2026, 5, 15);

        latest.IsNewerThan(current).Should().BeFalse();
    }

    [Fact]
    public void IsNewerThan_WhenCurrentSemanticVersionIsHigher_ReturnsFalse()
    {
        var current = VersionInfo.FromTag("v2.0.0");
        var latest = VersionInfo.FromTag("v1.0.0");
        latest.ReleaseDate = new DateTime(2026, 5, 15);

        latest.IsNewerThan(current).Should().BeFalse();
    }

    [Fact]
    public void IsNewerThan_WhenMovingFromDateStyleVersionToPublishedSemanticRelease_ReturnsTrue()
    {
        var current = VersionInfo.FromTag("v25.12.12");
        var latest = VersionInfo.FromTag("v1.0.0");
        latest.ReleaseDate = new DateTime(2026, 5, 15);

        latest.IsNewerThan(current).Should().BeTrue();
    }
}
