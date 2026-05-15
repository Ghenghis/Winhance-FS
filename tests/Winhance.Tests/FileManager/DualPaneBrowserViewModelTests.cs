using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.Tests.FileManager;

public class DualPaneBrowserViewModelTests
{
    [Fact]
    [Trait("Category", "Backend")]
    public void SelectedLeftItem_PopulatesCommandSelection()
    {
        var viewModel = CreateViewModel(new Mock<IFileManagerService>().Object);
        var item = new FileItemViewModel { Name = "report.txt", FullPath = @"C:\Left\report.txt" };

        viewModel.SelectedLeftItem = item;

        viewModel.IsLeftPaneActive.Should().BeTrue();
        viewModel.SelectedLeftItems.Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CopyToOtherPane_UsesSingleSelectedLeftItem()
    {
        var fileManagerService = new Mock<IFileManagerService>();
        fileManagerService
            .Setup(x => x.GetDrivesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FileManagerDriveInfo>());
        fileManagerService
            .Setup(x => x.GetDirectoryContentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FileSystemEntry>());
        fileManagerService
            .Setup(x => x.CopyFilesAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileOperationResult { Success = true });

        var viewModel = CreateViewModel(fileManagerService.Object);
        var item = new FileItemViewModel { Name = "report.txt", FullPath = @"C:\Left\report.txt" };

        viewModel.RightPanePath = @"D:\Right";
        viewModel.SelectedLeftItem = item;

        await viewModel.CopyToOtherPaneCommand.ExecuteAsync(null);

        fileManagerService.Verify(
            x => x.CopyFilesAsync(
                It.Is<IEnumerable<string>>(paths => paths.Single() == @"C:\Left\report.txt"),
                @"D:\Right",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static DualPaneBrowserViewModel CreateViewModel(IFileManagerService fileManagerService)
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var services = new Dictionary<Type, object>
        {
            [typeof(FavoritesPanelViewModel)] = new FavoritesPanelViewModel(),
        };
        serviceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((Type type) => services.TryGetValue(type, out var service) ? service : null);

        return new DualPaneBrowserViewModel(fileManagerService, serviceProvider.Object);
    }
}
