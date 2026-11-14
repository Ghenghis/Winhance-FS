using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Xunit;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Unit tests for BackupProtectionService - Fail-safe backup protection features.
/// </summary>
public class BackupProtectionServiceTests : IDisposable
{
    private readonly Mock<ILogService> _mockLogService;
    private readonly BackupProtectionService _service;
    private readonly string _testDirectory;

    public BackupProtectionServiceTests()
    {
        _mockLogService = new Mock<ILogService>();
        _service = new BackupProtectionService(_mockLogService.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"WinhanceBackupTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }

        GC.SuppressFinalize(this);
    }

    #region IsEnabled Tests (BKP-001 to BKP-003)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-001")]
    public void IsEnabled_DefaultValue_IsTrue()
    {
        // Assert
        _service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-002")]
    public void IsEnabled_CanBeSet_ToFalse()
    {
        // Act
        _service.IsEnabled = false;

        // Assert
        _service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-003")]
    public void ProtectionLevel_DefaultValue_IsStandard()
    {
        // Assert
        _service.ProtectionLevel.Should().Be(BackupProtectionLevel.Standard);
    }

    #endregion

    #region CreateBackupAsync Tests (BKP-004 to BKP-010)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-004")]
    public async Task CreateBackupAsync_WithValidFile_CreatesBackupSession()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Move,
        };

        // Act
        var session = await _service.CreateBackupAsync(operation);

        // Assert
        session.Should().NotBeNull();
        session.SessionId.Should().NotBeNullOrEmpty();
        session.Status.Should().Be(BackupSessionStatus.Active);
        session.Files.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-005")]
    public async Task CreateBackupAsync_WithDisabledProtection_ReturnsCommittedSession()
    {
        // Arrange
        _service.IsEnabled = false;
        var testFile = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Delete,
        };

        // Act
        var session = await _service.CreateBackupAsync(operation);

        // Assert
        session.Status.Should().Be(BackupSessionStatus.Committed);
        session.Files.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-006")]
    public async Task CreateBackupAsync_WithNonExistentFile_CreatesEmptySession()
    {
        // Arrange
        var operation = new FileOperationRequest
        {
            SourcePath = Path.Combine(_testDirectory, "nonexistent.txt"),
            OperationType = FileOperationType.Delete,
        };

        // Act
        var session = await _service.CreateBackupAsync(operation);

        // Assert
        session.Should().NotBeNull();
        session.Files.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-007")]
    public async Task CreateBatchBackupAsync_WithMultipleFiles_BacksUpAll()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "file1.txt");
        var file2 = Path.Combine(_testDirectory, "file2.txt");
        await File.WriteAllTextAsync(file1, "Content 1");
        await File.WriteAllTextAsync(file2, "Content 2");

        var operations = new[]
        {
            new FileOperationRequest { SourcePath = file1, OperationType = FileOperationType.Move },
            new FileOperationRequest { SourcePath = file2, OperationType = FileOperationType.Move },
        };

        // Act
        var session = await _service.CreateBatchBackupAsync(operations);

        // Assert
        session.Files.Should().HaveCount(2);
        session.TotalSize.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-008")]
    public async Task CreateBackupAsync_StoresFileHash()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "hashtest.txt");
        await File.WriteAllTextAsync(testFile, "Test content for hash");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Delete,
        };

        // Act
        var session = await _service.CreateBackupAsync(operation);

