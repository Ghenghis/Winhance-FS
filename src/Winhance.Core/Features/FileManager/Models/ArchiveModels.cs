using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Archive creation options
    /// </summary>
    public class ArchiveCreationOptions
    {
        public IEnumerable<string> SourcePaths { get; set; } = Array.Empty<string>();
        public string OutputPath { get; set; } = string.Empty;
        public ArchiveType Type { get; set; } = ArchiveType.Zip;
        public ArchiveType ArchiveType { get; set; } = ArchiveType.Zip;
        public Models.CompressionLevel CompressionLevel { get; set; } = Models.CompressionLevel.Optimal;
        public bool IncludeSubdirectories { get; set; } = true;
        public bool OverwriteExisting { get; set; } = false;
        public string? Password { get; set; }
    }

    /// <summary>
    /// Archive extraction options
    /// </summary>
    public class ExtractionOptions
    {
        public bool Overwrite { get; set; } = false;
        public bool OverwriteExisting { get; set; } = false;
        public bool CreateDirectoryStructure { get; set; } = true;
        public bool PreserveDirectoryStructure { get; set; } = true;
        public bool PreserveTimestamps { get; set; } = true;
        public string? Password { get; set; }
        public string[]? ExtractOnlyFiles { get; set; }
        public string[]? ExcludePatterns { get; set; }
    }

    /// <summary>
    /// Archive operation result
    /// </summary>
    public class ArchiveResult
    {
        public bool Success { get; set; }
        public string? OutputPath { get; set; }
        public string ArchivePath { get; set; } = string.Empty;
        public int ProcessedItems { get; set; }
        public int FilesAdded { get; set; }
        public long CompressedSize { get; set; }
        public long OriginalSize { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = new();
        public string[] ErrorMessages { get; set; } = Array.Empty<string>();
        public double CompressionRatio { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime StartTime { get; set; }
        public ArchiveType ArchiveType { get; set; }
        public int FilesProcessed { get; set; }
        public long TotalSize { get; set; }
        public int FoldersAdded { get; set; }
    }

    /// <summary>
    /// Archive extraction result
    /// </summary>
    public class ExtractionResult
    {
        public bool Success { get; set; }
        public int ExtractedItems { get; set; }
        public int EntriesExtracted { get; set; }
        public int EntriesSkipped { get; set; }
        public int EntriesFailed { get; set; }
        public long ExtractedSize { get; set; }
        public long BytesExtracted { get; set; }
        public string DestinationPath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime EndTime { get; set; }
        public DateTime StartTime { get; set; }
        public int FilesExtracted { get; set; }
        public List<string> ExtractedFiles { get; set; } = new();
        public long TotalSize { get; set; }
        public string ArchivePath { get; set; } = string.Empty;
        public string ExtractionPath { get; set; } = string.Empty;
        public string[] ErrorMessages { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Archive integrity test result
    /// </summary>
    public class TestResult
    {
        public bool Success { get; set; }
        public int TestedEntries { get; set; }
        public string[] ErrorMessages { get; set; } = Array.Empty<string>();
        public string ArchivePath { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> TestedFiles { get; set; } = new();
        public List<string> CorruptedFiles { get; set; } = new();
    }

    /// <summary>
    /// Archive information
    /// </summary>
    public class ArchiveInfo
    {
        public string Path { get; set; } = string.Empty;
        public string ArchivePath { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public ArchiveType Type { get; set; }
        public ArchiveType ArchiveType { get; set; }
        public int EntryCount { get; set; }
        public int TotalEntries { get; set; }
        public int FolderCount { get; set; }
        public long Size { get; set; }
        public long TotalSize { get; set; }
        public long UncompressedSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedAt { get; set; }
        public ArchiveEntry[] Entries { get; set; } = Array.Empty<ArchiveEntry>();
        public bool IsEncrypted { get; set; }
        public bool IsSplit { get; set; }
        public int VolumeCount { get; set; }
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Archive entry information
    /// </summary>
    public class ArchiveEntry
    {
        public string FullName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public long UncompressedSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime ModifiedDate { get; set; }
        public uint Crc { get; set; }
        public bool IsDirectory { get; set; }
        public DateTime? CreatedDate { get; set; }
        public bool IsEncrypted { get; set; }
        public string? CompressionMethod { get; set; }
        public System.IO.FileAttributes Attributes { get; set; }
    }

    /// <summary>
    /// Archive progress information
    /// </summary>
    public class ArchiveProgress
    {
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public string CurrentItem { get; set; } = string.Empty;
        public string CurrentFile { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public int FilesProcessed { get; set; }
        public double PercentComplete { get; set; }
        public long BytesProcessed { get; set; }
        public long ProcessedBytes { get; set; }
        public long TotalBytes { get; set; }
    }

    /// <summary>
    /// Archive format information
    /// </summary>
    public class ArchiveFormat
    {
        public string Name { get; set; } = string.Empty;
        public string[] Extensions { get; set; } = Array.Empty<string>();
        public string Extension { get; set; } = string.Empty;
        public ArchiveType ArchiveType { get; set; }
        public bool CanCreate { get; set; } = true;
        public bool CanExtract { get; set; } = true;
        public bool SupportsCompression { get; set; }
        public bool SupportsEncryption { get; set; }
        public bool SupportsSplitting { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Archive handler result for implementation classes
    /// </summary>
    public class ArchiveHandlerResult
    {
        public bool Success { get; set; }
        public string? OutputPath { get; set; }
        public int ProcessedItems { get; set; }
        public int FilesProcessed { get; set; }
        public long TotalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Extraction handler result for implementation classes
    /// </summary>
    public class ExtractionHandlerResult
    {
        public bool Success { get; set; }
        public int ExtractedItems { get; set; }
        public int FilesExtracted { get; set; }
        public long ExtractedSize { get; set; }
        public long TotalSize { get; set; }
        public List<string> ExtractedFiles { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Integrity test result for implementation classes
    /// </summary>
    public class IntegrityTestResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int TestedEntries { get; set; }
        public List<string> TestedFiles { get; set; } = new();
        public List<string> CorruptedFiles { get; set; } = new();
        public string[] ErrorMessages { get; set; } = Array.Empty<string>();
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Archive type enumeration
    /// </summary>
    public enum ArchiveType
    {
        Unknown,
        Zip,
        Rar,
        SevenZip,
        Tar,
        GZip,
        BZip2
    }

    /// <summary>
    /// Compression level options for archives
    /// </summary>
    public enum CompressionLevel
    {
        None,
        Fastest,
        Optimal,
        Maximum
    }

    /// <summary>
    /// Result of archive integrity testing
    /// </summary>
    public class ArchiveTestResult
    {
        public bool Success { get; set; }
        public bool IsValid { get; set; }
        public int EntriesTested { get; set; }
        public int EntriesPassed { get; set; }
        public int EntriesFailed { get; set; }
        public List<string> Errors { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Progress information for archive extraction
    /// </summary>
    public class ExtractionProgress
    {
        public string CurrentEntry { get; set; } = string.Empty;
        public int TotalEntries { get; set; }
        public int ExtractedEntries { get; set; }
        public long TotalBytes { get; set; }
        public long ExtractedBytes { get; set; }
        public int PercentComplete => TotalEntries > 0 ? ExtractedEntries * 100 / TotalEntries : 0;
    }

    /// <summary>
    /// Options for creating archives
    /// </summary>
    public class ArchiveOptions
    {
        public int CompressionLevel { get; set; } = 5;
        public string? Password { get; set; }
        public EncryptionMethod EncryptionMethod { get; set; } = EncryptionMethod.AES256GCM;
        public bool EncryptFileNames { get; set; } = false;
        public long SplitVolumeSize { get; set; } = 0;
        public bool IncludeBaseFolder { get; set; } = false;
        public string? Comment { get; set; }
        public bool SolidArchive { get; set; } = false;
        public string[]? ExcludePatterns { get; set; }
    }

}
