using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using CoreSpaceAnalyzerService = Winhance.Core.Features.FileManager.Interfaces.ISpaceAnalyzerService;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    // Model classes
    public class DiskDriveInfo
    {
        public string Name { get; set; } = string.Empty;
        public string RootPath { get; set; } = string.Empty;
        public long TotalSpace { get; set; }
        public long FreeSpace { get; set; }
        public long UsedSpace => TotalSpace - FreeSpace;
        public string FileSystem { get; set; } = string.Empty;
        public DriveType DriveType { get; set; }
    }

    public class SpaceUsageItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public double Percentage { get; set; }
        public ItemType Type { get; set; }
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
    }

    public class FolderSizeItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        public DateTime LastModified { get; set; }
        public double SizePercentage { get; set; }
    }

    public class FileSizeItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Extension { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }

    public class LargeFileItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Extension { get; set; } = string.Empty;
    }

    public class DuplicateSpaceGroup
    {
        public string Hash { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int FileCount { get; set; }
        public long WastedSpace => FileSize * (FileCount - 1);
        public ObservableCollection<DuplicateFile> Files { get; set; } = new();
    }

    public class FileSearchOptions
    {
        public string Path { get; set; } = string.Empty;
        public long MinFileSize { get; set; }
        public int MaxResults { get; set; } = 100;
        public bool IncludeSubfolders { get; set; } = true;
        public string[] FileExtensions { get; set; } = Array.Empty<string>();
    }

    // Enums
    public enum ItemType
    {
        File,
        Folder,
        System
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }

    public enum SuggestionType
    {
        TemporaryFiles,
        RecycleBin,
        BrowserCache,
        SystemCache,
        LogFiles,
        DuplicateFiles,
        LargeFiles,
        OldFiles
    }

}
