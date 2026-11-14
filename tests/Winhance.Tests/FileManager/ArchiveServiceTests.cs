using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Winhance.Core.Features.FileManager.Models;
using Winhance.Infrastructure.Features.FileManager.Services;
using Xunit;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Unit / integration tests for ArchiveService - archive creation, extraction, and inspection.
/// </summary>
public class ArchiveServiceTests : IDisposable
{
    private readonly ArchiveService _service;
    private readonly string _testDirectory;

    public ArchiveServiceTests()
    {
        _service = new ArchiveService();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WinhanceArchiveTest_{Guid.NewGuid():N}");
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

    // ------------------------------------------------------------------
    // Helper: create a zip archive from in-memory content for test reuse
    // ------------------------------------------------------------------
    private async Task<string> CreateTestZipAsync(int fileCount = 2)
    {
        var archivePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid():N}.zip");

        // Create source files
        var sourceDir = Path.Combine(_testDirectory, $"src_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sourceDir);
        for (int i = 0; i < fileCount; i++)
        {
            var f = Path.Combine(sourceDir, $"file{i}.txt");
            await File.WriteAllTextAsync(f, $"content of file {i}");
        }

        await _service.CreateArchiveAsync(archivePath, new[] { sourceDir });
        return archivePath;
    }

    #region IsArchive Tests

    [Fact]
    [Trait("Category", "Backend")]
    public void IsArchive_ZipExtension_ReturnsTrue()
    {
        _service.IsArchive("archive.zip").Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void IsArchive_RarExtension_ReturnsTrue()
    {
        _service.IsArchive("archive.rar").Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void IsArchive_TxtExtension_ReturnsFalse()
    {
        _service.IsArchive("document.txt").Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void IsArchive_ExeExtension_ReturnsFalse()
    {
        _service.IsArchive("setup.exe").Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void IsArchive_EmptyString_ReturnsFalse()
    {
        _service.IsArchive(string.Empty).Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void IsArchive_SevenZipExtension_ReturnsTrue()
    {
        _service.IsArchive("archive.7z").Should().BeTrue();
    }

    #endregion

    #region CreateArchiveAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CreateArchiveAsync_CreatesZipFileOnDisk()
    {
        // Arrange
        var archivePath = Path.Combine(_testDirectory, "created.zip");
        var srcFile = Path.Combine(_testDirectory, "source.txt");
        await File.WriteAllTextAsync(srcFile, "hello archive");

        // Act
        var result = await _service.CreateArchiveAsync(archivePath, new[] { srcFile });

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        File.Exists(archivePath).Should().BeTrue();
        new FileInfo(archivePath).Length.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CreateArchiveAsync_ArchiveContainsSourceFiles()
    {
        // Arrange
        var archivePath = Path.Combine(_testDirectory, "contents.zip");
        var srcFile = Path.Combine(_testDirectory, "entry.txt");
        await File.WriteAllTextAsync(srcFile, "check this entry");

        // Act
        await _service.CreateArchiveAsync(archivePath, new[] { srcFile });

        // Assert - verify using ZipFile directly
        using var zip = ZipFile.OpenRead(archivePath);
        zip.Entries.Should().Contain(e => e.Name == "entry.txt");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CreateArchiveAsync_WithDirectory_ReturnsSuccessAndPositiveFilesAdded()
    {
        // Arrange
        var archivePath = Path.Combine(_testDirectory, "fromdir.zip");
        var srcDir = Path.Combine(_testDirectory, "srcdir");
        Directory.CreateDirectory(srcDir);
        await File.WriteAllTextAsync(Path.Combine(srcDir, "a.txt"), "a");
        await File.WriteAllTextAsync(Path.Combine(srcDir, "b.txt"), "b");

        // Act
        var result = await _service.CreateArchiveAsync(archivePath, new[] { srcDir });

        // Assert
        result.Success.Should().BeTrue();
        result.FilesAdded.Should().Be(2);
    }

    #endregion

    #region ExtractAllAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task ExtractAllAsync_ExtractsFilesToDestination()
    {
        // Arrange
        var archivePath = await CreateTestZipAsync(fileCount: 3);
        var destDir = Path.Combine(_testDirectory, $"extract_{Guid.NewGuid():N}");

        // Act
        var result = await _service.ExtractAllAsync(archivePath, destDir);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.EntriesExtracted.Should().Be(3);
        Directory.Exists(destDir).Should().BeTrue();
        Directory.EnumerateFiles(destDir, "*", SearchOption.AllDirectories)
            .Should().HaveCount(3);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task ExtractAllAsync_CreatesDestinationDirectory()
    {
        // Arrange
        var archivePath = await CreateTestZipAsync();
        var destDir = Path.Combine(_testDirectory, $"newdir_{Guid.NewGuid():N}");
        Directory.Exists(destDir).Should().BeFalse(); // pre-condition

        // Act
        await _service.ExtractAllAsync(archivePath, destDir);

        // Assert
        Directory.Exists(destDir).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task ExtractAllAsync_NonZipArchive_ReturnsError()
    {
        // Arrange
        var fakePath = Path.Combine(_testDirectory, "archive.rar");
        var destDir = Path.Combine(_testDirectory, "rar_out");

        // Act
        var result = await _service.ExtractAllAsync(fakePath, destDir);

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    #endregion

    #region ListContentsAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task ListContentsAsync_ReturnsCorrectEntryCount()
    {
        // Arrange
        var archivePath = await CreateTestZipAsync(fileCount: 4);

        // Act
        var entries = (await _service.ListContentsAsync(archivePath)).ToList();

        // Assert
        entries.Should().NotBeNull();
        entries.Count.Should().Be(4);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task ListContentsAsync_EntriesHaveNames()
    {
        // Arrange
        var archivePath = await CreateTestZipAsync(fileCount: 2);

        // Act
        var entries = (await _service.ListContentsAsync(archivePath)).ToList();

        // Assert
        entries.Should().AllSatisfy(e => e.Name.Should().NotBeNullOrEmpty());
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task ListContentsAsync_NonZipArchive_ReturnsEmptyList()
    {
        // Arrange
        var fakePath = Path.Combine(_testDirectory, "fake.rar");

        // Act
        var entries = (await _service.ListContentsAsync(fakePath)).ToList();

        // Assert
        entries.Should().BeEmpty();
    }

    #endregion

    #region GetArchiveInfoAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetArchiveInfoAsync_ReturnsCorrectEntryCount()
    {
        // Arrange
        var archivePath = await CreateTestZipAsync(fileCount: 3);

        // Act
        var info = await _service.GetArchiveInfoAsync(archivePath);

        // Assert
        info.Should().NotBeNull();
        info.EntryCount.Should().Be(3);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetArchiveInfoAsync_ReturnsCorrectFormat()
    {
        // Arrange
        var archivePath = await CreateTestZipAsync();

        // Act
        var info = await _service.GetArchiveInfoAsync(archivePath);

        // Assert
        info.Format.Should().Be("ZIP");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task GetArchiveInfoAsync_SizeIsPositive()
    {
        // Arrange
        var archivePath = await CreateTestZipAsync();

        // Act
        var info = await _service.GetArchiveInfoAsync(archivePath);

        // Assert
        info.Size.Should().BeGreaterThan(0);
    }

    #endregion

    #region TestArchiveAsync Tests

    [Fact]
    [Trait("Category", "Backend")]
    public async Task TestArchiveAsync_ValidArchive_ReturnsValidResult()
    {
        // Arrange
        var archivePath = await CreateTestZipAsync();

        // Act
        var result = await _service.TestArchiveAsync(archivePath);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.EntriesFailed.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task TestArchiveAsync_NonZipPath_ReturnsInvalidResult()
    {
        // Arrange
        var fakePath = Path.Combine(_testDirectory, "bad.rar");

        // Act
        var result = await _service.TestArchiveAsync(fakePath);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    #endregion

    #region SupportedFormats Tests

    [Fact]
    [Trait("Category", "Backend")]
    public void SupportedFormats_ContainsZip()
    {
        _service.SupportedFormats.Should().Contain(f => f.Name == "ZIP");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void SupportedFormats_ZipCanCreateAndExtract()
    {
        var zip = _service.SupportedFormats.First(f => f.Name == "ZIP");
        zip.CanCreate.Should().BeTrue();
        zip.CanExtract.Should().BeTrue();
    }

    #endregion
}
