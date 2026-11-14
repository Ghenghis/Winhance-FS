using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Nexus search query model
    /// </summary>
    public class NexusSearchQuery
    {
        public string Query { get; set; } = string.Empty;
        public NexusSearchType Type { get; set; } = NexusSearchType.FileName;
        public NexusSearchType SearchType { get; set; } = NexusSearchType.FileName;
        public string[] Paths { get; set; } = Array.Empty<string>();
        public string[] Extensions { get; set; } = Array.Empty<string>();
        public string[] FileTypes { get; set; } = Array.Empty<string>();
        public string[] Drives { get; set; } = Array.Empty<string>();
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public bool CaseSensitive { get; set; }
        public bool UseRegex { get; set; }
        public int MaxResults { get; set; } = 1000;
    }

    /// <summary>
    /// Nexus search type enumeration
    /// </summary>
    public enum NexusSearchType
    {
        FileName,
        Content,
        Path,
        Extension,
        Size,
        Date,
        Regex,
        Semantic
    }

    /// <summary>
    /// Indexing status enumeration
    /// </summary>
    public enum IndexingStatus
    {
        NotStarted,
        Indexing,
        InProgress,
        Complete,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Nexus search result
    /// </summary>
    public class NexusSearchResult
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateModified { get; set; }
        public bool IsDirectory { get; set; }
        public double Score { get; set; }
        public NexusMatchType MatchType { get; set; }
    }
}
