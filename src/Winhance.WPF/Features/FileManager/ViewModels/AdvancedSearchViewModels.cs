using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    // Model classes
    public class SearchCondition
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public SearchField Field { get; set; }
        public SearchOperator Operator { get; set; }
        public string Value { get; set; } = string.Empty;
        public bool CaseSensitive { get; set; }
        public bool UseRegex { get; set; }
        public string? SecondValue { get; set; } // For range operators
    }

    public class SearchTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<SearchCondition> Conditions { get; set; } = new();
        public LogicalOperator ConditionOperator { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UsageCount { get; set; }
    }

    public class AdvancedSearchQuery
    {
        public string Name { get; set; } = string.Empty;
        public List<SearchCondition> Conditions { get; set; } = new();
        public LogicalOperator ConditionOperator { get; set; }
        public SearchScope Scope { get; set; }
        public string Path { get; set; } = string.Empty;
        public bool IncludeSubfolders { get; set; }
        public bool IncludeHiddenFiles { get; set; }
        public bool IncludeSystemFiles { get; set; }
        public int MaxResults { get; set; }
        public SearchSortBy SortBy { get; set; }
        public SortOrder SortOrder { get; set; }
    }

    public class DuplicateGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Hash { get; set; } = string.Empty;
        public List<DuplicateFile> Files { get; set; } = new();
        public long FileSize { get; set; }
        public int FileCount { get; set; }
        public long TotalWastedSpace { get; set; }
        public ComparisonMethod ComparisonMethod { get; set; }
    }

    public class DuplicateFile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime AccessedDate { get; set; }
        public string Hash { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsProtected { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DuplicateSearchOptions
    {
        public string Path { get; set; } = string.Empty;
        public bool IncludeSubfolders { get; set; }
        public long MinFileSize { get; set; }
        public long MaxFileSize { get; set; }
        public string[] FileTypesFilter { get; set; } = Array.Empty<string>();
        public bool ExcludeSystemFiles { get; set; }
        public ComparisonMethod ComparisonMethod { get; set; }
    }

    public class DuplicateScanProgress
    {
        public double Percentage { get; set; }
        public int FilesScanned { get; set; }
        public int DuplicatesFound { get; set; }
        public long WastedSpace { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public TimeSpan EstimatedTimeRemaining { get; set; }
    }

    // Enums
    public enum SearchField
    {
        FileName,
        FilePath,
        FileSize,
        CreatedDate,
        ModifiedDate,
        AccessedDate,
        FileExtension,
        ContentType,
        Attributes,
        Content,
        Checksum,
        Owner,
        Permissions
    }

    public enum SearchOperator
    {
        Equals,
        NotEquals,
        Contains,
        NotContains,
        StartsWith,
        EndsWith,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Between,
        Regex,
        In,
        NotIn,
        IsEmpty,
        IsNotEmpty
    }

    public enum LogicalOperator
    {
        And,
        Or
    }

    public enum SearchScope
    {
        CurrentFolder,
        AllDrives,
        SpecificPath,
        SelectedFiles
    }

    public enum SearchSortBy
    {
        Name,
        Size,
        Date,
        Type,
        Path,
        Relevance
    }

    public enum SortOrder
    {
        Ascending,
        Descending
    }

    public enum ComparisonMethod
    {
        QuickHash,
        FullHash,
        ByteCompare,
        SizeMatch
    }

}
