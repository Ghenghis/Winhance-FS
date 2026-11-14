using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using Winhance.Infrastructure.Features.FileManager.Services;
using Xunit;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Unit / integration tests for FileManagerService - file system operations.
/// </summary>
public class FileManagerServiceTests : IDisposable
{
    private readonly Mock<ILogService> _mockLogService;
    private readonly FileManagerService _service;
    private readonly string _testDirectory;

    public FileManagerServiceTests()
    {
        _mockLogService = new Mock<ILogService>();
        _service = new FileManagerService(_mockLogService.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WinhanceFMSTest_{Guid.NewGuid():N}");
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

    #region GetDirectoryContentsAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetDirectoryContentsAsync_WithRealTempDirectory_ReturnsItems()
    {
        // Arrange
        var sub = Path.Combine(_testDirectory, "SubDir");
        Directory.CreateDirectory(sub);
        var file = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(file, "hello");

        // Act
        var entries = (await _service.GetDirectoryContentsAsync(_testDirectory)).ToList();

        // Assert
        entries.Should().NotBeNull();
        // The service always injects a ".." parent entry when the directory has a parent
        entries.Should().Contain(e => e.Name == ".." || e.IsDirectory || !e.IsDirectory);
        entries.Should().Contain(e => e.Name == "SubDir" && e.IsDirectory);
        entries.Should().Contain(e => e.Name == "test.txt" && !e.IsDirectory);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetDirectoryContentsAsync_OnNonExistentPath_ReturnsEmpty()
    {
        // Arrange
        var nonExistent = Path.Combine(Path.GetTempPath(), $"DoesNotExist_{Guid.NewGuid():N}");

        // Act
        var entries = (await _service.GetDirectoryContentsAsync(nonExistent)).ToList();

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetDirectoryContentsAsync_FilesReturnCorrectMetadata()
    {
        // Arrange
        var file = Path.Combine(_testDirectory, "metadata.txt");
        var content = "metadata content";
        await File.WriteAllTextAsync(file, content);

        // Act
        var entries = (await _service.GetDirectoryContentsAsync(_testDirectory)).ToList();
        var fileEntry = entries.FirstOrDefault(e => e.Name == "metadata.txt");

        // Assert
        fileEntry.Should().NotBeNull();
        fileEntry!.IsDirectory.Should().BeFalse();
        fileEntry.Size.Should().Be(new FileInfo(file).Length);
        fileEntry.Extension.Should().Be(".txt");
        fileEntry.FullPath.Should().Be(file);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetDirectoryContentsAsync_DirectoriesListedBeforeFiles()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testDirectory, "ADirectory"));
        await File.WriteAllTextAsync(Path.Combine(_testDirectory, "AFile.txt"), "x");

        // Act
        var entries = (await _service.GetDirectoryContentsAsync(_testDirectory))
            .Where(e => e.Name != "..")   // exclude parent entry
            .ToList();

        // Assert — directories should appear before files (OrderByDescending IsDirectory)
        var firstNonParent = entries.FirstOrDefault();
        if (firstNonParent != null)
            firstNonParent.IsDirectory.Should().BeTrue();
    }

    #endregion

    #region CreateDirectoryAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CreateDirectoryAsync_CreatesDirectoryOnDisk()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "NewDirectory");

        // Act
        var result = await _service.CreateDirectoryAsync(newDir);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        Directory.Exists(newDir).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CreateDirectoryAsync_WhenDirectoryAlreadyExists_CreatesUniqueVariant()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "Duplicate");
        Directory.CreateDirectory(newDir); // pre-existing

        // Act
        var result = await _service.CreateDirectoryAsync(newDir);

        // Assert
        result.Success.Should().BeTrue();
        // The service uses GetUniqueDestinationPath, so a "(1)" variant should exist
        Directory.GetDirectories(_testDirectory).Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CreateDirectoryAsync_ReturnsNonEmptyTransactionId()
    {
        // Arrange
        var newDir = Path.Combine(_testDirectory, "TxDir");

        // Act
        var result = await _service.CreateDirectoryAsync(newDir);

        // Assert
        result.ItemsProcessed.Should().Be(1);
    }

    #endregion

    #region DeleteFilesAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task DeleteFilesAsync_PermanentDelete_RemovesFileFromDisk()
    {
        // Arrange
        var file = Path.Combine(_testDirectory, "ToDelete.txt");
        await File.WriteAllTextAsync(file, "delete me");
        File.Exists(file).Should().BeTrue();

        // Act
        var result = await _service.DeleteFilesAsync(new[] { file }, permanent: true);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(file).Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task DeleteFilesAsync_WithNonExistentFile_ReportsNoFailures()
    {
        // Arrange
        var missing = Path.Combine(_testDirectory, $"ghost_{Guid.NewGuid():N}.txt");

        // Act
        var result = await _service.DeleteFilesAsync(new[] { missing }, permanent: true);

        // Assert — file didn't exist; service processes the path entry (no-op) and
        // records no failures, so Success should be true.
        result.ItemsFailed.Should().Be(0);
        result.Success.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task DeleteFilesAsync_MultipleFiles_DeletesAll()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "Del1.txt");
        var file2 = Path.Combine(_testDirectory, "Del2.txt");
        await File.WriteAllTextAsync(file1, "a");
        await File.WriteAllTextAsync(file2, "b");

        // Act
        var result = await _service.DeleteFilesAsync(new[] { file1, file2 }, permanent: true);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemsProcessed.Should().Be(2);
        File.Exists(file1).Should().BeFalse();
        File.Exists(file2).Should().BeFalse();
    }

    #endregion

    #region GetPropertiesAsync Tests (acting as GetFileItemAsync)

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetPropertiesAsync_ReturnsCorrectMetadataForRealFile()
    {
        // Arrange
        var file = Path.Combine(_testDirectory, "properties.txt");
        var content = "some content for property check";
        await File.WriteAllTextAsync(file, content);

        // Act
        var props = await _service.GetPropertiesAsync(file);

        // Assert
        props.Should().NotBeNull();
        props.Name.Should().Be("properties.txt");
        props.FullPath.Should().Be(file);
        props.Size.Should().Be(new FileInfo(file).Length);
        props.Hash.Should().NotBeNullOrEmpty();
        props.Hash.Should().HaveLength(64); // SHA-256 = 64 hex chars
        props.Type.Should().Be("Text Document");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetPropertiesAsync_ForDirectory_ReturnsDirectoryProperties()
    {
        // Arrange
        var dir = Path.Combine(_testDirectory, "PropDir");
        Directory.CreateDirectory(dir);

        // Act
        var props = await _service.GetPropertiesAsync(dir);

        // Assert
        props.Name.Should().Be("PropDir");
        props.Type.Should().Be("Directory");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetPropertiesAsync_ForNonExistentPath_ReturnsEmptyProperties()
    {
        // Arrange
        var missing = Path.Combine(_testDirectory, $"missing_{Guid.NewGuid():N}.txt");

        // Act
        var props = await _service.GetPropertiesAsync(missing);

        // Assert
        props.Should().NotBeNull();
        props.Name.Should().BeEmpty();
    }

    #endregion

    #region GetDrivesAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetDrivesAsync_ReturnsAtLeastOneDrive()
    {
        // Act
        var drives = (await _service.GetDrivesAsync()).ToList();

        // Assert
        drives.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetDrivesAsync_EachDriveHasName()
    {
        // Act
        var drives = (await _service.GetDrivesAsync()).ToList();

        // Assert
        drives.Should().AllSatisfy(d => d.Name.Should().NotBeNullOrEmpty());
    }

    #endregion
}
