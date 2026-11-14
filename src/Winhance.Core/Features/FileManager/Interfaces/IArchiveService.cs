using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for archive operations - browsing, extracting, and creating archives.
    /// </summary>
    public interface IArchiveService
    {
        /// <summary>
        /// Gets supported archive formats.
        /// </summary>
        IReadOnlyList<ArchiveFormat> SupportedFormats { get; }

        /// <summary>
        /// Checks if a file is a supported archive.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>True if the file is a supported archive.</returns>
        bool IsArchive(string filePath);

        /// <summary>
        /// Gets archive information.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Archive information.</returns>
        Task<ArchiveInfo> GetArchiveInfoAsync(string archivePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists contents of an archive.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <param name="internalPath">Path within the archive (empty for root).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of archive entries.</returns>
        Task<IEnumerable<ArchiveEntry>> ListContentsAsync(string archivePath, string internalPath = "", CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts all contents from an archive.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <param name="destinationPath">Destination folder.</param>
        /// <param name="options">Extraction options.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Extraction result.</returns>
        Task<ExtractionResult> ExtractAllAsync(
            string archivePath,
            string destinationPath,
            ExtractionOptions? options = null,
            IProgress<ExtractionProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts specific entries from an archive.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <param name="entries">Entries to extract.</param>
        /// <param name="destinationPath">Destination folder.</param>
        /// <param name="options">Extraction options.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Extraction result.</returns>
        Task<ExtractionResult> ExtractEntriesAsync(
            string archivePath,
            IEnumerable<string> entries,
            string destinationPath,
            ExtractionOptions? options = null,
            IProgress<ExtractionProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Extracts a single entry to a stream.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <param name="entryPath">Path of the entry within the archive.</param>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ExtractToStreamAsync(string archivePath, string entryPath, Stream outputStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new archive.
        /// </summary>
        /// <param name="archivePath">Path for the new archive.</param>
        /// <param name="sourcePaths">Files/folders to add.</param>
        /// <param name="options">Creation options.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Creation result.</returns>
        Task<ArchiveResult> CreateArchiveAsync(
            string archivePath,
            IEnumerable<string> sourcePaths,
            ArchiveOptions? options = null,
            IProgress<ArchiveProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds files to an existing archive.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <param name="sourcePaths">Files/folders to add.</param>
        /// <param name="options">Options.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result.</returns>
        Task<ArchiveResult> AddToArchiveAsync(
            string archivePath,
            IEnumerable<string> sourcePaths,
            ArchiveOptions? options = null,
            IProgress<ArchiveProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the integrity of an archive.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Test result.</returns>
        Task<ArchiveTestResult> TestArchiveAsync(string archivePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches within an archive.
        /// </summary>
        /// <param name="archivePath">Path to the archive.</param>
        /// <param name="pattern">Search pattern.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Matching entries.</returns>
        Task<IEnumerable<ArchiveEntry>> SearchAsync(string archivePath, string pattern, CancellationToken cancellationToken = default);
    }

}
