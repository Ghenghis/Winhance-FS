using System;
using System.Collections.Generic;

namespace Winhance.WPF.Features.FileManager.ViewModels.Common
{
    // Enums used across multiple ViewModels
    public enum OperationType
    {
        Copy,
        Move,
        Delete,
        Rename,
        Archive,
        Extract,
        Sync,
        Compare,
        Other
    }

    public enum OperationStatus
    {
        Pending,
        Running,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public enum ComparisonMethod
    {
        Size,
        Modified,
        Content,
        Hash,
        Name
    }

    public enum ComparisonStatus
    {
        Identical,
        Different,
        OnlyLeft,
        OnlyRight,
        Error
    }

    public enum DifferenceType
    {
        None,
        Size,
        Modified,
        Content,
        Attributes
    }

    public enum SortBy
    {
        Name,
        Size,
        Date,
        Type,
        Path
    }

    public enum SortOrder
    {
        Ascending,
        Descending
    }

    public enum HealingStrategy
    {
        Automatic,
        SemiAutomatic,
        Manual,
        Disabled
    }

    public enum ShareType
    {
        ReadOnly,
        ReadWrite,
        Full
    }

    // Classes used across multiple ViewModels
    public class DuplicateGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Hash { get; set; } = string.Empty;
        public long Size { get; set; }
        public int Count { get; set; }
        public List<DuplicateFile> Files { get; set; } = new();
        public long TotalWastedSpace => Size * (Count - 1);
    }

    public class DuplicateFile
    {
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateModified { get; set; }
        public bool IsSelected { get; set; }
    }

    public class FileOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public OperationType Type { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public long TotalBytes { get; set; }
        public long ProcessedBytes { get; set; }
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public OperationStatus Status { get; set; }
        public double ProgressPercentage => TotalBytes > 0 ? (ProcessedBytes * 100.0 / TotalBytes) : 0;
        public string? ErrorMessage { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
    }

    public class ComparisonResult
    {
        public string LeftPath { get; set; } = string.Empty;
        public string RightPath { get; set; } = string.Empty;
        public ComparisonStatus Status { get; set; }
        public DifferenceType Difference { get; set; }
        public string Details { get; set; } = string.Empty;
    }

    public class NetworkConnection
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public ShareType ShareType { get; set; }
    }

}
