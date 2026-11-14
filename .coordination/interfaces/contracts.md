# Interface Contracts

**Purpose:** Shared interface definitions that both agents must follow.
**Owner:** Claude Code creates, Windsurf consumes
**Last Updated:** 2026-01-24

---

## How This Works

1. Claude Code defines interfaces here FIRST
2. Windsurf reads and implements ViewModels against these contracts
3. Any changes require notification to Windsurf
4. Breaking changes require coordination

---

## IFileManagerService

**Status:** ✅ IMPLEMENTED - Basic file operations
**Needed By:** DualPaneBrowserViewModel (currently using fallback)

```csharp
namespace Winhance.Core.Features.FileManager.Interfaces
{
    public interface IFileManagerService
    {
        /// <summary>
        /// Get directory contents with file information.
        </summary>
        Task<IEnumerable<FileEntry>> GetDirectoryContentsAsync(
            string path,
            CancellationToken ct = default);

        /// <summary>
        /// Copy files or directories.
        /// </summary>
        Task CopyAsync(
            string source,
            string destination,
            IProgress<OperationProgress> progress,
            CancellationToken ct = default);

        /// <summary>
        /// Move files or directories.
        /// </summary>
        Task MoveAsync(
            string source,
            string destination,
            IProgress<OperationProgress> progress,
            CancellationToken ct = default);

        /// <summary>
        /// Delete files or directories.
        /// </summary>
        Task DeleteAsync(
            string path,
            bool permanent = false,
            IProgress<OperationProgress> progress,
            CancellationToken ct = default);

        /// <summary>
        /// Create a new directory.
        /// </summary>
        Task CreateDirectoryAsync(
            string path,
            CancellationToken ct = default);

        /// <summary>
        /// Rename a file or directory.
        /// </summary>
        Task RenameAsync(
            string path,
            string newName,
            CancellationToken ct = default);

        /// <summary>
        /// Check if path exists.
        /// </summary>
        Task<bool> ExistsAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Get file or directory information.
        /// </summary>
        Task<FileEntry?> GetInfoAsync(string path, CancellationToken ct = default);
    }

    public class FileEntry
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime DateCreated { get; set; }
        public string Extension { get; set; } = string.Empty;
        public FileAttributes Attributes { get; set; }
    }

    public class OperationProgress
    {
        public int PercentComplete { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public long BytesProcessed { get; set; }
        public long TotalBytes { get; set; }
        public int FilesProcessed { get; set; }
        public int TotalFiles { get; set; }
    }
}
```

---

## Phase 1 Interfaces

### ISearchService

**Status:** PENDING - Claude Code to implement
**Needed By:** Windsurf for SearchResultsViewModel

