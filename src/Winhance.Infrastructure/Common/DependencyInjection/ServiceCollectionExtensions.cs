using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.Common.Services;
using Winhance.Infrastructure.Features.FileManager.Services;

namespace Winhance.Infrastructure.Common.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring Winhance services in dependency injection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add all core Winhance services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddWinhanceServices(this IServiceCollection services)
        {
            return services
                .AddCommonServices()
                .AddFileManagerServices()
                .AddAdvancedServices()
                .AddSecurityServices()
                .AddAnalyticsServices()
                .AddWorkflowServices();
        }

        /// <summary>
        /// Add common infrastructure services
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCommonServices(this IServiceCollection services)
        {
            // Core common services
            services.TryAddSingleton<ILogService, LogService>();
            services.TryAddSingleton<IConfigurationService, ConfigurationService>();
            services.TryAddSingleton<IEventBusService, EventBusService>();
            // services.TryAddSingleton<INotificationService, NotificationService>();

            return services;
        }

        /// <summary>
        /// Add core file manager services
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddFileManagerServices(this IServiceCollection services)
        {
            // Core file manager services
            services.TryAddScoped<IFileManagerService, FileManagerService>();
            services.TryAddScoped<ISearchService, SearchService>();
            services.TryAddScoped<IFileOperationsService, FileOperationsService>();
            services.TryAddScoped<IMetadataService, MetadataService>();
            services.TryAddScoped<INexusIndexerService, NexusIndexerService>();

            // UI support services
            services.TryAddScoped<ITabService, TabService>();
            services.TryAddScoped<IFavoritesService, FavoritesService>();
            services.TryAddScoped<IOperationQueueService, OperationQueueService>();
            services.TryAddScoped<IViewModeService, ViewModeService>();
            services.TryAddScoped<ISortingService, SortingService>();
            services.TryAddScoped<ISelectionService, SelectionService>();
            services.TryAddScoped<IClipboardService, ClipboardService>();

            // Navigation and display services
            services.TryAddScoped<IAddressBarService, AddressBarService>();
            services.TryAddScoped<IContextMenuService, ContextMenuService>();
            services.TryAddScoped<IColumnService, ColumnService>();
            services.TryAddScoped<IQuickFilterService, QuickFilterService>();
            services.TryAddScoped<IPreviewService, PreviewService>();
            services.TryAddScoped<IQuickLookService, QuickLookService>();

            // System integration services  
            services.TryAddScoped<IDriveDetectionService, DriveDetectionService>();
            services.TryAddScoped<IWatchFolderService, WatchFolderService>();

            return services;
        }

        /// <summary>
        /// Add advanced file operation services
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAdvancedServices(this IServiceCollection services)
        {
            // Advanced operations
            services.TryAddScoped<IAdvancedFileOperations, AdvancedFileOperationsService>();
            services.TryAddScoped<IBatchRenameService, BatchRenameService>();
            services.TryAddScoped<IDuplicateFinderService, DuplicateFinderService>();
            services.TryAddScoped<ISpaceAnalyzerService, SpaceAnalyzerService>();
            services.TryAddScoped<ICompareService, CompareService>();
            services.TryAddScoped<IOrganizerService, OrganizerService>();

            // Archive and compression
            services.TryAddScoped<IArchiveService, ArchiveService>();
            services.TryAddScoped<IArchiveManagementService, ArchiveManagementService>();

            // Synchronization and backup
            services.TryAddScoped<ISyncService, SyncService>();
            services.TryAddScoped<IBackupProtectionService, BackupProtectionService>();

            // AI and automation
            services.TryAddScoped<IAIOrganizationService, AIOrganizationService>();
            services.TryAddScoped<ITreeMapVisualizationService, TreeMapVisualizationService>();

            return services;
        }

        /// <summary>
        /// Add security and encryption services
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSecurityServices(this IServiceCollection services)
        {
            services.TryAddScoped<IFileSecurityService, FileSecurityService>();
            services.TryAddScoped<ITransactionManager, TransactionManager>();

            return services;
        }

        /// <summary>
        /// Add analytics and reporting services
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAnalyticsServices(this IServiceCollection services)
        {
            services.TryAddScoped<IFileAnalyticsService, FileAnalyticsService>();

            return services;
        }

        /// <summary>
        /// Add workflow automation services
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddWorkflowServices(this IServiceCollection services)
        {
            services.TryAddScoped<IWorkflowAutomationService, WorkflowAutomationService>();

            return services;
        }

        // ViewModels are registered in Winhance.WPF via App.xaml.cs / DI extensions
        // to avoid a circular project reference (Infrastructure cannot reference WPF).

        /// <summary>
        /// Add Winhance configuration with custom options
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddWinhanceWithOptions(
            this IServiceCollection services, 
            Action<WinhanceOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services.AddWinhanceServices();
        }
    }

    /// <summary>
    /// Configuration options for Winhance services
    /// </summary>
    public class WinhanceOptions
    {
        /// <summary>
        /// Enable advanced logging
        /// </summary>
        public bool EnableAdvancedLogging { get; set; } = true;

        /// <summary>
        /// Enable file system watching
        /// </summary>
        public bool EnableFileSystemWatching { get; set; } = true;

        /// <summary>
        /// Enable AI features
        /// </summary>
        public bool EnableAIFeatures { get; set; } = true;

        /// <summary>
        /// Enable cloud synchronization
        /// </summary>
        public bool EnableCloudSync { get; set; } = false;

        /// <summary>
        /// Enable transaction management
        /// </summary>
        public bool EnableTransactions { get; set; } = true;

        /// <summary>
        /// Enable security features
        /// </summary>
        public bool EnableSecurity { get; set; } = true;

        /// <summary>
        /// Enable analytics and reporting
        /// </summary>
        public bool EnableAnalytics { get; set; } = true;

        /// <summary>
        /// Enable workflow automation
        /// </summary>
        public bool EnableWorkflows { get; set; } = true;

        /// <summary>
        /// Default file operation timeout in seconds
        /// </summary>
        public int DefaultTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum concurrent file operations
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = 4;

        /// <summary>
        /// Cache size for file metadata (MB)
        /// </summary>
        public int MetadataCacheSizeMB { get; set; } = 100;

        /// <summary>
        /// Enable performance monitoring
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;
    }
}
