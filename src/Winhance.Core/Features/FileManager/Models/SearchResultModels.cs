using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Search result item
    /// </summary>
    public class SearchResult
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime DateCreated { get; set; }
        public string Extension { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public string Type { get; set; } = string.Empty;
        public SearchMatchType MatchType { get; set; }
        public double Relevance { get; set; }
        public double Score { get; set; }
        public string Directory { get; set; } = string.Empty;
        public string MatchHighlight { get; set; } = string.Empty;
        public string[] MatchedTerms { get; set; } = Array.Empty<string>();
        public string Snippet { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Search options configuration
    /// </summary>
    public class SearchOptions
    {
        public string Query { get; set; } = string.Empty;
        public string[] SearchPaths { get; set; } = Array.Empty<string>();
        public bool SearchContent { get; set; }
        public bool IncludeDirectories { get; set; } = true;
        public bool IncludeFiles { get; set; } = true;
        public bool CaseSensitive { get; set; }
        public bool UseRegex { get; set; }
        public bool IncludeHidden { get; set; }
        public bool IncludeSystem { get; set; }
        public string[] FileExtensions { get; set; } = Array.Empty<string>();
        public string[] ExcludeExtensions { get; set; } = Array.Empty<string>();
        public string[] Extensions { get; set; } = Array.Empty<string>();
        public string[] ExcludePaths { get; set; } = Array.Empty<string>();
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public DateTime? ModifiedAfter { get; set; }
        public DateTime? ModifiedBefore { get; set; }
        public bool FilesOnly { get; set; }
        public bool DirectoriesOnly { get; set; }
        public bool Recursive { get; set; } = true;
        public bool IncludeSubdirectories { get; set; } = true;
        public long? MinSizeBytes { get; set; }
        public long? MaxSizeBytes { get; set; }
        public int MaxResults { get; set; } = 1000;
        public SearchSortMode SortBy { get; set; } = SearchSortMode.Relevance;
        public bool SortDescending { get; set; }
    }



    /// <summary>
    /// Search sort modes
    /// </summary>
    public enum SearchSortMode
    {
        Relevance,
        Name,
        Size,
        DateModified,
        DateCreated,
        Extension,
        Path
    }

    /// <summary>
    /// Search filters for advanced searching
    /// </summary>
    public class SearchFilters
    {
        public string[] Tags { get; set; } = Array.Empty<string>();
        public FileCategory[] Categories { get; set; } = Array.Empty<FileCategory>();
        public string[] MimeTypes { get; set; } = Array.Empty<string>();
        public bool? IsArchived { get; set; }
        public bool? IsEncrypted { get; set; }
        public bool? IsCompressed { get; set; }
        public string Owner { get; set; } = string.Empty;
        public Dictionary<string, object> CustomFilters { get; set; } = new();
    }

    /// <summary>
    /// Search context for maintaining search state
    /// </summary>
    public class SearchContext
    {
        public string SearchId { get; set; } = Guid.NewGuid().ToString();
        public SearchOptions Options { get; set; } = new();
        public SearchFilters Filters { get; set; } = new();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public TimeSpan Duration { get; set; }
        public int TotalResults { get; set; }
        public int ProcessedFiles { get; set; }
        public int ProcessedDirectories { get; set; }
        public bool IsCompleted { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Search statistics and performance metrics
    /// </summary>
    public class SearchStatistics
    {
        public int TotalFilesScanned { get; set; }
        public int TotalDirectoriesScanned { get; set; }
        public int MatchingFiles { get; set; }
        public int MatchingDirectories { get; set; }
        public TimeSpan SearchDuration { get; set; }
        public long BytesScanned { get; set; }
        public double FilesPerSecond { get; set; }
        public Dictionary<SearchMatchType, int> MatchTypeBreakdown { get; set; } = new();
        public Dictionary<string, int> ExtensionBreakdown { get; set; } = new();
    }
}