```csharp
namespace Winhance.Core.Features.FileManager.Interfaces
{
    public interface ISearchService
    {
        /// <summary>
        /// Search for files matching the query.
        /// </summary>
        Task<IEnumerable<SearchResult>> SearchAsync(
            string query,
            SearchOptions options,
            CancellationToken ct = default);

        /// <summary>
        /// Index a drive for fast searching.
        /// </summary>
        Task<bool> IndexDriveAsync(
            string driveLetter,
            IProgress<IndexProgress> progress,
            CancellationToken ct = default);

        /// <summary>
        /// Get the current index status.
        /// </summary>
        Task<IndexStatus> GetIndexStatusAsync(CancellationToken ct = default);

        /// <summary>
        /// Cancel ongoing indexing.
        /// </summary>
        Task CancelIndexingAsync();
    }

    public class SearchResult
    {
        public string FullPath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsDirectory { get; set; }
        public string Extension { get; set; } = string.Empty;
        public double Score { get; set; }
        public string MatchHighlight { get; set; } = string.Empty;
    }

    public class SearchOptions
    {
        public bool UseRegex { get; set; } = false;
        public bool CaseSensitive { get; set; } = false;
        public bool SearchContents { get; set; } = false;
        public bool IncludeHidden { get; set; } = false;
        public string[]? Extensions { get; set; }
        public string[]? ExcludeExtensions { get; set; }
        public string[]? SearchPaths { get; set; }
        public string[]? ExcludePaths { get; set; }
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public DateTime? ModifiedAfter { get; set; }
        public DateTime? ModifiedBefore { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int MaxResults { get; set; } = 1000;
        public SearchSortBy SortBy { get; set; } = SearchSortBy.Relevance;
        public bool SortDescending { get; set; } = true;
    }

    public enum SearchSortBy
    {
        Relevance,
        Name,
        Size,
        DateModified,
        DateCreated,
        Extension,
        Path
    }

    public class IndexProgress
    {
        public string CurrentPath { get; set; } = string.Empty;
        public long FilesIndexed { get; set; }
        public long TotalFiles { get; set; }
        public int PercentComplete { get; set; }
        public string Phase { get; set; } = string.Empty;
    }

    public class IndexStatus
    {
        public bool IsIndexing { get; set; }
        public DateTime? LastIndexed { get; set; }
        public long TotalFilesIndexed { get; set; }
        public long IndexSizeBytes { get; set; }
        public Dictionary<string, DriveIndexStatus> DriveStatuses { get; set; } = new();
    }

    public class DriveIndexStatus
    {
        public string DriveLetter { get; set; } = string.Empty;
        public bool IsIndexed { get; set; }
        public DateTime? LastIndexed { get; set; }
        public long FilesIndexed { get; set; }
    }
}
```

---

### ITabService

**Status:** PENDING - Claude Code to implement
**Needed By:** Windsurf for TabContainerViewModel

```csharp
namespace Winhance.Core.Features.FileManager.Interfaces
{
    public interface ITabService
    {
        /// <summary>
        /// Observable collection of open tabs.
        /// </summary>
        ObservableCollection<FileTab> Tabs { get; }

        /// <summary>
        /// Currently active tab.
        /// </summary>
        FileTab? ActiveTab { get; set; }

        /// <summary>
        /// Event raised when active tab changes.
        /// </summary>
        event EventHandler<FileTab?> ActiveTabChanged;

        /// <summary>
        /// Create a new tab at the specified path.
        /// </summary>
        FileTab CreateTab(string path, bool activate = true);

        /// <summary>
        /// Close a specific tab.
        /// </summary>
        void CloseTab(FileTab tab);

        /// <summary>
        /// Close all tabs except the specified one.
        /// </summary>
        void CloseOtherTabs(FileTab tab);

        /// <summary>
        /// Close tabs to the right of the specified tab.
        /// </summary>
        void CloseTabsToRight(FileTab tab);

        /// <summary>
        /// Duplicate a tab.
        /// </summary>
        FileTab DuplicateTab(FileTab tab);

        /// <summary>
        /// Pin/unpin a tab.
        /// </summary>
        void TogglePin(FileTab tab);

        /// <summary>
        /// Reopen the last closed tab.
        /// </summary>
        FileTab? ReopenClosedTab();

        /// <summary>
        /// Navigate a tab to a new path.
        /// </summary>
        void NavigateTo(FileTab tab, string path);

        /// <summary>
        /// Go back in tab history.
        /// </summary>
        bool GoBack(FileTab tab);

        /// <summary>
        /// Go forward in tab history.
        /// </summary>
        bool GoForward(FileTab tab);

        /// <summary>
        /// Save current session.
        /// </summary>
        Task SaveSessionAsync(string name);

        /// <summary>
        /// Load a saved session.
        /// </summary>
        Task<bool> LoadSessionAsync(string name);

        /// <summary>
        /// Get list of saved sessions.
        /// </summary>
        IEnumerable<string> GetSavedSessions();

        /// <summary>
        /// Delete a saved session.
        /// </summary>
        void DeleteSession(string name);
    }

    public class FileTab : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string CurrentPath { get; set; } = string.Empty;
        public Stack<string> BackHistory { get; set; } = new();
        public Stack<string> ForwardHistory { get; set; } = new();
        public bool IsPinned { get; set; }
        public bool CanGoBack => BackHistory.Count > 0;
        public bool CanGoForward => ForwardHistory.Count > 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    }
}
```

