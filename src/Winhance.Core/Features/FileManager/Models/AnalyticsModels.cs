using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Usage report model for file analytics
    /// </summary>
    public class UsageReport
    {
        public string ReportPath { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public TimeRange ReportPeriod { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        
        // File statistics
        public int TotalFiles { get; set; }
        public int TotalDirectories { get; set; }
        public long TotalSize { get; set; }
        public string? LargestFile { get; set; }
        public string? OldestFile { get; set; }
        public string? NewestFile { get; set; }
        
        // Analysis results
        public FileTypeBreakdown[] FileTypeBreakdown { get; set; } = Array.Empty<FileTypeBreakdown>();
        public SizeDistribution SizeDistribution { get; set; } = new();
        public AccessPattern[] AccessPatterns { get; set; } = Array.Empty<AccessPattern>();
    }

    /// <summary>
    /// Report generation options
    /// </summary>
    public class ReportOptions
    {
        public TimeRange Period { get; set; } = new();
        public bool IncludeSubdirectories { get; set; } = true;
        public bool IncludeAccessPatterns { get; set; } = true;
        public bool IncludeFileTypeAnalysis { get; set; } = true;
        public bool IncludeSizeDistribution { get; set; } = true;
        public bool SaveReport { get; set; } = false;
        public string? OutputPath { get; set; }
    }

    /// <summary>
    /// Performance metrics model
    /// </summary>
    public class PerformanceMetrics
    {
        public TimeRange TimeRange { get; set; } = new();
        public DateTime CollectedAt { get; set; }
        
        public DiskUsageMetric[] DiskUsage { get; set; } = Array.Empty<DiskUsageMetric>();
        public int FileOperationsPerHour { get; set; }
        public long AverageFileSize { get; set; }
        public string[] MostAccessedFiles { get; set; } = Array.Empty<string>();
        public SystemResourceUsage SystemResourceUsage { get; set; } = new();
    }

    /// <summary>
    /// Trend analysis results
    /// </summary>
    public class TrendAnalysis
    {
        public string AnalysisPath { get; set; } = string.Empty;
        public TimeRange TimeRange { get; set; } = new();
        public DateTime AnalyzedAt { get; set; }
        public string? ErrorMessage { get; set; }
        
        public GrowthTrend GrowthTrend { get; set; } = new();
        public CreationPattern[] CreationPatterns { get; set; } = Array.Empty<CreationPattern>();
        public ModificationPattern[] ModificationPatterns { get; set; } = Array.Empty<ModificationPattern>();
        public DeletionPattern[] DeletionPatterns { get; set; } = Array.Empty<DeletionPattern>();
        public TrendPrediction[] Predictions { get; set; } = Array.Empty<TrendPrediction>();
    }

    /// <summary>
    /// File type analysis results
    /// </summary>
    public class FileTypeAnalysis
    {
        public string FileType { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public long AverageSize { get; set; }
        public string LargestFile { get; set; } = string.Empty;
        public string SmallestFile { get; set; } = string.Empty;
        public string[] Extensions { get; set; } = Array.Empty<string>();
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Storage analysis across multiple drives/paths
    /// </summary>
    public class StorageAnalysis
    {
        public string[] AnalyzedPaths { get; set; } = Array.Empty<string>();
        public DateTime AnalyzedAt { get; set; }
        public string? ErrorMessage { get; set; }
        
        public List<DriveAnalysis> DriveAnalyses { get; set; } = new();
        public long TotalAnalyzedSpace { get; set; }
        public long TotalFreeSpace { get; set; }
        public double OverallUsagePercentage { get; set; }
    }

    /// <summary>
    /// Individual drive analysis
    /// </summary>
    public class DriveAnalysis
    {
        public string DriveLetter { get; set; } = string.Empty;
        public string DriveLabel { get; set; } = string.Empty;
        public long TotalSpace { get; set; }
        public long FreeSpace { get; set; }
        public long UsedSpace { get; set; }
        public double UsagePercentage { get; set; }
        
        public FileDistribution? FileDistribution { get; set; }
        public DirectorySize[] LargestDirectories { get; set; } = Array.Empty<DirectorySize>();
    }

    /// <summary>
    /// File distribution across a drive/path
    /// </summary>
    public class FileDistribution
    {
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public Dictionary<string, int> TypeBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Directory size information
    /// </summary>
    public class DirectorySize
    {
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public int FileCount { get; set; }
    }

    /// <summary>
    /// File type breakdown for reports
    /// </summary>
    public class FileTypeBreakdown
    {
        public string FileType { get; set; } = string.Empty;
        public int Count { get; set; }
        public long TotalSize { get; set; }
        public long AverageSize { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// File size distribution analysis
    /// </summary>
    public class SizeDistribution
    {
        public int SmallFiles { get; set; }      // < 1MB
        public int MediumFiles { get; set; }     // 1MB - 100MB
        public int LargeFiles { get; set; }      // >= 100MB
        public long AverageSize { get; set; }
        public long MedianSize { get; set; }
        public long TotalSize { get; set; }
    }

    /// <summary>
    /// File access pattern analysis
    /// </summary>
    public class AccessPattern
    {
        public string TimeSlot { get; set; } = string.Empty;
        public int AccessCount { get; set; }
        public string MostAccessedFile { get; set; } = string.Empty;
        public TimeSpan AverageAccessDuration { get; set; }
    }

    /// <summary>
    /// Disk usage metrics
    /// </summary>
    public class DiskUsageMetric
    {
        public string DriveLetter { get; set; } = string.Empty;
        public long TotalSpace { get; set; }
        public long FreeSpace { get; set; }
        public long UsedSpace { get; set; }
        public double UsagePercentage { get; set; }
    }

    /// <summary>
    /// System resource usage information
    /// </summary>
    public class SystemResourceUsage
    {
        public double CpuUsagePercentage { get; set; }
        public double MemoryUsagePercentage { get; set; }
        public long DiskIOPerSecond { get; set; }
        public long NetworkIOPerSecond { get; set; }
    }

    /// <summary>
    /// Growth trend analysis
    /// </summary>
    public class GrowthTrend
    {
        public int FilesAddedPerDay { get; set; }
        public long SizeGrowthPerDay { get; set; }
        public double GrowthRate { get; set; }
        public long ProjectedSizeInOneMonth { get; set; }
    }

    /// <summary>
    /// File creation pattern analysis
    /// </summary>
    public class CreationPattern
    {
        public DayOfWeek DayOfWeek { get; set; }
        public int FilesCreated { get; set; }
        public int PeakHour { get; set; }
    }

    /// <summary>
    /// File modification pattern analysis
    /// </summary>
    public class ModificationPattern
    {
        public string FileType { get; set; } = string.Empty;
        public int ModificationsPerDay { get; set; }
        public int MostActiveHour { get; set; }
    }

    /// <summary>
    /// File deletion pattern analysis
    /// </summary>
    public class DeletionPattern
    {
        public string FileType { get; set; } = string.Empty;
        public int DeletionsPerDay { get; set; }
        public TimeSpan CommonDeletionTime { get; set; }
    }

    /// <summary>
    /// Trend prediction model
    /// </summary>
    public class TrendPrediction
    {
        public string Metric { get; set; } = string.Empty;
        public long PredictedValue { get; set; }
        public double Confidence { get; set; }
        public TimeSpan TimeFrame { get; set; }
    }

    /// <summary>
    /// File statistics collection helper
    /// </summary>
    public class FileStatistics
    {
        public int TotalFiles { get; set; }
        public int TotalDirectories { get; set; }
        public long TotalSize { get; set; }
        public string? LargestFile { get; set; }
        public string? OldestFile { get; set; }
        public string? NewestFile { get; set; }
        
        public Dictionary<string, List<FileSystemEntry>> FilesByType { get; set; } = new();
        public List<long> FilesSizes { get; set; } = new();
    }
}
