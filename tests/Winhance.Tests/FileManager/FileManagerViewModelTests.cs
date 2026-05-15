using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.Tests.FileManager;

public class FileManagerViewModelTests
{
    [Fact]
    [Trait("Category", "Backend")]
    public void SelectSpaceRecoveryTab_ShowsSpaceRecoveryViewModel()
    {
        var viewModel = new FileManagerViewModel();

        viewModel.SelectSpaceRecoveryTabCommand.Execute(null);

        viewModel.IsSpaceRecoveryTabSelected.Should().BeTrue();
        viewModel.IsBrowserTabSelected.Should().BeFalse();
        viewModel.CurrentView.Should().BeSameAs(viewModel.SpaceRecoveryViewModel);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task RefreshAsync_WhenSpaceRecoveryTabSelected_RunsSpaceRecoveryAnalysis()
    {
        var organizerService = new Mock<IOrganizerService>();
        organizerService
            .Setup(x => x.AnalyzeSpaceRecoveryAsync("C:", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpaceRecoveryAnalysis
            {
                DriveLetter = "C:",
                RecoverableSpace = 1024,
                Opportunities = new[]
                {
                    new RecoveryOpportunity
                    {
                        Category = "Temp",
                        Path = @"C:\Temp",
                        Size = 1024,
                        ItemCount = 1,
                        RecommendedAction = RecoveryAction.Clean,
                        Description = "Temporary files",
                        IsSafeToClean = true,
                    },
                },
            });

        var viewModel = CreateViewModel(organizerService.Object);
        viewModel.SelectSpaceRecoveryTabCommand.Execute(null);

        await viewModel.RefreshCommand.ExecuteAsync(null);

        organizerService.Verify(
            x => x.AnalyzeSpaceRecoveryAsync("C:", It.IsAny<CancellationToken>()),
            Times.Once);
        viewModel.SpaceRecoveryViewModel!.RecoverableSpace.Should().Be(1024);
        viewModel.SpaceRecoveryViewModel.RecoveryOpportunities.Should().ContainSingle();
    }

    private static FileManagerViewModel CreateViewModel(IOrganizerService organizerService)
    {
        var fileManagerService = new Mock<IFileManagerService>();
        fileManagerService
            .Setup(x => x.GetDrivesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FileManagerDriveInfo>());
        fileManagerService
            .Setup(x => x.GetDirectoryContentsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FileSystemEntry>());

        var batchRenameService = new Mock<IBatchRenameService>();
        batchRenameService
            .Setup(x => x.GetPresetsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RenamePreset>());

        var nexusIndexer = new Mock<INexusIndexerService>();
        nexusIndexer.SetupGet(x => x.IsAvailable).Returns(false);

        var serviceProvider = new Mock<IServiceProvider>();
        var services = new Dictionary<Type, object>
        {
            [typeof(FavoritesPanelViewModel)] = new FavoritesPanelViewModel(),
            [typeof(TabContainerViewModel)] = new TabContainerViewModel(),
            [typeof(SearchResultsViewModel)] = new SearchResultsViewModel(),
        };
        serviceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((Type type) => services.TryGetValue(type, out var service) ? service : null);

        return new FileManagerViewModel(
            fileManagerService.Object,
            batchRenameService.Object,
            organizerService,
            nexusIndexer.Object,
            serviceProvider.Object);
    }
}
