using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;

namespace Winhance.WPF.Features.Common.Extensions.DI
{
    /// <summary>
    /// Dependency injection extensions for FileManager services.
    /// </summary>
    public static class FileManagerServicesExtensions
    {
        /// <summary>
        /// Adds all FileManager services to the service collection.
        /// </summary>
        public static IServiceCollection AddFileManagerServices(this IServiceCollection services)
        {
            // Core FileManager Services (Singleton - Shared across the application)
            services.TryAddSingleton<IFileManagerService, FileManagerService>();
            services.TryAddSingleton<ISpaceAnalyzerService, SpaceAnalyzerService>();
            services.TryAddSingleton<IBatchRenameService, BatchRenameService>();
            services.TryAddSingleton<IOrganizerService, OrganizerService>();
            services.TryAddSingleton<ISearchService, SearchService>();
            services.TryAddSingleton<IDuplicateFinderService, DuplicateFinderService>();
            services.TryAddSingleton<IForensicsService>(sp => new ForensicsService(sp.GetService<ILogService>()));

            // Environment & PATH Doctor (registry-backed store; factory because the service has a test constructor)
            services.TryAddSingleton<IEnvironmentStore>(_ => SnapshotEnvironmentStore.CreateDefault()); // registry, or WINHANCE_ENV_SNAPSHOT file
            services.TryAddSingleton<IEnvironmentDoctorService>(sp => new EnvironmentDoctorService(
                sp.GetRequiredService<IEnvironmentStore>(),
                sp.GetRequiredService<Winhance.Core.Features.Common.Interfaces.ILogService>(),
                null));
            
            // File Operations Services
            services.TryAddSingleton<IFileOperationsService, FileOperationsService>();
            services.TryAddSingleton<IAdvancedFileOperations, AdvancedFileOperationsService>();
            
            // Navigation and UI Services
            services.TryAddSingleton<ITabService, TabService>();
            services.TryAddSingleton<IViewModeService, ViewModeService>();
            services.TryAddSingleton<IColumnService, ColumnService>();
            services.TryAddSingleton<ISelectionService, SelectionService>();
            services.TryAddSingleton<ISortingService, SortingService>();
            services.TryAddSingleton<IAddressBarService, AddressBarService>();
            
            // Preview and Media Services
            services.TryAddSingleton<IPreviewService, PreviewService>();
            services.TryAddSingleton<IQuickLookService, QuickLookService>();
            services.TryAddSingleton<IMetadataService, MetadataService>();
            
            // Archive and Compression Services
            services.TryAddSingleton<IArchiveService, ArchiveService>();
            
            // Sync and Backup Services
            services.TryAddSingleton<ISyncService, SyncService>();
            services.TryAddSingleton<IBackupProtectionService, BackupProtectionService>();
            
            // Monitoring and Watch Services
            services.TryAddSingleton<IWatchFolderService, WatchFolderService>();
            services.TryAddSingleton<IDriveDetectionService, DriveDetectionService>();
            
            // Context Menu and Clipboard Services
            services.TryAddSingleton<IContextMenuService, ContextMenuService>();
            services.TryAddSingleton<IClipboardService, ClipboardService>();
            
            // Filter and Quick Search Services
            services.TryAddSingleton<IQuickFilterService, QuickFilterService>();
            
            // Comparison Services
            services.TryAddSingleton<ICompareService, CompareService>();
            
            // Operation Queue Service
            services.TryAddSingleton<IOperationQueueService, OperationQueueService>();
            
            // Nexus Integration Services
            services.TryAddSingleton<INexusIndexerService>(sp => new NexusIndexerService(sp.GetService<ILogService>()));
            
            // Favorites Service
            services.TryAddSingleton<IFavoritesService, FavoritesService>();

            return services;
        }
    }
}
