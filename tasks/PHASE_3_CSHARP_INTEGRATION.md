# Phase 3: C# Integration & Services

> **[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 2](PHASE_2_RUST_BACKEND.md) | **Next:** [Phase 4](PHASE_4_UI_THEMING.md)

---

## Phase Overview

| Attribute | Value |
|-----------|-------|
| **Status** | `[ ]` Not Started |
| **Priority** | Critical |
| **Estimated Tasks** | 20 |
| **Dependencies** | [Phase 2](PHASE_2_RUST_BACKEND.md) Complete |

---

## Objectives

1. Create C# wrapper for Rust nexus-native library
2. Implement Storage Intelligence services following Winhance patterns
3. Create domain models in Winhance.Core
4. Build infrastructure services in Winhance.Infrastructure
5. Follow OperationResult<T> pattern consistently

---

## Winhance Patterns Reference

### OperationResult Pattern
```csharp
public class OperationResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public Exception? Exception { get; init; }

    public static OperationResult<T> Succeeded(T data) => new() { Success = true, Data = data };
    public static OperationResult<T> Failed(string message) => new() { Success = false, ErrorMessage = message };
    public static OperationResult<T> Failed(Exception ex) => new() { Success = false, Exception = ex, ErrorMessage = ex.Message };
}
```

### Service Interface Pattern
```csharp
public interface IStorageService
{
    Task<OperationResult<T>> MethodAsync(params);
}
```

---

## Task List

### 3.1 Native Wrapper

#### Task 3.1.1: Create NexusNative Wrapper Class

**Status:** `[ ]` Not Started

**Description:**
Create a C# wrapper class that loads and interacts with the Rust DLL.

**Acceptance Criteria:**
- [ ] `NexusNativeWrapper.cs` loads `nexus_native.dll`
- [ ] All UniFFI-generated functions wrapped
- [ ] Proper error handling with try/catch
- [ ] IDisposable implemented for cleanup

**Files to Create:**
```
src/Winhance.Infrastructure/Features/Storage/Native/NexusNativeWrapper.cs
src/Winhance.Infrastructure/Features/Storage/Native/NexusNativeExtensions.cs
```

**Implementation:**
```csharp
// NexusNativeWrapper.cs
namespace Winhance.Infrastructure.Features.Storage.Native;

using Winhance.Core.Features.Storage.Models;
using Winhance.Core.Features.Storage.Interfaces;

public sealed class NexusNativeWrapper : INexusNative, IDisposable
{
    private readonly NexusRuntime _runtime;
    private bool _disposed;

    public NexusNativeWrapper()
    {
        _runtime = new NexusRuntime();
    }

    public async Task<OperationResult<ScanResult>> ScanDriveAsync(
        string driveLetter,
        ScanOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            options ??= ScanOptions.Default;

            var result = await Task.Run(() =>
                _runtime.ScanDrive(driveLetter, options.ToNative()),
                cancellationToken);

            return OperationResult<ScanResult>.Succeeded(result.ToManaged());
        }
        catch (NexusException ex)
        {
            return OperationResult<ScanResult>.Failed($"Scan failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OperationResult<ScanResult>.Failed(ex);
        }
    }

    public async Task<OperationResult<IReadOnlyList<SearchResult>>> SearchAsync(
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            options ??= SearchOptions.Default;

            var results = await Task.Run(() =>
                _runtime.Search(query, options.ToNative()),
                cancellationToken);

            return OperationResult<IReadOnlyList<SearchResult>>.Succeeded(
                results.Select(r => r.ToManaged()).ToList());
        }
        catch (NexusException ex)
        {
            return OperationResult<IReadOnlyList<SearchResult>>.Failed($"Search failed: {ex.Message}");
        }
    }

    public async Task<OperationResult<double>> CalculateEntropyAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entropy = await Task.Run(() =>
                _runtime.CalculateEntropy(filePath),
                cancellationToken);

            return OperationResult<double>.Succeeded(entropy);
        }
        catch (NexusException ex)
        {
            return OperationResult<double>.Failed($"Entropy calculation failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _runtime.Dispose();
            _disposed = true;
        }
    }
}
```

