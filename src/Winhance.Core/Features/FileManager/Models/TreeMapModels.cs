using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Tree map node representing a file or directory
    /// </summary>
    public class TreeMapNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public bool IsDirectory { get; set; }
        public TreeMapNode? Parent { get; set; }
        public List<TreeMapNode> Children { get; set; } = new();
        public BoundingBox Bounds { get; set; } = new();
        public BoundingBox Rectangle { get; set; } = new();
        public string Color { get; set; } = "#808080";
        public DateTime LastModified { get; set; }
        public int Depth { get; set; }
        public int Level { get; set; }
        public double SizePercentage { get; set; }
    }

    /// <summary>
    /// Tree map item interface
    /// </summary>
    public interface ITreeMapItem
    {
        string Name { get; }
        string FullPath { get; }
        long Size { get; }
        bool IsDirectory { get; }
        IEnumerable<ITreeMapItem> Children { get; }
    }

    /// <summary>
    /// Tree map visualization options
    /// </summary>
    public class TreeMapOptions
    {
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
        public int MinimumSize { get; set; } = 10;
        public bool ShowLabels { get; set; } = true;
        public bool IncludeMetadata { get; set; } = true;
        public ColorScheme ColorScheme { get; set; } = ColorScheme.FileType;
        public SizeMetric SizeMetric { get; set; } = SizeMetric.FileSize;
        public int MaxDepth { get; set; } = 5;
        public bool ShowTooltips { get; set; } = true;
    }

    /// <summary>
    /// Bounding box for positioning
    /// </summary>
    public class BoundingBox
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Area => Width * Height;
    }

    /// <summary>
    /// Directory analysis item
    /// </summary>
    public class DirectoryAnalysisItem
    {
        public string Path { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime DateModified { get; set; }
        public bool IsDirectory { get; set; } = true;
        public int Level { get; set; }
        public Dictionary<string, int> FileTypes { get; set; } = new();
    }

    /// <summary>
    /// Directory analysis result
    /// </summary>
    public class DirectoryAnalysisResult
    {
        public string RootPath { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public int TotalFiles { get; set; }
        public int TotalDirectories { get; set; }
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public List<DirectoryAnalysisItem> Items { get; set; } = new();
        public List<FileAnalysisItem> Files { get; set; } = new();
        public List<DirectoryAnalysisItem> Directories { get; set; } = new();
        public Dictionary<string, long> FileTypeSizes { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public TimeSpan AnalysisTime { get; set; }
        public long AverageFileSize { get; set; }
        public FileAnalysisItem? LargestFile { get; set; }
        public FileAnalysisItem? OldestFile { get; set; }
        public FileAnalysisItem? NewestFile { get; set; }
        public Dictionary<string, FileTypeInfo> FileTypeDistribution { get; set; } = new();
        public int MaxDepth { get; set; }
        public double AverageDepth { get; set; }
        public List<FileAnalysisItem> RecentlyAccessedFiles { get; set; } = new();
        public List<FileAnalysisItem> StaleFiles { get; set; } = new();
        public Dictionary<string, int> SizeDistribution { get; set; } = new();
    }

    /// <summary>
    /// Directory snapshot for change tracking
    /// </summary>
    public class DirectorySnapshot
    {
        public string Id { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime Date { get; set; }
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public Dictionary<string, FileSnapshot> Files { get; set; } = new();
        public Dictionary<string, string> FileHashes { get; set; } = new();
        public string[] DirectoryStructure { get; set; } = Array.Empty<string>();
        public Dictionary<string, int> FileTypeCounts { get; set; } = new();
        public FileAnalysisItem[] LargestFiles { get; set; } = Array.Empty<FileAnalysisItem>();
    }

    /// <summary>
    /// File snapshot for change tracking
    /// </summary>
    public class FileSnapshot
    {
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public string Hash { get; set; } = string.Empty;
    }

    /// <summary>
    /// Usage trend information
    /// </summary>
    public class UsageTrend
    {
        public DateTime Date { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public FileAnalysisItem[] LargestFiles { get; set; } = Array.Empty<FileAnalysisItem>();
        public List<UsageDataPoint> DataPoints { get; set; } = new();
        public TrendDirection Direction { get; set; }
        public double GrowthRate { get; set; }
    }

    /// <summary>
    /// Usage data point
    /// </summary>
    public class UsageDataPoint
    {
        public DateTime Date { get; set; }
        public long Size { get; set; }
        public int FileCount { get; set; }
    }

    /// <summary>
    /// File analysis item
    /// </summary>
    public class FileAnalysisItem
    {
        public string Path { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Accessed { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime DateAccessed { get; set; }
        public bool IsDirectory { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public bool IsCompressible { get; set; }
        public string Hash { get; set; } = string.Empty;
        public FileCategory Category { get; set; }
        public bool IsDuplicate { get; set; }
        public string? DuplicateGroup { get; set; }
    }

    /// <summary>
    /// Color scheme enumeration
    /// </summary>
    public enum ColorScheme
    {
        FileType,
        Size,
        Age,
        Custom
    }

    /// <summary>
    /// Size metric enumeration
    /// </summary>
    public enum SizeMetric
    {
        FileSize,
        FileCount,
        LastModified
    }

    /// <summary>
    /// Trend direction enumeration
    /// </summary>
    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable,
        Volatile
    }

    /// <summary>
    /// File category enumeration
    /// </summary>
    public enum FileCategory
    {
        Document,
        Documents,
        Image,
        Images,
        Video,
        Videos,
        Audio,
        Archive,
        Archives,
        Code,
        Executable,
        System,
        Temporary,
        Applications,
        Other,
        Unknown
    }

    /// <summary>
    /// Directory change information
    /// </summary>
    public class DirectoryChange
    {
        public string Path { get; set; } = string.Empty;
        public ChangeType Type { get; set; }
        public DateTime Timestamp { get; set; }
        public long SizeBefore { get; set; }
        public long SizeAfter { get; set; }
        public string? Details { get; set; }
    }

    /// <summary>
    /// Change type enumeration
    /// </summary>
    public enum ChangeType
    {
        Added,
        Modified,
        Deleted,
        Moved,
        Renamed,
        SizeChanged
    }

    /// <summary>
    /// Interactive element for treemap visualization
    /// </summary>
    public class InteractiveElement
    {
        public string NodeId { get; set; } = string.Empty;
        public BoundingBox BoundingBox { get; set; } = new();
        public string Tooltip { get; set; } = string.Empty;
        public string ClickAction { get; set; } = string.Empty;
        public string HoverColor { get; set; } = string.Empty;
        public float ZoomLevel { get; set; } = 1.0f;
        public string OnHover { get; set; } = string.Empty;
        public string OnLeave { get; set; } = string.Empty;
        public string OnClick { get; set; } = string.Empty;
    }

    /// <summary>
    /// File type information for analysis
    /// </summary>
    public class FileTypeInfo
    {
        public int Count { get; set; }
        public long TotalSize { get; set; }
        public double AverageSize { get; set; }
    }
}
