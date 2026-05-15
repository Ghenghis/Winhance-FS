using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.WPF.Features.FileManager.ViewModels;
using Xunit;
using CoreViewMode = Winhance.Core.Features.FileManager.Interfaces.ViewMode;

namespace Winhance.Tests.FileManager;

public class FileListViewModelTests
{
    [Fact]
    [Trait("Category", "Backend")]
    public async Task NavigateBackAndForward_TracksLoadedPaths()
    {
        var viewModel = CreateViewModel();

        await viewModel.LoadPathAsync(@"C:\One");
        await viewModel.LoadPathAsync(@"C:\Two");

        viewModel.CanNavigateBack.Should().BeTrue();

        await viewModel.NavigateBackCommand.ExecuteAsync(null);

        viewModel.CurrentPath.Should().Be(@"C:\One");
        viewModel.CanNavigateBack.Should().BeFalse();
        viewModel.CanNavigateForward.Should().BeTrue();

        await viewModel.NavigateForwardCommand.ExecuteAsync(null);

        viewModel.CurrentPath.Should().Be(@"C:\Two");
        viewModel.CanNavigateBack.Should().BeTrue();
        viewModel.CanNavigateForward.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void SortByColumn_KeepsParentFirstAndGroupsFoldersBeforeFiles()
    {
        var viewModel = CreateViewModel();
        viewModel.SortColumn = "Size";
        viewModel.Items.Add(new FileItemViewModel { Name = "file10.txt", FullPath = @"C:\file10.txt", Size = 10 });
        viewModel.Items.Add(new FileItemViewModel { Name = "FolderB", FullPath = @"C:\FolderB", IsDirectory = true });
        viewModel.Items.Add(new FileItemViewModel { Name = "..", FullPath = @"C:\", IsDirectory = true, IsParentDirectory = true });
        viewModel.Items.Add(new FileItemViewModel { Name = "file2.txt", FullPath = @"C:\file2.txt", Size = 2 });
        viewModel.Items.Add(new FileItemViewModel { Name = "FolderA", FullPath = @"C:\FolderA", IsDirectory = true });

        viewModel.SortByColumn("Name");

        viewModel.Items.Select(i => i.Name).Should().ContainInOrder("..", "FolderA", "FolderB", "file2.txt", "file10.txt");
        viewModel.FilteredItems.Select(i => i.Name).Should().ContainInOrder("..", "FolderA", "FolderB", "file2.txt", "file10.txt");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ToggleFoldersFirst_MixesFilesAndFoldersWhenDisabled()
    {
        var viewModel = CreateViewModel();
        viewModel.SortColumn = "Size";
        viewModel.Items.Add(new FileItemViewModel { Name = "z-file.txt", FullPath = @"C:\z-file.txt" });
        viewModel.Items.Add(new FileItemViewModel { Name = "a-folder", FullPath = @"C:\a-folder", IsDirectory = true });

        viewModel.ToggleFoldersFirstCommand.Execute(null);
        viewModel.SortByColumn("Name");

        viewModel.FoldersFirst.Should().BeFalse();
        viewModel.Items.Select(i => i.Name).Should().ContainInOrder("a-folder", "z-file.txt");
        viewModel.StatusMessage.Should().Contain("mixed");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ShowSortOptions_TogglesSortOptionsState()
    {
        var viewModel = CreateViewModel();

        viewModel.ShowSortOptionsCommand.Execute(null);

        viewModel.IsSortOptionsOpen.Should().BeTrue();
        viewModel.StatusMessage.Should().Contain("Sort:");

        viewModel.ShowSortOptionsCommand.Execute(null);

        viewModel.IsSortOptionsOpen.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void SetViewMode_MapsIconsButtonToCoreMediumIconsMode()
    {
        var viewModeService = new Mock<IViewModeService>();
        viewModeService.SetupProperty(x => x.CurrentViewMode, CoreViewMode.Details);
        var viewModel = CreateViewModel(viewModeService: viewModeService.Object);

        viewModel.SetViewModeCommand.Execute("Icons");

        viewModel.ViewMode.Should().Be("Icons");
        viewModeService.Object.CurrentViewMode.Should().Be(CoreViewMode.MediumIcons);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void FileItemViewModel_Type_ReturnsFolderOrDescriptionForBindings()
    {
        var folder = new FileItemViewModel { IsDirectory = true };
        var document = new FileItemViewModel { Extension = ".pdf" };

        folder.Type.Should().Be("Folder");
        document.Type.Should().Be("PDF Document");
    }

    private static FileListViewModel CreateViewModel(IViewModeService? viewModeService = null)
    {
        return new FileListViewModel(
            fileManagerService: null,
            selectionService: null,
            sortingService: null,
            viewModeService: viewModeService,
            serviceProvider: null);
    }
}