**Next Task:** [3.1.2](#task-312-create-native-type-extensions)

---

#### Task 3.1.2: Create Native Type Extensions

**Status:** `[ ]` Not Started

**Description:**
Create extension methods to convert between UniFFI types and managed types.

**Acceptance Criteria:**
- [ ] `ToNative()` extensions for all options types
- [ ] `ToManaged()` extensions for all result types
- [ ] No data loss during conversion

**Files to Create:**
```
src/Winhance.Infrastructure/Features/Storage/Native/NexusNativeExtensions.cs
```

**Implementation:**
```csharp
// NexusNativeExtensions.cs
namespace Winhance.Infrastructure.Features.Storage.Native;

public static class NexusNativeExtensions
{
    // Convert managed ScanOptions to native
    public static uniffi.nexus.ScanOptions ToNative(this ScanOptions options)
    {
        return new uniffi.nexus.ScanOptions
        {
            IncludeHidden = options.IncludeHidden,
            IncludeSystem = options.IncludeSystem,
            MaxDepth = options.MaxDepth
        };
    }

    // Convert native ScanResult to managed
    public static ScanResult ToManaged(this uniffi.nexus.ScanResult result)
    {
        return new ScanResult
        {
            Entries = result.Entries.Select(e => e.ToManaged()).ToList(),
            TotalSize = result.TotalSize,
            FileCount = result.FileCount,
            DirectoryCount = result.DirectoryCount,
            ScanDurationMs = result.ScanDurationMs
        };
    }

    // Convert native MftEntry to managed
    public static FileEntry ToManaged(this uniffi.nexus.MftEntry entry)
    {
        return new FileEntry
        {
            Name = entry.Name,
            Size = entry.Size,
            IsDirectory = entry.IsDirectory,
            Created = entry.Created.HasValue
                ? DateTime.FromFileTimeUtc((long)entry.Created.Value)
                : null,
            Modified = entry.Modified.HasValue
                ? DateTime.FromFileTimeUtc((long)entry.Modified.Value)
                : null,
            MftRecordNumber = entry.MftRecordNumber,
            ParentRecordNumber = entry.ParentRecordNumber
        };
    }

    // Convert managed SearchOptions to native
    public static uniffi.nexus.SearchOptions ToNative(this SearchOptions options)
    {
        return new uniffi.nexus.SearchOptions
        {
            CaseSensitive = options.CaseSensitive,
            UseRegex = options.UseRegex,
            MaxResults = options.MaxResults
        };
    }

    // Convert native SearchResult to managed
    public static SearchResult ToManaged(this uniffi.nexus.SearchResult result)
    {
        return new SearchResult
        {
            Path = result.Path,
            Name = result.Name,
            Score = result.Score
        };
    }
}
```

**Dependencies:** [3.1.1](#task-311-create-nexusnative-wrapper-class)

**Next Task:** [3.2.1](#task-321-create-core-models)

---

### 3.2 Core Domain Models

#### Task 3.2.1: Create Core Models

**Status:** `[ ]` Not Started

**Description:**
Create domain models in Winhance.Core for Storage Intelligence.

**Acceptance Criteria:**
- [ ] All models follow Winhance naming conventions
- [ ] Models are immutable where appropriate
- [ ] Proper XML documentation

**Files to Create:**
```
src/Winhance.Core/Features/Storage/Models/DriveInfo.cs
src/Winhance.Core/Features/Storage/Models/FileEntry.cs
src/Winhance.Core/Features/Storage/Models/ScanOptions.cs
src/Winhance.Core/Features/Storage/Models/ScanResult.cs
src/Winhance.Core/Features/Storage/Models/SearchOptions.cs
src/Winhance.Core/Features/Storage/Models/SearchResult.cs
src/Winhance.Core/Features/Storage/Models/SpaceRecommendation.cs
src/Winhance.Core/Features/Storage/Models/AIModel.cs
```

**Implementation:**
```csharp
// FileEntry.cs
namespace Winhance.Core.Features.Storage.Models;

/// <summary>
/// Represents a file or directory entry from the MFT scan.
/// </summary>
public sealed record FileEntry
{
    /// <summary>File or directory name.</summary>
    public required string Name { get; init; }

    /// <summary>Size in bytes.</summary>
    public required ulong Size { get; init; }

    /// <summary>Whether this entry is a directory.</summary>
    public required bool IsDirectory { get; init; }

    /// <summary>Creation time (UTC).</summary>
    public DateTime? Created { get; init; }

    /// <summary>Last modification time (UTC).</summary>
    public DateTime? Modified { get; init; }

    /// <summary>MFT record number.</summary>
    public ulong MftRecordNumber { get; init; }

    /// <summary>Parent directory MFT record number.</summary>
    public ulong ParentRecordNumber { get; init; }

    /// <summary>Full path (reconstructed from MFT).</summary>
    public string? FullPath { get; init; }

    /// <summary>File category based on extension.</summary>
    public FileCategory Category { get; init; }
}

// ScanOptions.cs
namespace Winhance.Core.Features.Storage.Models;

/// <summary>
/// Options for MFT scanning.
/// </summary>
public sealed record ScanOptions
{
    /// <summary>Include hidden files.</summary>
    public bool IncludeHidden { get; init; } = true;

    /// <summary>Include system files.</summary>
    public bool IncludeSystem { get; init; } = false;

    /// <summary>Maximum directory depth (0 = unlimited).</summary>
    public uint MaxDepth { get; init; } = 0;

    /// <summary>Default scan options.</summary>
    public static ScanOptions Default => new();
}

// SpaceRecommendation.cs
namespace Winhance.Core.Features.Storage.Models;

/// <summary>
/// A recommendation for recovering disk space.
/// </summary>
public sealed record SpaceRecommendation
{
    /// <summary>Recommendation category.</summary>
    public required RecommendationType Type { get; init; }

    /// <summary>Human-readable description.</summary>
    public required string Description { get; init; }

    /// <summary>Potential space to recover in bytes.</summary>
    public required ulong PotentialSavings { get; init; }

    /// <summary>Risk level of this action.</summary>
    public required RiskLevel Risk { get; init; }

    /// <summary>Files/folders affected.</summary>
    public IReadOnlyList<string> AffectedPaths { get; init; } = [];

    /// <summary>Whether this action is reversible.</summary>
    public bool IsReversible { get; init; }
}

public enum RecommendationType
{
    TempFiles,
    DuplicateFiles,
    LargeFiles,
    OldDownloads,
    EmptyFolders,
    CacheFiles,
    LogFiles,
    SystemCleanup,
    AIModels,
    RecycleBin
}

public enum RiskLevel
{
    Safe,
    Low,
    Medium,
    High
}
```

**Next Task:** [3.2.2](#task-322-create-core-interfaces)

---

#### Task 3.2.2: Create Core Interfaces

**Status:** `[ ]` Not Started

**Description:**
Define service interfaces in Winhance.Core.

**Acceptance Criteria:**
- [ ] Interfaces use `Task<OperationResult<T>>` pattern
- [ ] CancellationToken on all async methods
- [ ] Progress reporting where appropriate

**Files to Create:**
```
src/Winhance.Core/Features/Storage/Interfaces/IStorageScanner.cs
src/Winhance.Core/Features/Storage/Interfaces/IDeepScanner.cs
src/Winhance.Core/Features/Storage/Interfaces/IAIModelManager.cs
src/Winhance.Core/Features/Storage/Interfaces/INexusNative.cs
```

**Implementation:**
```csharp
// IStorageScanner.cs
namespace Winhance.Core.Features.Storage.Interfaces;

/// <summary>
/// Service for scanning drives and analyzing storage.
/// </summary>
public interface IStorageScanner
{
    /// <summary>
    /// Scan a drive and return file information.
    /// </summary>
    Task<OperationResult<ScanResult>> ScanDriveAsync(
        string driveLetter,
        ScanOptions? options = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get space usage breakdown by category.
    /// </summary>
    Task<OperationResult<SpaceBreakdown>> GetSpaceBreakdownAsync(
        string driveLetter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get space recovery recommendations.
    /// </summary>
    Task<OperationResult<IReadOnlyList<SpaceRecommendation>>> GetRecommendationsAsync(
        string driveLetter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for files matching a query.
    /// </summary>
    Task<OperationResult<IReadOnlyList<SearchResult>>> SearchAsync(
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default);
}

// IDeepScanner.cs
namespace Winhance.Core.Features.Storage.Interfaces;

/// <summary>
/// Service for deep forensic scanning.
/// </summary>
public interface IDeepScanner
{
    /// <summary>
    /// Scan for Alternate Data Streams.
    /// </summary>
    Task<OperationResult<IReadOnlyList<AdsEntry>>> ScanAdsAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get Volume Shadow Copy snapshots.
    /// </summary>
    Task<OperationResult<IReadOnlyList<VssSnapshot>>> GetVssSnapshotsAsync(
        string driveLetter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate file entropy.
    /// </summary>
    Task<OperationResult<EntropyResult>> CalculateEntropyAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find duplicate files.
    /// </summary>
    Task<OperationResult<IReadOnlyList<DuplicateGroup>>> FindDuplicatesAsync(
        string path,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

// IAIModelManager.cs
namespace Winhance.Core.Features.Storage.Interfaces;

/// <summary>
/// Service for managing AI/ML models.
/// </summary>
public interface IAIModelManager
{
    /// <summary>
    /// Discover AI models on the system.
    /// </summary>
    Task<OperationResult<IReadOnlyList<AIModel>>> DiscoverModelsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total space used by AI models.
    /// </summary>
    Task<OperationResult<ulong>> GetTotalModelSizeAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move models to a new location.
    /// </summary>
    Task<OperationResult<bool>> MoveModelsAsync(
        IEnumerable<string> modelPaths,
        string destinationFolder,
        IProgress<TransferProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create symlinks for moved models.
    /// </summary>
    Task<OperationResult<bool>> CreateSymlinksAsync(
        IEnumerable<(string Source, string Target)> links,
        CancellationToken cancellationToken = default);
}
```

**Dependencies:** [3.2.1](#task-321-create-core-models)

**Next Task:** [3.2.3](#task-323-create-core-enums)

---

#### Task 3.2.3: Create Core Enums

**Status:** `[ ]` Not Started

**Description:**
Define enumerations for file categories and scan states.

**Files to Create:**
```
src/Winhance.Core/Features/Storage/Enums/FileCategory.cs
src/Winhance.Core/Features/Storage/Enums/ScanDepth.cs
src/Winhance.Core/Features/Storage/Enums/EntropyClass.cs
```

**Implementation:**
```csharp
// FileCategory.cs
namespace Winhance.Core.Features.Storage.Enums;

/// <summary>
/// Categories for file classification.
/// </summary>
public enum FileCategory
{
    Unknown,
    Document,
    Image,
    Video,
    Audio,
    Archive,
    Code,
    Database,
    Executable,
    System,
    Temporary,
    Log,
    Configuration,
    AIModel,
    Font,
    Backup
}

// ScanDepth.cs
namespace Winhance.Core.Features.Storage.Enums;

/// <summary>
/// Depth levels for scanning.
/// </summary>
public enum ScanDepth
{
    /// <summary>Quick scan - MFT only.</summary>
    Quick,

    /// <summary>Standard scan - MFT + basic analysis.</summary>
    Standard,

    /// <summary>Deep scan - includes entropy, ADS, etc.</summary>
    Deep,

    /// <summary>Forensic scan - full analysis.</summary>
    Forensic
}

// EntropyClass.cs
namespace Winhance.Core.Features.Storage.Enums;

/// <summary>
/// Classification based on file entropy.
/// </summary>
public enum EntropyClass
{
    /// <summary>Low entropy (&lt;4.0) - text, structured data.</summary>
    Low,

    /// <summary>Medium entropy (4.0-7.0) - code, documents.</summary>
    Medium,

    /// <summary>High entropy (7.0-7.9) - compressed files.</summary>
    High,

    /// <summary>Very high entropy (&gt;7.9) - encrypted files.</summary>
    VeryHigh
}
```

**Dependencies:** [3.2.2](#task-322-create-core-interfaces)

**Next Task:** [3.3.1](#task-331-implement-storageintelligenceservice)

---

### 3.3 Infrastructure Services

#### Task 3.3.1: Implement StorageIntelligenceService

**Status:** `[ ]` Not Started

**Description:**
Create the main storage intelligence service.

**Acceptance Criteria:**
- [ ] Implements `IStorageScanner`
- [ ] Uses `NexusNativeWrapper` for Rust calls
- [ ] Proper error handling
- [ ] Progress reporting works

**Files to Create:**
```
src/Winhance.Infrastructure/Features/Storage/Services/StorageIntelligenceService.cs
```

**Implementation:**
```csharp
// StorageIntelligenceService.cs
namespace Winhance.Infrastructure.Features.Storage.Services;

public sealed class StorageIntelligenceService : IStorageScanner, IDisposable
{
    private readonly INexusNative _native;
    private readonly ILogger<StorageIntelligenceService> _logger;
    private readonly IFileCategorizer _categorizer;

    public StorageIntelligenceService(
        INexusNative native,
        ILogger<StorageIntelligenceService> logger,
        IFileCategorizer categorizer)
    {
        _native = native;
        _logger = logger;
        _categorizer = categorizer;
    }

    public async Task<OperationResult<ScanResult>> ScanDriveAsync(
        string driveLetter,
        ScanOptions? options = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scan of drive {Drive}", driveLetter);

        progress?.Report(new ScanProgress { Stage = "Starting MFT scan...", Percentage = 0 });

        var result = await _native.ScanDriveAsync(driveLetter, options, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Scan failed: {Error}", result.ErrorMessage);
            return result;
        }

        progress?.Report(new ScanProgress { Stage = "Categorizing files...", Percentage = 50 });

        // Categorize files
        foreach (var entry in result.Data!.Entries)
        {
            entry.Category = _categorizer.Categorize(entry.Name);
        }

        progress?.Report(new ScanProgress { Stage = "Complete", Percentage = 100 });

        _logger.LogInformation(
            "Scan complete: {Files} files, {Size} bytes",
            result.Data.FileCount,
            result.Data.TotalSize);

        return result;
    }

    public async Task<OperationResult<SpaceBreakdown>> GetSpaceBreakdownAsync(
        string driveLetter,
        CancellationToken cancellationToken = default)
    {
        var scanResult = await ScanDriveAsync(driveLetter, cancellationToken: cancellationToken);

        if (!scanResult.Success)
        {
            return OperationResult<SpaceBreakdown>.Failed(scanResult.ErrorMessage!);
        }

        var breakdown = new SpaceBreakdown
        {
            TotalSize = scanResult.Data!.TotalSize,
            Categories = scanResult.Data.Entries
                .GroupBy(e => e.Category)
                .ToDictionary(
                    g => g.Key,
                    g => new CategoryInfo
                    {
                        Category = g.Key,
                        FileCount = g.Count(),
                        TotalSize = (ulong)g.Sum(e => (long)e.Size)
                    })
        };

        return OperationResult<SpaceBreakdown>.Succeeded(breakdown);
    }

    public async Task<OperationResult<IReadOnlyList<SpaceRecommendation>>> GetRecommendationsAsync(
        string driveLetter,
        CancellationToken cancellationToken = default)
    {
        var recommendations = new List<SpaceRecommendation>();

        // Check temp folders
        var tempResult = await AnalyzeTempFoldersAsync(driveLetter, cancellationToken);
        if (tempResult.Success && tempResult.Data!.TotalSize > 0)
        {
            recommendations.Add(new SpaceRecommendation
            {
                Type = RecommendationType.TempFiles,
                Description = "Temporary files that can be safely deleted",
                PotentialSavings = tempResult.Data.TotalSize,
                Risk = RiskLevel.Safe,
                IsReversible = false,
                AffectedPaths = tempResult.Data.Paths
            });
        }

        // Check for large files
        var largeFilesResult = await FindLargeFilesAsync(driveLetter, cancellationToken);
        if (largeFilesResult.Success && largeFilesResult.Data!.Any())
        {
            recommendations.Add(new SpaceRecommendation
            {
                Type = RecommendationType.LargeFiles,
                Description = "Large files that might be candidates for archival",
                PotentialSavings = (ulong)largeFilesResult.Data.Sum(f => (long)f.Size),
                Risk = RiskLevel.Medium,
                IsReversible = true,
                AffectedPaths = largeFilesResult.Data.Select(f => f.FullPath!).ToList()
            });
        }

        // Check AI models
        var aiResult = await AnalyzeAIModelsAsync(driveLetter, cancellationToken);
        if (aiResult.Success && aiResult.Data!.TotalSize > 0)
        {
            recommendations.Add(new SpaceRecommendation
            {
                Type = RecommendationType.AIModels,
                Description = "AI/ML models that could be moved to another drive",
                PotentialSavings = aiResult.Data.TotalSize,
                Risk = RiskLevel.Low,
                IsReversible = true,
                AffectedPaths = aiResult.Data.Paths
            });
        }

        return OperationResult<IReadOnlyList<SpaceRecommendation>>.Succeeded(recommendations);
    }

    public async Task<OperationResult<IReadOnlyList<SearchResult>>> SearchAsync(
        string query,
        SearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await _native.SearchAsync(query, options, cancellationToken);
    }

    public void Dispose()
    {
        if (_native is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    // Private helper methods...
    private async Task<OperationResult<TempAnalysis>> AnalyzeTempFoldersAsync(
        string driveLetter, CancellationToken ct) { /* ... */ }

    private async Task<OperationResult<IReadOnlyList<FileEntry>>> FindLargeFilesAsync(
        string driveLetter, CancellationToken ct) { /* ... */ }

    private async Task<OperationResult<AIModelAnalysis>> AnalyzeAIModelsAsync(
        string driveLetter, CancellationToken ct) { /* ... */ }
}
```

**Dependencies:** [3.2.3](#task-323-create-core-enums)

**Next Task:** [3.3.2](#task-332-implement-deepscanservice)

---

#### Task 3.3.2: Implement DeepScanService

**Status:** `[ ]` Not Started

**Description:**
Create the deep forensic scanning service.

**Acceptance Criteria:**
- [ ] Implements `IDeepScanner`
- [ ] ADS scanning works
- [ ] VSS access works
- [ ] Entropy calculation works
- [ ] Duplicate detection works

**Files to Create:**
```
src/Winhance.Infrastructure/Features/Storage/Services/DeepScanService.cs
```

**Dependencies:** [3.3.1](#task-331-implement-storageintelligenceservice)

**Next Task:** [3.3.3](#task-333-implement-aimodelmanagerservice)

---

#### Task 3.3.3: Implement AIModelManagerService

**Status:** `[ ]` Not Started

**Description:**
Create the AI model management service.

**Acceptance Criteria:**
- [ ] Implements `IAIModelManager`
- [ ] Discovers models from known locations
- [ ] Calculates model sizes
- [ ] Move and symlink operations work

**Files to Create:**
```
src/Winhance.Infrastructure/Features/Storage/Services/AIModelManagerService.cs
```

**Known AI Model Locations:**
```
%USERPROFILE%\.cache\huggingface\
%USERPROFILE%\.ollama\models\
%USERPROFILE%\.cache\torch\
%USERPROFILE%\AppData\Local\lm-studio\
%USERPROFILE%\.cache\whisper\
%USERPROFILE%\.cache\llama.cpp\
```

**Dependencies:** [3.3.2](#task-332-implement-deepscanservice)

**Next Task:** [3.3.4](#task-334-implement-filecategorizer)

---

#### Task 3.3.4: Implement FileCategorizer

**Status:** `[ ]` Not Started

**Description:**
Create file categorization service based on extensions.

**Acceptance Criteria:**
- [ ] Categorizes by extension
- [ ] Handles unknown extensions
- [ ] Fast lookup (dictionary-based)

**Files to Create:**
```
src/Winhance.Infrastructure/Features/Storage/Services/FileCategorizer.cs
```

**Implementation:**
```csharp
// FileCategorizer.cs
namespace Winhance.Infrastructure.Features.Storage.Services;

public sealed class FileCategorizer : IFileCategorizer
{
    private static readonly Dictionary<string, FileCategory> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        [".doc"] = FileCategory.Document,
        [".docx"] = FileCategory.Document,
        [".pdf"] = FileCategory.Document,
        [".txt"] = FileCategory.Document,
        [".rtf"] = FileCategory.Document,
        [".odt"] = FileCategory.Document,
        [".xls"] = FileCategory.Document,
        [".xlsx"] = FileCategory.Document,
        [".ppt"] = FileCategory.Document,
        [".pptx"] = FileCategory.Document,

        // Images
        [".jpg"] = FileCategory.Image,
        [".jpeg"] = FileCategory.Image,
        [".png"] = FileCategory.Image,
        [".gif"] = FileCategory.Image,
        [".bmp"] = FileCategory.Image,
        [".svg"] = FileCategory.Image,
        [".webp"] = FileCategory.Image,
        [".ico"] = FileCategory.Image,

        // Video
        [".mp4"] = FileCategory.Video,
        [".avi"] = FileCategory.Video,
        [".mkv"] = FileCategory.Video,
        [".mov"] = FileCategory.Video,
        [".wmv"] = FileCategory.Video,
        [".webm"] = FileCategory.Video,

        // Audio
        [".mp3"] = FileCategory.Audio,
        [".wav"] = FileCategory.Audio,
        [".flac"] = FileCategory.Audio,
        [".aac"] = FileCategory.Audio,
        [".ogg"] = FileCategory.Audio,
        [".m4a"] = FileCategory.Audio,

        // Archives
        [".zip"] = FileCategory.Archive,
        [".rar"] = FileCategory.Archive,
        [".7z"] = FileCategory.Archive,
        [".tar"] = FileCategory.Archive,
        [".gz"] = FileCategory.Archive,

        // Code
        [".cs"] = FileCategory.Code,
        [".rs"] = FileCategory.Code,
        [".py"] = FileCategory.Code,
        [".js"] = FileCategory.Code,
        [".ts"] = FileCategory.Code,
        [".java"] = FileCategory.Code,
        [".cpp"] = FileCategory.Code,
        [".c"] = FileCategory.Code,
        [".h"] = FileCategory.Code,
        [".go"] = FileCategory.Code,

        // AI Models
        [".gguf"] = FileCategory.AIModel,
        [".ggml"] = FileCategory.AIModel,
        [".bin"] = FileCategory.AIModel,
        [".safetensors"] = FileCategory.AIModel,
        [".pt"] = FileCategory.AIModel,
        [".pth"] = FileCategory.AIModel,
        [".onnx"] = FileCategory.AIModel,

        // System
        [".dll"] = FileCategory.System,
        [".sys"] = FileCategory.System,
        [".drv"] = FileCategory.System,

        // Executables
        [".exe"] = FileCategory.Executable,
        [".msi"] = FileCategory.Executable,
        [".bat"] = FileCategory.Executable,
        [".cmd"] = FileCategory.Executable,
        [".ps1"] = FileCategory.Executable,

        // Temporary
        [".tmp"] = FileCategory.Temporary,
        [".temp"] = FileCategory.Temporary,
        [".bak"] = FileCategory.Temporary,

        // Logs
        [".log"] = FileCategory.Log,

        // Configuration
        [".json"] = FileCategory.Configuration,
        [".xml"] = FileCategory.Configuration,
        [".yaml"] = FileCategory.Configuration,
        [".yml"] = FileCategory.Configuration,
        [".toml"] = FileCategory.Configuration,
        [".ini"] = FileCategory.Configuration,
        [".config"] = FileCategory.Configuration,
    };

    public FileCategory Categorize(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
        {
            return FileCategory.Unknown;
        }

        return ExtensionMap.TryGetValue(extension, out var category)
            ? category
            : FileCategory.Unknown;
    }
}
```

**Dependencies:** [3.3.3](#task-333-implement-aimodelmanagerservice)

**Next Task:** [3.4.1](#task-341-register-services-in-di)

---

### 3.4 Dependency Injection

#### Task 3.4.1: Register Services in DI

**Status:** `[ ]` Not Started

**Description:**
Register all storage services in the Winhance DI container.

**Acceptance Criteria:**
- [ ] All services registered
- [ ] Correct lifetimes (Singleton/Scoped/Transient)
- [ ] Follows existing Winhance patterns

**Files to Modify:**
```
src/Winhance.WPF/App.xaml.cs
```

**Implementation:**
```csharp
// In ConfigureServices method
services.AddSingleton<INexusNative, NexusNativeWrapper>();
services.AddSingleton<IFileCategorizer, FileCategorizer>();
services.AddSingleton<IStorageScanner, StorageIntelligenceService>();
services.AddSingleton<IDeepScanner, DeepScanService>();
services.AddSingleton<IAIModelManager, AIModelManagerService>();
```

**Dependencies:** [3.3.4](#task-334-implement-filecategorizer)

---

## Phase Completion Checklist

- [ ] All 3.1.x tasks complete (Native Wrapper)
- [ ] All 3.2.x tasks complete (Core Models)
- [ ] All 3.3.x tasks complete (Infrastructure Services)
- [ ] All 3.4.x tasks complete (Dependency Injection)
- [ ] `dotnet build` succeeds
- [ ] All services can be resolved from DI
- [ ] Basic integration tests pass

---

**[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 2](PHASE_2_RUST_BACKEND.md) | **Next:** [Phase 4](PHASE_4_UI_THEMING.md)
