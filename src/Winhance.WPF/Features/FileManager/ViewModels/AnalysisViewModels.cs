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
    public class OptimizationRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OptimizationType Type { get; set; }
        public bool IsEnabled { get; set; }
        public OptimizationLevel Level { get; set; }
        public string Pattern { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class ScanOptions
    {
        public string Path { get; set; } = string.Empty;
        public OptimizationLevel Level { get; set; }
        public bool IncludeSystemFiles { get; set; }
        public List<OptimizationRule> Rules { get; set; } = new();
        public string[] ExcludePaths { get; set; } = Array.Empty<string>();
    }

    // Enums
    public enum GroupBy
    {
        Extension,
        Category,
        Size,
        Date,
        Type
    }

    public enum SortBy
    {
        Name,
        Size,
        Count,
        AverageSize,
        Percentage,
        Date,
        Type
    }

    public enum OptimizationType
    {
        RemoveDuplicates,
        CompressFiles,
        ArchiveOldFiles,
        RemoveTempFiles,
        CleanCache,
        RemoveEmptyFolders,
        Deduplicate,
        CompressImages,
        OptimizeStorage,
        RemoveJunk,
        CleanRegistry,
        Defragment
    }

    public enum OptimizationLevel
    {
        Safe,
        Moderate,
        Aggressive,
        Extreme
    }

}
