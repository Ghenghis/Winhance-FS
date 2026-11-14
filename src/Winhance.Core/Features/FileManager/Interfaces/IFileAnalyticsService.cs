using System;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Interface for file analytics and reporting services
    /// </summary>
    public interface IFileAnalyticsService
    {
        /// <summary>
        /// Generate a comprehensive usage report for a path
        /// </summary>
        /// <param name="path">Path to analyze</param>
        /// <param name="options">Report generation options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Usage report</returns>
        Task<UsageReport> GenerateUsageReportAsync(
            string path, 
            ReportOptions options, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get performance metrics for a time range
        /// </summary>
        /// <param name="range">Time range to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Performance metrics</returns>
        Task<PerformanceMetrics> GetPerformanceMetricsAsync(
            TimeRange range, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze trends for a specific path over time
        /// </summary>
        /// <param name="path">Path to analyze</param>
        /// <param name="range">Time range for trend analysis</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Trend analysis</returns>
        Task<TrendAnalysis> AnalyzeTrendsAsync(
            string path, 
            TimeRange range, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze file types in a directory
        /// </summary>
        /// <param name="path">Directory path to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of file type analyses</returns>
        Task<FileTypeAnalysis[]> GetFileTypeAnalysisAsync(
            string path, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze storage usage across multiple paths
        /// </summary>
        /// <param name="paths">Paths to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Storage analysis</returns>
        Task<StorageAnalysis> AnalyzeStorageAsync(
            string[] paths, 
            CancellationToken cancellationToken = default);
    }
}
