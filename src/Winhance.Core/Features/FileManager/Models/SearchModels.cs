using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Auto rule result from pattern analysis
    /// </summary>
    public class AutoRuleResult
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Conditions { get; set; } = new();
        public List<string> Actions { get; set; } = new();
        public double Confidence { get; set; }
        public int MatchCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Success { get; set; }
        public int RulesCreated { get; set; }
        public string Message { get; set; } = string.Empty;
        public TimeSpan ObservationPeriod { get; set; }
        public string AnalyzedPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Cleanup suggestion from analysis
    /// </summary>
    public class CleanupSuggestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Path { get; set; } = string.Empty;
        public CleanupType Type { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public long SpaceSavings { get; set; }
        public long EstimatedSpaceSaved { get; set; }
        public double Confidence { get; set; }
        public SafetyLevel SafetyLevel { get; set; } = SafetyLevel.Medium;
        public List<string> Items { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public CleanupPriority Priority { get; set; } = CleanupPriority.Medium;
    }

    /// <summary>
    /// Safety level enumeration
    /// </summary>
    public enum SafetyLevel
    {
        Low,
        Medium,
        Moderate,
        High,
        Critical,
        Cautious,
        Safe
    }

    /// <summary>
    /// Smart category result from AI analysis
    /// </summary>
    public class SmartCategoryResult
    {
        public string FilePath { get; set; } = string.Empty;
        public string SuggestedCategory { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public double Confidence { get; set; }
        public Dictionary<string, double> CategoryScores { get; set; } = new();
        public Dictionary<FileCategory, List<string>> Categories { get; set; } = new();
        public List<string> UncategorizedFiles { get; set; } = new();
        public bool Success { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// File type distribution analysis
    /// </summary>
    public class FileTypeDistribution
    {
        public string Extension { get; set; } = string.Empty;
        public int Count { get; set; }
        public long TotalSize { get; set; }
        public double Percentage { get; set; }
        public Dictionary<string, int> TypeCounts { get; set; } = new();
        public Dictionary<string, long> TypeSizes { get; set; } = new();
        public List<string> CommonLocations { get; set; } = new();
        public DateTime LastSeen { get; set; }
    }

    /// <summary>
    /// Naming pattern detected by analysis
    /// </summary>
    public class NamingPattern
    {
        public string PatternId { get; set; } = Guid.NewGuid().ToString();
        public string Pattern { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Examples { get; set; } = new();
        public int MatchCount { get; set; }
        public int FileCount { get; set; }
        public double Confidence { get; set; }
        public NamingPatternType PatternType { get; set; }
        public PatternCategory Category { get; set; }
    }

    /// <summary>
    /// Date-based pattern from file analysis
    /// </summary>
    public class DatePattern
    {
        public string PatternId { get; set; } = Guid.NewGuid().ToString();
        public DatePatternType Type { get; set; }
        public DatePatternType PatternType { get; set; }
        public DateTime Date { get; set; }
        public string Format { get; set; } = string.Empty;
        public List<string> Examples { get; set; } = new();
        public int MatchCount { get; set; }
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public double Confidence { get; set; }
        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Size-based pattern from analysis
    /// </summary>
    public class SizePattern
    {
        public string PatternId { get; set; } = Guid.NewGuid().ToString();
        public SizeRange Range { get; set; } = new();
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public long MinSize { get; set; }
        public long MaxSize { get; set; }
        public long TotalSize { get; set; }
        public long AverageSize { get; set; }
        public List<string> CommonExtensions { get; set; } = new();
        public SizeCategory Category { get; set; }
    }

    /// <summary>
    /// Size range definition
    /// </summary>
    public class SizeRange
    {
        public long MinSize { get; set; }
        public long MaxSize { get; set; }
        public string DisplayName => FormatSize(MinSize, MaxSize);

        private static string FormatSize(long min, long max)
        {
            return $"{FormatBytes(min)} - {FormatBytes(max)}";
        }

        private static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }

    /// <summary>
    /// Search match type enumeration
    /// </summary>
    public enum SearchMatchType
    {
        FileName,
        DirectoryName,
        FileContent,
        FilePath,
        FileExtension,
        FileSize,
        FileDate,
        Extension,
        Path,
        Metadata
    }

    /// <summary>
    /// Nexus match type enumeration
    /// </summary>
    public enum NexusMatchType
    {
        FileName,
        Content,
        Path,
        Extension,
        Size,
        Date
    }

    /// <summary>
    /// Cleanup type enumeration
    /// </summary>
    public enum CleanupType
    {
        Duplicates,
        DuplicateFiles,
        TemporaryFiles,
        LargeFiles,
        LargeUnusedFiles,
        LogFiles,
        OldFiles,
        UnusedFiles,
        EmptyDirectories,
        BrokenShortcuts,
        BrokenLinks,
        SystemCache,
        AIRecommended
    }

    /// <summary>
    /// Cleanup priority enumeration
    /// </summary>
    public enum CleanupPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Pattern category enumeration
    /// </summary>
    public enum PatternCategory
    {
        Sequential,
        Date,
        Version,
        Project,
        User,
        System,
        Custom
    }

    /// <summary>
    /// Naming pattern type enumeration
    /// </summary>
    public enum NamingPatternType
    {
        Prefix,
        Suffix,
        Date,
        Sequential,
        Extension,
        Custom
    }

    /// <summary>
    /// Date pattern type enumeration
    /// </summary>
    public enum DatePatternType
    {
        CreationDate,
        Creation,
        ModificationDate,
        FilenameDate,
        DirectoryDate,
        ArchiveDate
    }

    /// <summary>
    /// Size category enumeration
    /// </summary>
    public enum SizeCategory
    {
        Empty,      // 0 bytes
        Tiny,       // < 16 KB
        Small,      // 16 KB - 1 MB
        Medium,     // 1 MB - 128 MB
        Large,      // 128 MB - 1 GB
        Huge        // > 1 GB
    }
}
