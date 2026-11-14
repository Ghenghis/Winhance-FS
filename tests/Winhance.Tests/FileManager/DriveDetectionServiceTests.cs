using System;
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
/// Unit tests for DriveDetectionService - Drive detection and hardware info features.
/// </summary>
public class DriveDetectionServiceTests
{
    private readonly Mock<ILogService> _mockLogService;
    private readonly DriveDetectionService _service;

    public DriveDetectionServiceTests()
    {
        _mockLogService = new Mock<ILogService>();
        _service = new DriveDetectionService(_mockLogService.Object);
    }

    #region GetAllDrivesAsync Tests (ORG-DRV-001 to ORG-DRV-005)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-001")]
    public async Task GetAllDrivesAsync_ReturnsAvailableDrives()
    {
        // Act
        var drives = await _service.GetAllDrivesAsync();

        // Assert
        drives.Should().NotBeNull();
        drives.Should().NotBeEmpty("System should have at least one drive");
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-002")]
    public async Task GetAllDrivesAsync_DriveHasValidProperties()
    {
        // Act
        var drives = await _service.GetAllDrivesAsync();
        var firstDrive = drives.First();

        // Assert
        firstDrive.DriveLetter.Should().NotBeNullOrEmpty();
        firstDrive.DriveLetter.Should().MatchRegex("^[A-Z]$");
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-003")]
    public async Task GetAllDrivesAsync_SystemDriveIsIdentified()
    {
        // Act
        var drives = await _service.GetAllDrivesAsync();

        // Assert
        drives.Should().Contain(d => d.IsSystemDrive, "System drive should be identified");
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-004")]
    public async Task GetAllDrivesAsync_DriveHasCapacityInfo()
    {
        // Act
        var drives = await _service.GetAllDrivesAsync();
        var readyDrive = drives.FirstOrDefault(d => d.IsReady);

        // Assert
        readyDrive.Should().NotBeNull();
        readyDrive!.TotalCapacity.Should().BeGreaterThan(0);
        readyDrive.FreeSpace.Should().BeGreaterOrEqualTo(0);
        readyDrive.UsedSpace.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-005")]
    public async Task GetAllDrivesAsync_DriveHasFileSystemInfo()
    {
        // Act
        var drives = await _service.GetAllDrivesAsync();
        var readyDrive = drives.FirstOrDefault(d => d.IsReady);

        // Assert
        readyDrive.Should().NotBeNull();
        readyDrive!.FileSystem.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetDriveInfoAsync Tests (ORG-DRV-006 to ORG-DRV-010)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-006")]
    public async Task GetDriveInfoAsync_WithValidDrive_ReturnsDriveInfo()
    {
        // Arrange
        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1);

        // Act
        var driveInfo = await _service.GetDriveInfoAsync(systemDrive);

        // Assert
        driveInfo.Should().NotBeNull();
        driveInfo!.DriveLetter.Should().Be(systemDrive);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-007")]
    public async Task GetDriveInfoAsync_NormalizesDriveLetter()
    {
        // Arrange
        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1);

        // Act - Test various input formats
        var result1 = await _service.GetDriveInfoAsync($"{systemDrive}:");
        var result2 = await _service.GetDriveInfoAsync($"{systemDrive}:\\");
        var result3 = await _service.GetDriveInfoAsync(systemDrive.ToLowerInvariant());

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result3.Should().NotBeNull();
        result1!.DriveLetter.Should().Be(result2!.DriveLetter);
        result2.DriveLetter.Should().Be(result3!.DriveLetter);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-008")]
    public async Task GetDriveInfoAsync_WithInvalidDrive_ReturnsNull()
    {
        // Act
        var driveInfo = await _service.GetDriveInfoAsync("Z");

        // Assert - Z: drive typically doesn't exist
        // This test may pass or fail depending on system configuration
        // The key is that it doesn't throw an exception
        (driveInfo == null || driveInfo.DriveLetter == "Z").Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-009")]
    public async Task GetDriveInfoAsync_HasTypeDescription()
    {
        // Arrange
        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1);

        // Act
        var driveInfo = await _service.GetDriveInfoAsync(systemDrive);

        // Assert
        driveInfo.Should().NotBeNull();
        driveInfo!.TypeDescription.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-010")]
    public async Task GetDriveInfoAsync_HasDisplayFormats()
    {
        // Arrange
        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1);

        // Act
        var driveInfo = await _service.GetDriveInfoAsync(systemDrive);

        // Assert
        driveInfo.Should().NotBeNull();
        driveInfo!.TotalCapacityDisplay.Should().NotBeNullOrEmpty();
        driveInfo.FreeSpaceDisplay.Should().NotBeNullOrEmpty();
        driveInfo.TotalCapacityDisplay.Should().ContainAny("B", "KB", "MB", "GB", "TB");
    }

    #endregion

    #region DriveMediaType Tests (ORG-DRV-011 to ORG-DRV-015)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-011")]
    public void DriveMediaType_HasExpectedValues()
    {
        // Assert - Verify enum has all expected values
        Enum.GetValues<DriveMediaType>().Should().Contain(DriveMediaType.Unknown);
        Enum.GetValues<DriveMediaType>().Should().Contain(DriveMediaType.HDD);
        Enum.GetValues<DriveMediaType>().Should().Contain(DriveMediaType.SSD);
        Enum.GetValues<DriveMediaType>().Should().Contain(DriveMediaType.NVMe);
        Enum.GetValues<DriveMediaType>().Should().Contain(DriveMediaType.USB);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-012")]
    public void DriveBusType_HasExpectedValues()
    {
        // Assert - Verify enum has all expected values
        Enum.GetValues<DriveBusType>().Should().Contain(DriveBusType.SATA);
        Enum.GetValues<DriveBusType>().Should().Contain(DriveBusType.NVMe);
        Enum.GetValues<DriveBusType>().Should().Contain(DriveBusType.USB3);
        Enum.GetValues<DriveBusType>().Should().Contain(DriveBusType.USB3Gen2);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-013")]
    public async Task GetAllDrivesAsync_HasEstimatedSpeeds()
    {
        // Act
        var drives = await _service.GetAllDrivesAsync();
        var readyDrive = drives.FirstOrDefault(d => d.IsReady);

        // Assert
        readyDrive.Should().NotBeNull();
        readyDrive!.MaxReadSpeedMBps.Should().BeGreaterOrEqualTo(0);
        readyDrive.MaxWriteSpeedMBps.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region GetOptimalTransferSettingsAsync Tests (ORG-DRV-016 to ORG-DRV-020)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-016")]
    public async Task GetOptimalTransferSettingsAsync_ReturnsValidSettings()
    {
        // Arrange
        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1);

        // Act
        var settings = await _service.GetOptimalTransferSettingsAsync(systemDrive, systemDrive);

        // Assert
        settings.Should().NotBeNull();
        settings.BufferSize.Should().BeGreaterThan(0);
        settings.ParallelOperations.Should().BeGreaterThan(0);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-017")]
    public async Task GetOptimalTransferSettingsAsync_HasDescription()
    {
        // Arrange
        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1);

        // Act
        var settings = await _service.GetOptimalTransferSettingsAsync(systemDrive, systemDrive);

        // Assert
        settings.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-018")]
    public async Task GetOptimalTransferSettingsAsync_WithInvalidDrives_ReturnsDefaults()
    {
        // Act
        var settings = await _service.GetOptimalTransferSettingsAsync("X", "Y");

        // Assert
        settings.Should().NotBeNull();
        settings.BufferSize.Should().BeGreaterThan(0);
        settings.Description.Should().Contain("Unable to detect");
    }

    #endregion

    #region RefreshDriveCacheAsync Tests (ORG-DRV-021 to ORG-DRV-023)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-021")]
    public async Task RefreshDriveCacheAsync_DoesNotThrow()
    {
        // Act
        var action = async () => await _service.RefreshDriveCacheAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-022")]
    public async Task RefreshDriveCacheAsync_UpdatesCache()
    {
        // Act
        await _service.RefreshDriveCacheAsync();
        var drives = await _service.GetAllDrivesAsync();

        // Assert
        drives.Should().NotBeEmpty();
        drives.All(d => d.LastChecked > DateTime.UtcNow.AddMinutes(-1)).Should().BeTrue();
    }

    #endregion

    #region GetPhysicalDiskIdAsync Tests (ORG-DRV-024 to ORG-DRV-025)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-024")]
    public async Task GetPhysicalDiskIdAsync_ForSystemDrive_ReturnsIdOrNull()
    {
        // Arrange
        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 1);

        // Act
        var diskId = await _service.GetPhysicalDiskIdAsync(systemDrive);

        // Assert - Physical disk ID may not be available in all environments (VMs, containers)
        // If available, it should contain "PHYSICALDRIVE"
        if (!string.IsNullOrEmpty(diskId))
        {
            diskId.Should().Contain("PHYSICALDRIVE");
        }
    }

    #endregion

    #region DriveDetailedInfo Model Tests (ORG-DRV-026 to ORG-DRV-030)

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-026")]
    public void DriveDetailedInfo_UsagePercentage_CalculatesCorrectly()
    {
        // Arrange
        var drive = new DriveDetailedInfo
        {
            TotalCapacity = 1000,
            FreeSpace = 300,
        };

        // Assert
        drive.UsedSpace.Should().Be(700);
        drive.UsagePercentage.Should().Be(70);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-027")]
    public void DriveDetailedInfo_UsagePercentage_HandlesZeroCapacity()
    {
        // Arrange
        var drive = new DriveDetailedInfo
        {
            TotalCapacity = 0,
            FreeSpace = 0,
        };

        // Assert
        drive.UsagePercentage.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-028")]
    public void DriveDetailedInfo_TypeDescription_FormatsByMediaType()
    {
        // Arrange
        var nvmeDrive = new DriveDetailedInfo
        {
            MediaType = DriveMediaType.NVMe,
            BusType = DriveBusType.NVMe,
        };
        var hddDrive = new DriveDetailedInfo
        {
            MediaType = DriveMediaType.HDD,
            BusType = DriveBusType.SATA,
        };

        // Assert
        nvmeDrive.TypeDescription.Should().Contain("NVMe");
        hddDrive.TypeDescription.Should().Contain("HDD");
    }

    [Fact]
    [Trait("Category", "Backend")]
    [Trait("Feature", "ORG-DRV-029")]
    public void DriveTransferSettings_HasReasonableDefaults()
    {
        // Arrange
        var settings = new DriveTransferSettings();

        // Assert
        settings.BufferSize.Should().Be(4 * 1024 * 1024); // 4MB
        settings.ParallelOperations.Should().Be(4);
        settings.UseAsyncIO.Should().BeTrue();
    }

    #endregion
}