---

### IFavoritesService

**Status:** PENDING - Claude Code to implement
**Needed By:** Windsurf for FavoritesPanelViewModel

```csharp
namespace Winhance.Core.Features.FileManager.Interfaces
{
    public interface IFavoritesService
    {
        /// <summary>
        /// Observable collection of favorite items.
        /// </summary>
        ObservableCollection<FavoriteItem> Favorites { get; }

        /// <summary>
        /// Add a path to favorites.
        /// </summary>
        FavoriteItem AddFavorite(string path, string? customName = null, string? group = null);

        /// <summary>
        /// Remove a favorite.
        /// </summary>
        void RemoveFavorite(FavoriteItem item);

        /// <summary>
        /// Update a favorite's properties.
        /// </summary>
        void UpdateFavorite(FavoriteItem item);

        /// <summary>
        /// Reorder favorites.
        /// </summary>
        void MoveFavorite(FavoriteItem item, int newIndex);

        /// <summary>
        /// Get recent locations.
        /// </summary>
        IEnumerable<RecentLocation> GetRecentLocations(int count = 20);

        /// <summary>
        /// Get frequently accessed locations.
        /// </summary>
        IEnumerable<FrequentLocation> GetFrequentLocations(int count = 10);

        /// <summary>
        /// Clear recent history.
        /// </summary>
        void ClearRecentHistory();

        /// <summary>
        /// Record a location visit (for tracking).
        /// </summary>
        void RecordVisit(string path);

        /// <summary>
        /// Check if path is a favorite.
        /// </summary>
        bool IsFavorite(string path);

        /// <summary>
        /// Get favorite groups.
        /// </summary>
        IEnumerable<string> GetGroups();

        /// <summary>
        /// Create a new group.
        /// </summary>
        void CreateGroup(string name);

        /// <summary>
        /// Delete a group (moves items to ungrouped).
        /// </summary>
        void DeleteGroup(string name);

        /// <summary>
        /// Save favorites to persistent storage.
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// Load favorites from persistent storage.
        /// </summary>
        Task LoadAsync();
    }

    public class FavoriteItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Group { get; set; }
        public int SortOrder { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public string IconPath { get; set; } = string.Empty;
        public bool IsValid => Directory.Exists(Path) || File.Exists(Path);
    }

    public class RecentLocation
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime LastVisited { get; set; }
    }

    public class FrequentLocation
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int VisitCount { get; set; }
        public DateTime LastVisited { get; set; }
    }
}
```

---

### IOperationQueueService

**Status:** PENDING - Claude Code to implement
**Needed By:** Windsurf for OperationProgressDialog

