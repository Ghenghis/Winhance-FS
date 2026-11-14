using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Xunit;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Unit tests for AddressBarService - address bar, breadcrumb, and navigation features.
/// </summary>
public class AddressBarServiceTests : IDisposable
{
    private readonly AddressBarService _service;
    private readonly string _testDirectory;

    public AddressBarServiceTests()
    {
        _service = new AddressBarService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WinhanceAddressBarTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, true);
        }
        catch { /* ignore cleanup errors */ }

        GC.SuppressFinalize(this);
    }

    #region NavigateAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task NavigateAsync_WithValidExistingPath_ReturnsTrueAndUpdatesCurrentPath()
    {
        // Act
        var result = await _service.NavigateAsync(_testDirectory);

        // Assert
        result.Should().BeTrue();
        _service.CurrentPath.Should().Be(Path.GetFullPath(_testDirectory));
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task NavigateAsync_WithNonExistentPath_ReturnsFalse()
    {
        // Arrange
        var nonExistent = Path.Combine(Path.GetTempPath(), $"DoesNotExist_{Guid.NewGuid():N}");

        // Act
        var result = await _service.NavigateAsync(nonExistent);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task NavigateAsync_WithEmptyPath_ReturnsFalse()
    {
        // Act
        var result = await _service.NavigateAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task NavigateAsync_WithValidPath_FiresPathChangedEvent()
    {
        // Arrange
        PathChangedEventArgs? captured = null;
        _service.PathChanged += (_, e) => captured = e;

        // Act
        await _service.NavigateAsync(_testDirectory);

        // Assert
        captured.Should().NotBeNull();
        captured!.NewPath.Should().Be(Path.GetFullPath(_testDirectory));
    }

    #endregion

    #region GetSuggestionsAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetSuggestionsAsync_WithMatchingPathPrefix_ReturnsSuggestions()
    {
        // Arrange — create a known subdirectory so there is at least one match
        var subDir = Path.Combine(_testDirectory, "SubDirForSuggestion");
        Directory.CreateDirectory(subDir);

        // Act — pass the parent directory so it lists its children
        var suggestions = (await _service.GetSuggestionsAsync(_testDirectory)).ToList();

        // Assert
        suggestions.Should().NotBeNull();
        suggestions.Should().Contain(s => s.FullPath.Equals(subDir, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetSuggestionsAsync_WithEmptyInput_ReturnsRecentItems()
    {
        // Arrange — navigate first so there is a recent path
        await _service.NavigateAsync(_testDirectory);

        // Act
        var suggestions = (await _service.GetSuggestionsAsync(string.Empty)).ToList();

        // Assert
        suggestions.Should().NotBeNull();
        // After a successful navigation the recent list is populated, so at least one suggestion exists
        suggestions.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetSuggestionsAsync_RespectsMaxResultsLimit()
    {
        // Act
        var suggestions = (await _service.GetSuggestionsAsync(string.Empty, maxResults: 3)).ToList();

        // Assert
        suggestions.Count.Should().BeLessOrEqualTo(3);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetSuggestionsAsync_SpecialFolderKeyword_ReturnsFolderSuggestion()
    {
        // Act — "desktop" is a recognised shortcut in the service
        var suggestions = (await _service.GetSuggestionsAsync("desktop")).ToList();

        // Assert
        suggestions.Should().Contain(s => s.Type == SuggestionType.SpecialFolder);
    }

    #endregion

    #region GetRecentLocationsAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetRecentLocationsAsync_AfterNavigation_ContainsNavigatedPath()
    {
        // Arrange
        await _service.NavigateAsync(_testDirectory);

        // Act
        var recent = (await _service.GetRecentLocationsAsync()).ToList();

        // Assert
        recent.Should().NotBeEmpty();
        recent.Should().Contain(r => r.FullPath.Equals(Path.GetFullPath(_testDirectory), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetRecentLocationsAsync_AfterClear_IsEmpty()
    {
        // Arrange
        await _service.NavigateAsync(_testDirectory);
        await _service.ClearRecentLocationsAsync();

        // Act
        var recent = (await _service.GetRecentLocationsAsync()).ToList();

        // Assert
        recent.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetRecentLocationsAsync_RespectsMaxResults()
    {
        // Arrange — navigate to several distinct paths
        for (int i = 0; i < 5; i++)
        {
            var sub = Path.Combine(_testDirectory, $"Sub{i}");
            Directory.CreateDirectory(sub);
            await _service.NavigateAsync(sub);
        }

        // Act
        var recent = (await _service.GetRecentLocationsAsync(maxResults: 3)).ToList();

        // Assert
        recent.Count.Should().BeLessOrEqualTo(3);
    }

    #endregion

    #region ParsePath Tests

    [Fact]
    [Trait("Category", "Backend")]
    public void ParsePath_WithWindowsPath_ReturnsBreadcrumbSegmentsIncludingParts()
    {
        // Arrange
        var path = Path.Combine("C:", "Users", "Test");

        // Act
        var segments = _service.ParsePath(path).ToList();

        // Assert
        segments.Should().NotBeEmpty();
        // First segment should be "This PC" root
        segments.First().IsSpecialFolder.Should().BeTrue();
        segments.First().SpecialFolder.Should().Be(SpecialFolderType.ThisPc);
        // Remaining segments should reflect path parts
        var displayNames = segments.Select(s => s.DisplayName).ToList();
        displayNames.Should().ContainSingle(n => n.StartsWith("C"));
        displayNames.Should().Contain("Users");
        displayNames.Should().Contain("Test");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ParsePath_WithEmptyPath_ReturnsEmptyCollection()
    {
        // Act
        var segments = _service.ParsePath(string.Empty).ToList();

        // Assert
        segments.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ParsePath_LastSegment_IsMarkedAsLast()
    {
        // Arrange
        var path = Path.Combine("C:", "Users", "Test");

        // Act
        var segments = _service.ParsePath(path).ToList();

        // Assert
        segments.Last().IsLast.Should().BeTrue();
    }

    #endregion

    #region ExpandPath Tests

    [Fact]
    [Trait("Category", "Backend")]
    public void ExpandPath_WithUserProfileVariable_ResolvesToActualPath()
    {
        // Arrange
        var expected = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var result = _service.ExpandPath("%USERPROFILE%");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ExpandPath_WithTildeAlone_ResolvesToUserProfile()
    {
        // Arrange
        var expected = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Act
        var result = _service.ExpandPath("~");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ExpandPath_WithTildeSlash_CombinesWithUserProfile()
    {
        // Arrange
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Documents");

        // Act
        var result = _service.ExpandPath("~/Documents");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ExpandPath_WithNoVariables_ReturnsInputUnchanged()
    {
        // Arrange
        var input = @"C:\Some\Fixed\Path";

        // Act
        var result = _service.ExpandPath(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ExpandPath_WithEmptyString_ReturnsEmpty()
    {
        // Act
        var result = _service.ExpandPath(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ValidatePath Tests

    [Fact]
    [Trait("Category", "Backend")]
    public void ValidatePath_WithExistingDirectory_IsValidAndExists()
    {
        // Act
        var result = _service.ValidatePath(_testDirectory);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Exists.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ValidatePath_WithNonExistentPath_IsValidButNotExists()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid():N}");

        // Act
        var result = _service.ValidatePath(path);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Exists.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ValidatePath_WithEmptyPath_IsInvalid()
    {
        // Act
        var result = _service.ValidatePath(string.Empty);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion
}
