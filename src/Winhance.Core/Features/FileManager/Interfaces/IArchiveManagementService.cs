using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Interface for archive management services
    /// </summary>
    public interface IArchiveManagementService
    {
        /// <summary>
        /// Create an archive from specified files and directories
        /// </summary>
        /// <param name="options">Archive creation options</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Archive creation result</returns>
        Task<ArchiveResult> CreateArchiveAsync(
            ArchiveCreationOptions options,
            IProgress<ArchiveProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extract files from an archive
        /// </summary>
        /// <param name="archivePath">Path to the archive</param>
        /// <param name="extractionPath">Path to extract files to</param>
        /// <param name="options">Extraction options</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Extraction result</returns>
        Task<ExtractionResult> ExtractArchiveAsync(
            string archivePath,
            string extractionPath,
            ExtractionOptions? options = null,
            IProgress<ArchiveProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get information about an archive
        /// </summary>
        /// <param name="archivePath">Path to the archive</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Archive information</returns>
        Task<ArchiveInfo> GetArchiveInfoAsync(
            string archivePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Test the integrity of an archive
        /// </summary>
        /// <param name="archivePath">Path to the archive</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Test result</returns>
        Task<TestResult> TestArchiveIntegrityAsync(
            string archivePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Detect the type of archive from file path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Archive type</returns>
        ArchiveType DetectArchiveType(string filePath);

        /// <summary>
        /// Check if an archive format is supported
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if supported</returns>
        bool IsArchiveSupported(string filePath);

        /// <summary>
        /// Get all supported archive formats
        /// </summary>
        /// <returns>Enumerable of supported formats</returns>
        IEnumerable<ArchiveFormat> GetSupportedFormats();
    }

    /// <summary>
    /// Interface for archive handlers
    /// </summary>
    public interface IArchiveHandler
    {
        /// <summary>
        /// Create an archive
        /// </summary>
        Task<ArchiveHandlerResult> CreateArchiveAsync(
            IEnumerable<string> sourcePaths,
            string outputPath,
            ArchiveCreationOptions options,
            IProgress<ArchiveProgress>? progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Extract an archive
        /// </summary>
        Task<ExtractionHandlerResult> ExtractArchiveAsync(
            string archivePath,
            string extractionPath,
            ExtractionOptions options,
            IProgress<ArchiveProgress>? progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Get archive information
        /// </summary>
        Task<ArchiveInfo> GetArchiveInfoAsync(string archivePath, CancellationToken cancellationToken);

        /// <summary>
        /// Test archive integrity
        /// </summary>
        Task<IntegrityTestResult> TestIntegrityAsync(string archivePath, CancellationToken cancellationToken);
    }
}
