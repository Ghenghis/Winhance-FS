using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Winhance.WPF.Features.Common.Extensions.DI;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Verifies the application's real composition root can construct the File Manager.
/// Compile-time checks cannot catch a missing constructor dependency; it would only surface as a
/// crash when the File Manager tab is opened.
/// </summary>
public class FileManagerDependencyInjectionTests
{
    /// <summary>Builds the exact host the app uses (CompositionRoot.CreateWinhanceHost).</summary>
    private static IHost BuildAppHost() => CompositionRoot.CreateWinhanceHost().Build();

    [Fact]
    [Trait("Category", "Backend")]
    public void CompositionRoot_RegistersEveryFileManagerBackendService()
    {
        var services = new ServiceCollection();
        services.ConfigureWinhanceServices();

        var fileManagerServices = services
            .Where(d => d.ServiceType.Namespace == "Winhance.Core.Features.FileManager.Interfaces")
            .Select(d => d.ServiceType)
            .Distinct()
            .ToList();

        fileManagerServices.Should().HaveCountGreaterOrEqualTo(28,
            "all File Manager backend services must be registered with the app container");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void CompositionRoot_CanResolveEveryFileManagerBackendService()
    {
        var services = new ServiceCollection();
        services.ConfigureWinhanceServices();
        using var host = BuildAppHost();
        var provider = host.Services;

        var serviceTypes = services
            .Where(d => d.ServiceType.Namespace == "Winhance.Core.Features.FileManager.Interfaces")
            .Select(d => d.ServiceType)
            .Distinct()
            .ToList();

        foreach (var serviceType in serviceTypes)
        {
            var resolve = () => provider.GetRequiredService(serviceType);
            resolve.Should().NotThrow($"{serviceType.Name} must be constructible from the app container");
        }
    }

    [Theory]
    [Trait("Category", "Backend")]
    [InlineData(typeof(FileManagerViewModel))]
    [InlineData(typeof(DualPaneBrowserViewModel))]
    [InlineData(typeof(BatchRenameViewModel))]
    [InlineData(typeof(OrganizerViewModel))]
    [InlineData(typeof(SpaceRecoveryViewModel))]
    [InlineData(typeof(TabContainerViewModel))]
    [InlineData(typeof(SearchResultsViewModel))]
    [InlineData(typeof(FavoritesPanelViewModel))]
    [InlineData(typeof(AdvancedSelectionViewModel))]
    [InlineData(typeof(ContextMenuViewModel))]
    public void CompositionRoot_CanResolveRegisteredFileManagerViewModel(Type viewModelType)
    {
        using var host = BuildAppHost();

        var resolve = () => host.Services.GetRequiredService(viewModelType);

        resolve.Should().NotThrow($"{viewModelType.Name} is DI-registered and must be constructible");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CompositionRoot_EnvironmentTab_GetsAWorkingDoctor()
    {
        using var host = BuildAppHost();

        var fileManager = host.Services.GetRequiredService<FileManagerViewModel>();
        host.Services.GetRequiredService<IEnvironmentDoctorService>().Should().BeOfType<EnvironmentDoctorService>();

        fileManager.EnvironmentDoctorViewModel.Should().NotBeNull();
        fileManager.SelectEnvironmentTabCommand.Execute(null);
        fileManager.CurrentView.Should().BeSameAs(fileManager.EnvironmentDoctorViewModel);
        await fileManager.EnvironmentDoctorViewModel!.ScanCommand.ExecuteAsync(null);
        fileManager.EnvironmentDoctorViewModel.HasScanned.Should().BeTrue("the doctor must be injected, not null");
        fileManager.EnvironmentDoctorViewModel.Variables.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CompositionRoot_DuplicatesTab_FindsAndMarksDuplicates()
    {
        using var host = BuildAppHost();
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "winhance-dupvm-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(root, "b"));
        try
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(root, "a.txt"), new string('z', 200_000));
            System.IO.File.WriteAllText(System.IO.Path.Combine(root, "b", "a copy.txt"), new string('z', 200_000));
            var fileManager = host.Services.GetRequiredService<FileManagerViewModel>();
            fileManager.SelectDuplicatesTabCommand.Execute(null);
            var vm = fileManager.DuplicateFinderViewModel!;
            fileManager.CurrentView.Should().BeSameAs(vm);
            fileManager.IsEnvironmentTabSelected.Should().BeFalse();

            vm.NewFolder = root;
            await vm.ScanCommand.ExecuteAsync(null);

            vm.Files.Should().HaveCount(2, vm.StatusMessage);
            vm.GroupCount.Should().Be(1);
            vm.SelectedRule = DuplicateFinderViewModel.KeepRules.Single(r => r.Strategy == DuplicateKeepStrategy.KeepShortestPath);
            vm.ApplyRuleCommand.Execute(null);
            vm.Files.Single(f => f.IsMarked).Name.Should().Be("a copy.txt");
            vm.MarkedSummary.Should().StartWith("1 marked");
        }
        finally
        {
            System.IO.Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task CompositionRoot_ForensicsTab_FindsStreamsAndSuspiciousFiles()
    {
        using var host = BuildAppHost();
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "winhance-forvm-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(root);
        try
        {
            var file = System.IO.Path.Combine(root, "setup-notes.txt");
            System.IO.File.WriteAllText(file, "hello");
            System.IO.File.WriteAllText(file + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\n");
            var random = new byte[50_000];
            new Random(1).NextBytes(random);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(root, "thesis.docx"), random);

            var fileManager = host.Services.GetRequiredService<FileManagerViewModel>();
            host.Services.GetRequiredService<IForensicsService>().Should().BeOfType<ForensicsService>();
            fileManager.SelectForensicsTabCommand.Execute(null);
            var vm = fileManager.ForensicsViewModel!;
            fileManager.CurrentView.Should().BeSameAs(vm);

            vm.TargetPath = root;
            await vm.ScanStreamsCommand.ExecuteAsync(null);
            vm.Streams.Should().ContainSingle(vm.StatusMessage).Which.KindText.Should().Be("Download marker");

            await vm.ScanContentCommand.ExecuteAsync(null);
            vm.IsContentSection.Should().BeTrue();
            vm.Inspections.Should().ContainSingle(vm.StatusMessage).Which.VerdictText.Should().Be("Likely encrypted");
        }
        finally
        {
            System.IO.Directory.Delete(root, recursive: true);
        }
    }
}
