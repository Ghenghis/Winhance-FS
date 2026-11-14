using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Represents a search query with all options and filters.
    /// </summary>
    public class SearchQuery
    {
        /// <summary>
        /// The search query text.
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// The path to search in.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Whether to include subfolders in the search.
        /// </summary>
        public bool IncludeSubfolders { get; set; } = true;

        /// <summary>
        /// Whether the search is case sensitive.
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Whether to use regex for the search.
        /// </summary>
        public bool UseRegex { get; set; }

        /// <summary>
        /// Whether to search in file content.
        /// </summary>
        public bool SearchContent { get; set; }

        /// <summary>
        /// Whether to include hidden files.
        /// </summary>
        public bool IncludeHidden { get; set; }

        /// <summary>
        /// The filter type for results.
        /// </summary>
        public FilterType FilterType { get; set; } = FilterType.All;

        /// <summary>
        /// Date range start for filtering.
        /// </summary>
        public DateTime? DateFrom { get; set; }

        /// <summary>
        /// Date range end for filtering.
        /// </summary>
        public DateTime? DateTo { get; set; }

        /// <summary>
        /// Minimum file size in bytes.
        /// </summary>
        public long? SizeMin { get; set; }

        /// <summary>
        /// Maximum file size in bytes.
        /// </summary>
        public long? SizeMax { get; set; }

        /// <summary>
        /// File extensions to filter by.
        /// </summary>
        public List<string> FileExtensions { get; set; } = new();

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int MaxResults { get; set; } = 1000;

        /// <summary>
        /// Tags to filter by.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Creates a copy of this query.
        /// </summary>
        public SearchQuery Clone()
        {
            return new SearchQuery
            {
                Query = Query,
                Path = Path,
                IncludeSubfolders = IncludeSubfolders,
                CaseSensitive = CaseSensitive,
                UseRegex = UseRegex,
                SearchContent = SearchContent,
                IncludeHidden = IncludeHidden,
                FilterType = FilterType,
                DateFrom = DateFrom,
                DateTo = DateTo,
                SizeMin = SizeMin,
                SizeMax = SizeMax,
                FileExtensions = new List<string>(FileExtensions),
                MaxResults = MaxResults,
                Tags = new List<string>(Tags)
            };
        }
    }

    /// <summary>
    /// Filter type for search results.
    /// </summary>
    public enum FilterType
    {
        /// <summary>All files and folders.</summary>
        All,
        /// <summary>Files only.</summary>
        Files,
        /// <summary>Folders only.</summary>
        Folders,
        /// <summary>Image files.</summary>
        Images,
        /// <summary>Document files.</summary>
        Documents,
        /// <summary>Video files.</summary>
        Videos,
        /// <summary>Audio files.</summary>
        Audio,
        /// <summary>Archive files.</summary>
        Archives,
        /// <summary>Executable files.</summary>
        Executables,
        /// <summary>Source code files.</summary>
        SourceCode,
        /// <summary>Custom filter.</summary>
        Custom
    }

    /// <summary>
    /// Represents a saved search query.
    /// </summary>
    public class SavedSearchQuery
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Display name for the saved search.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The search query configuration.
        /// </summary>
        public SearchQuery Query { get; set; } = new();

        /// <summary>
        /// When the search was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// When the search was last used.
        /// </summary>
        public DateTime LastUsedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Number of times this search has been used.
        /// </summary>
        public int UseCount { get; set; }
    }
}