        // Assert
        session.Files.First().Hash.Should().NotBeNullOrEmpty();
        session.Files.First().Hash.Should().HaveLength(64); // SHA-256 = 64 hex chars
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-009")]
    public async Task CreateBackupAsync_StoresFileMetadata()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "metadata.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Move,
        };

        // Act
        var session = await _service.CreateBackupAsync(operation);

        // Assert
        var entry = session.Files.First();
        entry.OriginalPath.Should().Be(testFile);
        entry.Size.Should().BeGreaterThan(0);
        entry.LastModified.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region RestoreBackupAsync Tests (BKP-010 to BKP-015)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-010")]
    public async Task RestoreBackupAsync_WithValidSession_RestoresFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "restore.txt");
        var originalContent = "Original content to restore";
        await File.WriteAllTextAsync(testFile, originalContent);
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Delete,
        };
        var session = await _service.CreateBackupAsync(operation);

        // Simulate file deletion
        File.Delete(testFile);
        File.Exists(testFile).Should().BeFalse();

        // Act
        var result = await _service.RestoreBackupAsync(session.SessionId);

        // Assert
        result.Success.Should().BeTrue();
        result.FilesRestored.Should().Be(1);
        File.Exists(testFile).Should().BeTrue();
        (await File.ReadAllTextAsync(testFile)).Should().Be(originalContent);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-011")]
    public async Task RestoreBackupAsync_WithInvalidSession_ReturnsError()
    {
        // Act
        var result = await _service.RestoreBackupAsync("invalid-session-id");

        // Assert
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Session not found"));
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-012")]
    public async Task RestoreFileAsync_RestoresSingleFile()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "single1.txt");
        var file2 = Path.Combine(_testDirectory, "single2.txt");
        await File.WriteAllTextAsync(file1, "Content 1");
        await File.WriteAllTextAsync(file2, "Content 2");

        var operations = new[]
        {
            new FileOperationRequest { SourcePath = file1, OperationType = FileOperationType.Delete },
            new FileOperationRequest { SourcePath = file2, OperationType = FileOperationType.Delete },
        };
        var session = await _service.CreateBatchBackupAsync(operations);

        // Delete only file1
        File.Delete(file1);

        // Act
        var result = await _service.RestoreFileAsync(session.SessionId, file1);

        // Assert
        result.Success.Should().BeTrue();
        result.FilesRestored.Should().Be(1);
        File.Exists(file1).Should().BeTrue();
    }

    #endregion

    #region CommitBackupAsync Tests (BKP-016 to BKP-018)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-016")]
    public async Task CommitBackupAsync_CleansUpBackupFiles()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "commit.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Move,
        };
        var session = await _service.CreateBackupAsync(operation);
        var backupPath = session.BackupPath;

        // Act
        await _service.CommitBackupAsync(session.SessionId);

        // Assert
        Directory.Exists(backupPath).Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-017")]
    public async Task CommitBackupAsync_RemovesFromActiveSessions()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "commit2.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Delete,
        };
        var session = await _service.CreateBackupAsync(operation);

        // Act
        await _service.CommitBackupAsync(session.SessionId);
        var activeSessions = await _service.GetActiveSessionsAsync();

        // Assert
        activeSessions.Should().NotContain(s => s.SessionId == session.SessionId);
    }

    #endregion

    #region RollbackAsync Tests (BKP-019 to BKP-021)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-019")]
    public async Task RollbackAsync_RestoresAllFiles()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "rollback1.txt");
        var file2 = Path.Combine(_testDirectory, "rollback2.txt");
        await File.WriteAllTextAsync(file1, "Content 1");
        await File.WriteAllTextAsync(file2, "Content 2");

        var operations = new[]
        {
            new FileOperationRequest { SourcePath = file1, OperationType = FileOperationType.Delete },
            new FileOperationRequest { SourcePath = file2, OperationType = FileOperationType.Delete },
        };
        var session = await _service.CreateBatchBackupAsync(operations);

        // Simulate deletion
        File.Delete(file1);
        File.Delete(file2);

        // Act
        var result = await _service.RollbackAsync(session.SessionId);

        // Assert
        result.Success.Should().BeTrue();
        result.FilesRestored.Should().Be(2);
        File.Exists(file1).Should().BeTrue();
        File.Exists(file2).Should().BeTrue();
    }

    #endregion

    #region VerifyBackupAsync Tests (BKP-022 to BKP-024)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-022")]
    public async Task VerifyBackupAsync_WithValidBackup_ReturnsValid()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "verify.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Move,
        };
        var session = await _service.CreateBackupAsync(operation);

        // Act
        var result = await _service.VerifyBackupAsync(session.SessionId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ValidFiles.Should().Be(1);
        result.CorruptedFiles.Should().Be(0);
        result.MissingFiles.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-023")]
    public async Task VerifyBackupAsync_WithInvalidSession_ReturnsInvalid()
    {
        // Act
        var result = await _service.VerifyBackupAsync("invalid-session");

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region GetStorageInfoAsync Tests (BKP-025 to BKP-027)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-025")]
    public async Task GetStorageInfoAsync_ReturnsValidInfo()
    {
        // Act
        var info = await _service.GetStorageInfoAsync();

        // Assert
        info.Should().NotBeNull();
        info.StoragePath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-026")]
    public async Task GetStorageInfoAsync_AfterBackup_IncludesSession()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "storage.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Delete,
        };
        await _service.CreateBackupAsync(operation);

        // Act
        var info = await _service.GetStorageInfoAsync();

        // Assert
        info.ActiveSessions.Should().BeGreaterThan(0);
        info.TotalFiles.Should().BeGreaterThan(0);
    }

    #endregion

    #region CleanupOldBackupsAsync Tests (BKP-028 to BKP-030)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-028")]
    public async Task CleanupOldBackupsAsync_DoesNotRemoveActiveBackups()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "cleanup.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Delete,
        };
        await _service.CreateBackupAsync(operation);

        // Act
        var result = await _service.CleanupOldBackupsAsync(retentionDays: 0);

        // Assert - Active sessions should not be cleaned
        result.SessionsCleaned.Should().Be(0);
    }

    #endregion

    #region GetFileHistoryAsync Tests (BKP-031 to BKP-033)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-031")]
    public async Task GetFileHistoryAsync_ReturnsBackupHistory()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "history.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Modify,
        };
        await _service.CreateBackupAsync(operation);

        // Act
        var history = await _service.GetFileHistoryAsync(testFile);

        // Assert
        history.Should().NotBeEmpty();
        history.First().FilePath.Should().Be(testFile);
        history.First().IsAvailable.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-032")]
    public async Task GetFileHistoryAsync_WithNoHistory_ReturnsEmpty()
    {
        // Act
        var history = await _service.GetFileHistoryAsync("nonexistent-file.txt");

        // Assert
        history.Should().BeEmpty();
    }

    #endregion

    #region Protection Level Tests (BKP-034 to BKP-036)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-034")]
    public void BackupProtectionLevel_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<BackupProtectionLevel>().Should().Contain(BackupProtectionLevel.None);
        Enum.GetValues<BackupProtectionLevel>().Should().Contain(BackupProtectionLevel.Basic);
        Enum.GetValues<BackupProtectionLevel>().Should().Contain(BackupProtectionLevel.Standard);
        Enum.GetValues<BackupProtectionLevel>().Should().Contain(BackupProtectionLevel.Enhanced);
        Enum.GetValues<BackupProtectionLevel>().Should().Contain(BackupProtectionLevel.Maximum);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-035")]
    public void FileOperationType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<FileOperationType>().Should().Contain(FileOperationType.Move);
        Enum.GetValues<FileOperationType>().Should().Contain(FileOperationType.Copy);
        Enum.GetValues<FileOperationType>().Should().Contain(FileOperationType.Delete);
        Enum.GetValues<FileOperationType>().Should().Contain(FileOperationType.Rename);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "BKP-036")]
    public async Task CreateBackupAsync_WithNoneProtectionLevel_ReturnsCommittedSession()
    {
        // Arrange
        _service.ProtectionLevel = BackupProtectionLevel.None;
        var testFile = Path.Combine(_testDirectory, "none.txt");
        await File.WriteAllTextAsync(testFile, "Test content");
        var operation = new FileOperationRequest
        {
            SourcePath = testFile,
            OperationType = FileOperationType.Delete,
        };

        // Act
        var session = await _service.CreateBackupAsync(operation);

        // Assert
        session.Status.Should().Be(BackupSessionStatus.Committed);
    }

    #endregion
}
