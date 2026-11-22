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
}