```csharp
namespace Winhance.Core.Features.FileManager.Interfaces
{
    public interface IOperationQueueService
    {
        /// <summary>
        /// Observable collection of queued operations.
        /// </summary>
        ObservableCollection<FileOperation> Operations { get; }

        /// <summary>
        /// Currently running operation.
        /// </summary>
        FileOperation? CurrentOperation { get; }

        /// <summary>
        /// Event for operation progress updates.
        /// </summary>
        event EventHandler<OperationProgressEventArgs> ProgressChanged;

        /// <summary>
        /// Event when operation completes.
        /// </summary>
        event EventHandler<OperationCompletedEventArgs> OperationCompleted;

        /// <summary>
        /// Queue a copy operation.
        /// </summary>
        FileOperation QueueCopy(IEnumerable<string> sources, string destination);

        /// <summary>
        /// Queue a move operation.
        /// </summary>
        FileOperation QueueMove(IEnumerable<string> sources, string destination);

        /// <summary>
        /// Queue a delete operation.
        /// </summary>
        FileOperation QueueDelete(IEnumerable<string> paths, bool permanent = false);

        /// <summary>
        /// Pause an operation.
        /// </summary>
        void Pause(FileOperation operation);

        /// <summary>
        /// Resume a paused operation.
        /// </summary>
        void Resume(FileOperation operation);

        /// <summary>
        /// Cancel an operation.
        /// </summary>
        void Cancel(FileOperation operation);

        /// <summary>
        /// Change operation priority (move up/down in queue).
        /// </summary>
        void ChangePriority(FileOperation operation, int newPosition);

        /// <summary>
        /// Retry a failed operation.
        /// </summary>
        void Retry(FileOperation operation);

        /// <summary>
        /// Handle a conflict.
        /// </summary>
        void ResolveConflict(FileOperation operation, ConflictResolution resolution);

        /// <summary>
        /// Get operation history.
        /// </summary>
        IEnumerable<FileOperation> GetHistory(int count = 50);

        /// <summary>
        /// Clear completed operations from history.
        /// </summary>
        void ClearHistory();
    }

    public class FileOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public OperationType Type { get; set; }
        public OperationStatus Status { get; set; }
        public IEnumerable<string> SourcePaths { get; set; } = Array.Empty<string>();
        public string? DestinationPath { get; set; }
        public long TotalBytes { get; set; }
        public long ProcessedBytes { get; set; }
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public double SpeedBytesPerSecond { get; set; }
        public TimeSpan EstimatedRemaining { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public ConflictInfo? PendingConflict { get; set; }

        public int PercentComplete => TotalBytes > 0
            ? (int)(ProcessedBytes * 100 / TotalBytes)
            : 0;
    }

    public enum OperationType { Copy, Move, Delete }

    public enum OperationStatus
    {
        Queued,
        Running,
        Paused,
        Conflict,
        Completed,
        Failed,
        Cancelled
    }

    public class ConflictInfo
    {
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public long SourceSize { get; set; }
        public long DestinationSize { get; set; }
        public DateTime SourceModified { get; set; }
        public DateTime DestinationModified { get; set; }
    }

    public enum ConflictResolution
    {
        Skip,
        SkipAll,
        Overwrite,
        OverwriteAll,
        OverwriteIfNewer,
        OverwriteIfNewerAll,
        Rename,
        RenameAll,
        Cancel
    }

    public class OperationProgressEventArgs : EventArgs
    {
        public FileOperation Operation { get; set; } = null!;
    }

    public class OperationCompletedEventArgs : EventArgs
    {
        public FileOperation Operation { get; set; } = null!;
        public bool Success { get; set; }
        public int FilesProcessed { get; set; }
        public int FilesFailed { get; set; }
    }
}
```

---

## Existing Interfaces (Implementation Status)

| Interface                  | Status            | Location/Notes                  |
| -------------------------- | ----------------- | ------------------------------- |
| `IBatchRenameService`      | ❌ Not Implemented | Needed for batch rename UI      |
| `IOrganizerService`        | ❌ Not Implemented | Needed for smart organizer      |
| `ISpaceRecoveryService`    | ❌ Not Implemented | Needed for space analyzer       |
| `IFileManagerService`      | ✅ IMPLEMENTED     | Basic file operations           |
| `ISearchService`           | ✅ IMPLEMENTED     | Full search with indexing       |
| `ITabService`              | ✅ IMPLEMENTED     | Tab management with persistence |
| `IFavoritesService`        | ✅ IMPLEMENTED     | Favorites with recent/frequent  |
| `IOperationQueueService`   | ✅ IMPLEMENTED     | Background file operations      |
| `IDriveDetectionService`   | ✅ IMPLEMENTED     | 80 tests passing                |
| `IBackupProtectionService` | ✅ IMPLEMENTED     | 80 tests passing                |

**Claude Code Priority:** Implement remaining services to enable full UI functionality.

---

## NEW: IDriveDetectionService (Implemented 2026-01-24)

**Status:** ✅ IMPLEMENTED - 80 unit tests passing
**Location:** `Winhance.Core/Features/FileManager/Interfaces/IDriveDetectionService.cs`
**Implementation:** `Winhance.Infrastructure/Features/FileManager/Services/DriveDetectionService.cs`
**DI Registration:** Added to InfrastructureServicesExtensions.cs

