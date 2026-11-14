using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Cloud synchronization engine interface
    /// </summary>
    public interface ICloudSyncEngine
    {
        /// <summary>
        /// Configure cloud provider connection
        /// </summary>
        Task<bool> ConfigureProviderAsync(CloudProvider provider, Dictionary<string, string> configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Perform bidirectional sync between local and cloud
        /// </summary>
        Task<BidirectionalSyncResult> SyncBidirectionalAsync(string localPath, string cloudPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload files to cloud
        /// </summary>
        Task<UploadResult> UploadAsync(string localPath, string cloudPath, IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Download files from cloud
        /// </summary>
        Task<DownloadResult> DownloadAsync(string cloudPath, string localPath, IProgress<SyncProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check sync status
        /// </summary>
        Task<SyncStatus> GetSyncStatusAsync(string path, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve sync conflicts
        /// </summary>
        Task<ConflictResolutionResult> ResolveConflictsAsync(List<SyncConflict> conflicts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get available cloud providers
        /// </summary>
        List<CloudProviderInfo> GetAvailableProviders();

        /// <summary>
        /// Test cloud connection
        /// </summary>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Upload result
    /// </summary>
    public class UploadResult
    {
        public bool Success { get; set; }
        public int FilesUploaded { get; set; }
        public long BytesUploaded { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// Download result
    /// </summary>
    public class DownloadResult
    {
        public bool Success { get; set; }
        public int FilesDownloaded { get; set; }
        public long BytesDownloaded { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
    }


    /// <summary>
    /// Conflict resolution result
    /// </summary>
    public class ConflictResolutionResult
    {
        public bool Success { get; set; }
        public int ResolvedConflicts { get; set; }
        public List<SyncConflict> UnresolvedConflicts { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Cloud provider information
    /// </summary>
    public class CloudProviderInfo
    {
        public CloudProvider Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public List<string> RequiredConfiguration { get; set; } = new();
    }
}
