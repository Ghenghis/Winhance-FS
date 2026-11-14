using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for comparing folders and their contents - created by SemFix constraint-based repair
    /// </summary>
    public interface IFolderComparisonService
    {
        /// <summary>
        /// Compare two folders
        /// </summary>
        /// <param name="leftPath">Left folder path</param>
        /// <param name="rightPath">Right folder path</param>
        /// <returns>Comparison result</returns>
        Task<FolderComparisonResult> CompareFoldersAsync(string leftPath, string rightPath);

        /// <summary>
        /// Compare files within folders
        /// </summary>
        /// <param name="leftPath">Left folder path</param>
        /// <param name="rightPath">Right folder path</param>
        /// <param name="options">Comparison options</param>
        /// <returns>File comparison results</returns>
        Task<IEnumerable<FileComparisonResult>> CompareFilesAsync(string leftPath, string rightPath, ComparisonOptions options);

        /// <summary>
        /// Synchronize folders
        /// </summary>
        /// <param name="sourcePath">Source folder</param>
        /// <param name="targetPath">Target folder</param>
        /// <param name="direction">Sync direction</param>
        /// <returns>Synchronization result</returns>
        Task<FolderSyncResult> SynchronizeFoldersAsync(string sourcePath, string targetPath, FolderSyncDirection direction);

        /// <summary>
        /// Find duplicate files across folders
        /// </summary>
        /// <param name="folderPaths">Folders to search</param>
        /// <param name="options">Duplicate detection options</param>
        /// <returns>Groups of duplicate files</returns>
        Task<IEnumerable<FolderDuplicateGroup>> FindDuplicatesAsync(string[] folderPaths, DuplicateOptions options);

        /// <summary>
        /// Get folder statistics
        /// </summary>
        /// <param name="folderPath">Folder path</param>
        /// <returns>Folder statistics</returns>
        Task<FolderStatistics> GetFolderStatisticsAsync(string folderPath);

        /// <summary>
        /// Event fired when comparison progress updates
        /// </summary>
        event EventHandler<ComparisonProgressEventArgs>? ProgressChanged;
    }

    /// <summary>
    /// Folder comparison result
    /// </summary>
    public class FolderComparisonResult
    {
        /// <summary>
        /// Files only in left folder
        /// </summary>
        public List<string> LeftOnlyFiles { get; set; } = new();

        /// <summary>
        /// Files only in right folder
        /// </summary>
        public List<string> RightOnlyFiles { get; set; } = new();

        /// <summary>
        /// Files in both folders (same or different)
        /// </summary>
        public List<FileComparisonResult> CommonFiles { get; set; } = new();

        /// <summary>
        /// Folders only in left
        /// </summary>
        public List<string> LeftOnlyFolders { get; set; } = new();

        /// <summary>
        /// Folders only in right
        /// </summary>
        public List<string> RightOnlyFolders { get; set; } = new();

        /// <summary>
        /// Summary statistics
        /// </summary>
        public ComparisonStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// File comparison result
    /// </summary>
    public class FileComparisonResult
    {
        /// <summary>
        /// Relative file path
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Left file full path
        /// </summary>
        public string LeftPath { get; set; } = string.Empty;

        /// <summary>
        /// Right file full path
        /// </summary>
        public string RightPath { get; set; } = string.Empty;

        /// <summary>
        /// Comparison status
        /// </summary>
        public ComparisonStatus Status { get; set; }

        /// <summary>
        /// Size difference in bytes
        /// </summary>
        public long SizeDifference { get; set; }

        /// <summary>
        /// Date difference
        /// </summary>
        public TimeSpan DateDifference { get; set; }

        /// <summary>
        /// Content hash comparison result
        /// </summary>
        public bool ContentIdentical { get; set; }
    }

    /// <summary>
    /// Comparison options
    /// </summary>
    public class ComparisonOptions
    {
        /// <summary>
        /// Compare file contents (hash-based)
        /// </summary>
        public bool CompareContent { get; set; } = false;

        /// <summary>
        /// Compare file timestamps
        /// </summary>
        public bool CompareTimestamps { get; set; } = true;

        /// <summary>
        /// Compare file sizes
        /// </summary>
        public bool CompareSizes { get; set; } = true;

        /// <summary>
        /// Include hidden files
        /// </summary>
        public bool IncludeHidden { get; set; } = false;

        /// <summary>
        /// Include system files
        /// </summary>
        public bool IncludeSystem { get; set; } = false;

        /// <summary>
        /// File patterns to exclude
        /// </summary>
        public string[] ExcludePatterns { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Comparison status
    /// </summary>
    public enum ComparisonStatus
    {
        Identical,
        Different,
        LeftNewer,
        RightNewer,
        SizeDifferent,
        LeftOnly,
        RightOnly
    }

    /// <summary>
    /// Folder synchronization direction
    /// </summary>
    public enum FolderSyncDirection
    {
        LeftToRight,
        RightToLeft,
        Bidirectional
    }

    /// <summary>
    /// Folder synchronization result
    /// </summary>
    public class FolderSyncResult
    {
        /// <summary>
        /// Files copied
        /// </summary>
        public int FilesCopied { get; set; }

        /// <summary>
        /// Files updated
        /// </summary>
        public int FilesUpdated { get; set; }

        /// <summary>
        /// Files deleted
        /// </summary>
        public int FilesDeleted { get; set; }

        /// <summary>
        /// Errors encountered
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Success status
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Folder duplicate file group
    /// </summary>
    public class FolderDuplicateGroup
    {
        /// <summary>
        /// File paths with same content
        /// </summary>
        public List<string> FilePaths { get; set; } = new();

        /// <summary>
        /// File size
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Content hash
        /// </summary>
        public string Hash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Duplicate detection options
    /// </summary>
    public class DuplicateOptions
    {
        /// <summary>
        /// Compare by content hash
        /// </summary>
        public bool UseContentHash { get; set; } = true;

        /// <summary>
        /// Compare by file size
        /// </summary>
        public bool UseFileSize { get; set; } = true;

        /// <summary>
        /// Compare by filename
        /// </summary>
        public bool UseFilename { get; set; } = false;

        /// <summary>
        /// Minimum file size to consider
        /// </summary>
        public long MinFileSize { get; set; } = 0;
    }

    /// <summary>
    /// Folder statistics
    /// </summary>
    public class FolderStatistics
    {
        /// <summary>
        /// Total files
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Total folders
        /// </summary>
        public int FolderCount { get; set; }

        /// <summary>
        /// Total size in bytes
        /// </summary>
        public long TotalSize { get; set; }

        /// <summary>
        /// File type breakdown
        /// </summary>
        public Dictionary<string, int> FileTypeBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Comparison statistics
    /// </summary>
    public class ComparisonStatistics
    {
        /// <summary>
        /// Total files compared
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Identical files
        /// </summary>
        public int IdenticalFiles { get; set; }

        /// <summary>
        /// Different files
        /// </summary>
        public int DifferentFiles { get; set; }

        /// <summary>
        /// Unique files
        /// </summary>
        public int UniqueFiles { get; set; }
    }

    /// <summary>
    /// Comparison progress event args
    /// </summary>
    public class ComparisonProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Current file being processed
        /// </summary>
        public string CurrentFile { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Files processed
        /// </summary>
        public int FilesProcessed { get; set; }

        /// <summary>
        /// Total files to process
        /// </summary>
        public int TotalFiles { get; set; }
    }
}
