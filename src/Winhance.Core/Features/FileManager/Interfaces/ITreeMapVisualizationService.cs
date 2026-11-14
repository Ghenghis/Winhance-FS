using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Tree map visualization service interface
    /// </summary>
    public interface ITreeMapVisualizationService
    {
        /// <summary>
        /// Generate tree map visualization data for a directory
        /// </summary>
        Task<TreeMapData> GenerateTreeMapAsync(string directoryPath, TreeMapOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze storage usage and generate optimization suggestions
        /// </summary>
        Task<List<StorageOptimization>> AnalyzeStorageAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create tree map visualization with layout
        /// </summary>
        Task<TreeMapVisualization> CreateVisualizationAsync(TreeMapData data, VisualizationOptions options, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get directory usage trends over time
        /// </summary>
        Task<List<UsageTrend>> GetUsageTrendsAsync(string directoryPath, TimeRange timeRange, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generate comprehensive directory analysis
        /// </summary>
        Task<DirectoryAnalysisResult> AnalyzeDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Create directory snapshot for change tracking
        /// </summary>
        Task<DirectorySnapshot> CreateSnapshotAsync(string directoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compare snapshots to detect changes
        /// </summary>
        Task<List<DirectoryChange>> CompareSnapshotsAsync(DirectorySnapshot before, DirectorySnapshot after, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get file analysis results
        /// </summary>
        Task<List<FileAnalysisItem>> AnalyzeFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);
    }
}