### Key Features:
- Real hardware detection via WMI (drive ID, model, serial, firmware)
- Drive type classification (NVMe, SATA SSD, HDD, USB 2.0/3.0/3.2 Gen2)
- Speed estimation based on drive type and bus
- Optimal transfer settings calculation for source/destination pairs
- Speed testing with benchmarks
- Cached drive info with 30-second expiration

### Usage Example for ViewModels:
```csharp
// Inject the service
private readonly IDriveDetectionService _driveService;

// Get all drives with detailed info
var drives = await _driveService.GetAllDrivesAsync();
foreach (var drive in drives)
{
    Console.WriteLine($"{drive.DriveLetter}: {drive.TypeDescription}");
    Console.WriteLine($"  Model: {drive.Model}");
    Console.WriteLine($"  Free: {drive.FreeSpaceDisplay} of {drive.TotalCapacityDisplay}");
    Console.WriteLine($"  Speed: ~{drive.MaxReadSpeedMBps} MB/s read");
}

// Get optimal transfer settings
var settings = await _driveService.GetOptimalTransferSettingsAsync("C", "D");
Console.WriteLine($"Buffer: {settings.BufferSize / (1024*1024)}MB, Parallel: {settings.ParallelOperations}");
```

---

## NEW: IBackupProtectionService (Implemented 2026-01-24)

**Status:** ✅ IMPLEMENTED - 80 unit tests passing
**Location:** `Winhance.Core/Features/FileManager/Interfaces/IBackupProtectionService.cs`
**Implementation:** `Winhance.Infrastructure/Features/FileManager/Services/BackupProtectionService.cs`
**DI Registration:** Added to InfrastructureServicesExtensions.cs

### Key Features:
- Automatic backup before dangerous file operations (move, delete, rename)
- Multi-level protection (None, Basic, Standard, Enhanced, Maximum)
- SHA-256 hash verification for backup integrity
- Batch backup for multi-file operations
- Full rollback support with transaction IDs
- Compression at Enhanced+ levels (GZip)
- Session persistence across app restarts
- Storage management (cleanup old backups, storage info)
- Can be disabled in settings via `IsEnabled` property

### Usage Example for ViewModels:
```csharp
// Inject the service
private readonly IBackupProtectionService _backupService;

// Create backup before dangerous operation
var session = await _backupService.CreateBackupAsync(new FileOperationRequest
{
    SourcePath = @"C:\Important\file.txt",
    OperationType = FileOperationType.Delete
});

try
{
    // Perform the operation
    File.Delete(@"C:\Important\file.txt");

    // If successful, commit (deletes backup)
    await _backupService.CommitBackupAsync(session.SessionId);
}
catch
{
    // On failure, rollback restores the file
    await _backupService.RollbackAsync(session.SessionId);
}

// User can disable backup in settings
_backupService.IsEnabled = false;
```

---

## Phase 2 Interfaces (Preview)

These will be defined in Week 4 for Phase 2 work:

- `IArchiveService` - Archive browsing and operations
- `IDuplicateScannerService` - Duplicate detection
- `IPreviewService` - File preview generation
- `IPresetService` - Batch rename presets

---

## Change Log

| Date       | Interface              | Change                        | Breaking |
| ---------- | ---------------------- | ----------------------------- | -------- |
| 2026-01-24 | All                    | Initial definitions           | N/A      |
| 2026-01-24 | IFileManagerService    | Added implemented interface   | No       |
| 2026-01-24 | ISearchService         | Changed status to IMPLEMENTED | No       |
| 2026-01-24 | ITabService            | Changed status to IMPLEMENTED | No       |
| 2026-01-24 | IFavoritesService      | Changed status to IMPLEMENTED | No       |
| 2026-01-24 | IOperationQueueService | Changed status to IMPLEMENTED | No       |

---
## Notes

- All interfaces use `CancellationToken` for async operations
- All collections should be `ObservableCollection<T>` for UI binding
- Use `IProgress<T>` for progress reporting
- Events use standard `EventHandler<T>` pattern
