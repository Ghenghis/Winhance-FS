using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Cloud provider enumeration
    /// </summary>
    public enum CloudProvider
    {
        OneDrive,
        GoogleDrive,
        Dropbox,
        AmazonS3,
        Azure,
        iCloud,
        Box,
        pCloud,
        Custom
    }

    /// <summary>
    /// General sync result
    /// </summary>
    public class SyncResult
    {
        public bool Success { get; set; }
        public int FilesUploaded { get; set; }
        public int FilesDownloaded { get; set; }
        public int FilesCopied { get; set; }
        public int FilesUpdated { get; set; }
        public int FilesDeleted { get; set; }
        public int FilesSkipped { get; set; }
        public int FilesFailed { get; set; }
        public int ConflictsResolved { get; set; }
        public TimeSpan SyncTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public long BytesTransferred { get; set; }
    }

    /// <summary>
    /// Sync job status enumeration
    /// </summary>
    public enum SyncStatus
    {
        NotRun,
        Running,
        Success,
        Warning,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Cloud sync status information
    /// </summary>
    public class CloudSyncStatus
    {
        public bool IsInSync { get; set; }
        public DateTime LastSyncTime { get; set; }
        public int PendingChanges { get; set; }
        public List<string> ConflictingFiles { get; set; } = new();
        public CloudProvider Provider { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bidirectional sync result
    /// </summary>
    public class BidirectionalSyncResult
    {
        public bool Success { get; set; }
        public int UploadedFiles { get; set; }
        public int DownloadedFiles { get; set; }
        public int ConflictsResolved { get; set; }
        public int ErrorCount { get; set; }
        public List<SyncConflict> Conflicts { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public long BytesTransferred { get; set; }
    }

    /// <summary>
    /// Sync conflict information
    /// </summary>
    public class SyncConflict
    {
        public string LocalPath { get; set; } = string.Empty;
        public string CloudPath { get; set; } = string.Empty;
        public ConflictType Type { get; set; }
        public string Resolution { get; set; } = string.Empty;
        public DateTime LocalModified { get; set; }
        public DateTime CloudModified { get; set; }
    }

    /// <summary>
    /// Conflict type enumeration
    /// </summary>
    public enum ConflictType
    {
        ModifiedOnBothSides,
        DeletedLocally,
        DeletedOnCloud,
        TypeMismatch,
        PermissionDenied
    }

    /// <summary>
    /// Tree map data structure
    /// </summary>
    public class TreeMapData
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string RootPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public bool IsDirectory { get; set; }
        public List<TreeMapData> Children { get; set; } = new();
        public List<TreeMapNode> Nodes { get; set; } = new();
        public string Color { get; set; } = "#808080";
        public int Depth { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TreeMapOptions Options { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Tree map visualization result
    /// </summary>
    public class TreeMapVisualization
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TreeMapData RootNode { get; set; } = new();
        public int TotalItems { get; set; }
        public long TotalSize { get; set; }
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
        public Dictionary<string, string> ColorMap { get; set; } = new();
        public BoundingBox ViewBox { get; set; } = new();
        public List<InteractiveElement> InteractiveElements { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Visualization options
    /// </summary>
    public class VisualizationOptions
    {
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
        public ColorScheme ColorScheme { get; set; } = ColorScheme.FileType;
        public int MaxDepth { get; set; } = 5;
        public long MinimumSize { get; set; } = 1024; // 1KB
        public bool ShowLabels { get; set; } = true;
        public string FontFamily { get; set; } = "Arial";
        public int FontSize { get; set; } = 12;
    }

    /// <summary>
    /// Storage optimization suggestion
    /// </summary>
    public class StorageOptimization
    {
        public string Path { get; set; } = string.Empty;
        public OptimizationType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public long PotentialSavings { get; set; }
        public double Confidence { get; set; }
        public List<string> AffectedFiles { get; set; } = new();
        public List<string> Items { get; set; } = new();
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Layout result for tree map
    /// </summary>
    public class LayoutResult
    {
        public List<TreeMapNode> Nodes { get; set; } = new();
        public BoundingBox TotalBounds { get; set; } = new();
        public BoundingBox Rectangle { get; set; } = new();
        public TreeMapNode Item { get; set; } = new();
        public int ProcessedNodes { get; set; }
        public TimeSpan LayoutTime { get; set; }
    }

    /// <summary>
    /// Optimization type enumeration
    /// </summary>
    public enum OptimizationType
    {
        DuplicateRemoval,
        Compression,
        Archiving,
        CloudStorage,
        Cleanup,
        Defragmentation,
        LargeFiles,
        Duplicates
    }
}
