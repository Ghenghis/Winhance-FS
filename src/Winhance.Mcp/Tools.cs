using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Mcp;

/// <summary>Shared JSON output and argument checks for the tools.</summary>
internal static class Out
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // keep paths readable
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Json(object value) => JsonSerializer.Serialize(value, Options);

    public static string Error(string message, string? hint = null) => Json(new { error = message, hint });

    public static string Size(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.##} {units[unit]}";
    }

    /// <summary>Full path of an existing file or folder, or null.</summary>
    public static string? ExistingPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            var full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')));
            return File.Exists(full) || Directory.Exists(full) ? full : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}

/// <summary>Loads the app's saved search index on first use and reloads it when the app saves a newer one.</summary>
public sealed class SearchIndexHolder(INexusIndexerService engine)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private DateTime _loadedFileTime;

    public INexusIndexerService Engine => engine;

    public async Task<bool> EnsureAsync(CancellationToken ct)
    {
        if (!engine.IsAvailable)
        {
            return false;
        }

        var file = engine.IndexFilePath;
        var fileTime = File.Exists(file) ? File.GetLastWriteTimeUtc(file) : DateTime.MinValue;
        if (engine.HasIndex && (fileTime == DateTime.MinValue || fileTime <= _loadedFileTime))
        {
            return true;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (fileTime > _loadedFileTime && await engine.LoadSavedIndexAsync(ct) != null)
            {
                _loadedFileTime = fileTime;
            }

            return engine.HasIndex;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void MarkBuilt() => _loadedFileTime = File.Exists(engine.IndexFilePath) ? File.GetLastWriteTimeUtc(engine.IndexFilePath) : DateTime.UtcNow;

    public string NoIndex() => engine.IsAvailable
        ? Out.Error(
            "No search index yet.",
            "Open Winhance-FS > File Manager > Search and click Build Index (whole NTFS drives take seconds as Administrator), or call build_search_index.")
        : Out.Error(engine.UnavailableReason ?? "The search engine (nexus_core.dll) is not available.");
}

[McpServerToolType]
public sealed class SearchTools(SearchIndexHolder index)
{
    private static object Entry(NexusFileEntry e) => new
    {
        path = e.Path,
        type = e.IsDirectory ? "folder" : "file",
        size = e.IsDirectory ? null : Out.Size(e.Size),
        bytes = e.IsDirectory ? (long?)null : e.Size,
        modified = e.Modified?.ToString("yyyy-MM-dd HH:mm"),
        missing = e.Missing ? true : (bool?)null,
    };

    [McpServerTool(Name = "search_files", Title = "Search files by name", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Finds files and folders by name on every drive or folder in the Winhance-FS index, in milliseconds. " +
                 "Query rules (case-insensitive): all words must appear in the name ('tax 2024'); \"quoted text\" is one term; " +
                 "* and ? match the whole name ('*.iso', 'IMG_????.jpg'); a term containing \\ matches the full path " +
                 "('projects\\ readme', 'C:\\Users\\Me\\Documents\\ report'); !word excludes; ext:pdf;docx keeps those types; " +
                 "file: or folder: keeps only files or folders. Results include size and modified date.")]
    public async Task<string> SearchFiles(
        [Description("What to look for, using the rules above. May be empty when sort is 'size' or 'modified'.")] string query,
        [Description("'relevance' (best name match, default), 'size' (largest first) or 'modified' (newest first).")] string sort = "relevance",
        [Description("Maximum results to return, 1-500 (default 50).")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (!await index.EnsureAsync(cancellationToken))
        {
            return index.NoIndex();
        }

        var order = sort?.Trim().ToLowerInvariant() switch
        {
            "size" or "largest" => NexusSort.Size,
            "modified" or "newest" or "date" => NexusSort.Modified,
            _ => NexusSort.Relevance,
        };
        if (string.IsNullOrWhiteSpace(query) && order == NexusSort.Relevance)
        {
            return Out.Error("Give a query, or sort by 'size' or 'modified' to list everything.");
        }

        var result = await index.Engine.SearchAsync(query ?? string.Empty, Math.Clamp(limit, 1, 500), cancellationToken, order);
        return Out.Json(new
        {
            totalMatches = result.Total,
            shown = result.Results.Count,
            milliseconds = result.Milliseconds,
            results = result.Results.Select(Entry),
        });
    }

    [McpServerTool(Name = "largest_files", Title = "Largest files", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the largest files on all indexed drives, or under one folder, to see what uses disk space. " +
                 "Optionally only some file types. Uses the search index, so it answers in milliseconds.")]
    public async Task<string> LargestFiles(
        [Description("Folder to look in, e.g. C:\\Users\\Me\\Downloads. Empty = everything indexed.")] string? folder = null,
        [Description("Only these extensions, e.g. 'mp4;mkv' or 'iso'. Empty = all types.")] string? extensions = null,
        [Description("How many files to return, 1-200 (default 25).")] int count = 25,
        CancellationToken cancellationToken = default)
    {
        if (!await index.EnsureAsync(cancellationToken))
        {
            return index.NoIndex();
        }

        var query = "file:";
        if (!string.IsNullOrWhiteSpace(folder))
        {
            var full = Out.ExistingPath(folder);
            if (full == null || !Directory.Exists(full))
            {
                return Out.Error($"Folder not found: {folder}");
            }

            var covered = index.Engine.Current!.Roots.Any(r =>
                full.StartsWith(r.Path, StringComparison.OrdinalIgnoreCase) &&
                (full.Length == r.Path.Length || r.Path.EndsWith('\\') || full[r.Path.Length] == '\\'));
            if (!covered)
            {
                return Out.Error(
                    $"{full} is not in the search index.",
                    $"Indexed: {string.Join(", ", index.Engine.Current.Roots.Select(r => r.Path))}. Add it in Winhance-FS > Search, or call build_search_index.");
            }

            query += $" \"{full.TrimEnd('\\')}\\\"";
        }

        if (!string.IsNullOrWhiteSpace(extensions))
        {
            query += " ext:" + extensions.Replace(" ", string.Empty).Replace(',', ';');
        }

        var result = await index.Engine.SearchAsync(query, Math.Clamp(count, 1, 200), cancellationToken, NexusSort.Size);
        return Out.Json(new
        {
            filesMatched = result.Total,
            totalOfShown = Out.Size(result.Results.Sum(r => r.Size)),
            files = result.Results.Select(Entry),
        });
    }

    [McpServerTool(Name = "search_index_status", Title = "Search index status", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Shows which drives and folders the search index covers, how many items it holds and when it was built.")]
    public async Task<string> Status(CancellationToken cancellationToken = default)
    {
        if (!await index.EnsureAsync(cancellationToken))
        {
            return index.NoIndex();
        }

        var info = index.Engine.Current!;
        return Out.Json(new
        {
            files = info.Files,
            folders = info.Folders,
            builtUtc = info.BuiltUtc.ToString("u"),
            buildSeconds = info.BuildMilliseconds / 1000.0,
            roots = info.Roots.Select(r => new { r.Path, method = r.Method == "mft" ? "NTFS file table" : "folder scan", items = r.Entries, note = r.Note }),
            indexFile = index.Engine.IndexFilePath,
            engine = index.Engine.EngineVersion,
        });
    }

    [McpServerTool(Name = "build_search_index", Title = "Build the search index", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Builds (or rebuilds) the search index for drives like 'C:\\' or folders, and saves it for the Winhance-FS app too. " +
                 "Reads names only; no file is changed. Whole NTFS drives are read from the file table in seconds when this server " +
                 "runs as Administrator; otherwise folders are scanned, which can take minutes for a whole drive.")]
    public async Task<string> Build(
        [Description("Drives and folders to index, e.g. [\"C:\\\\\", \"D:\\\\Projects\"]. Replaces the previous index.")] string[] roots,
        CancellationToken cancellationToken = default)
    {
        if (!index.Engine.IsAvailable)
        {
            return index.NoIndex();
        }

        if (roots == null || roots.Length == 0)
        {
            return Out.Error("Give at least one drive or folder.");
        }

        try
        {
            var info = await index.Engine.BuildIndexAsync(roots, null, cancellationToken);
            index.MarkBuilt();
            return Out.Json(new
            {
                indexed = info.Files + info.Folders,
                files = info.Files,
                folders = info.Folders,
                seconds = info.BuildMilliseconds / 1000.0,
                roots = info.Roots.Select(r => new { r.Path, method = r.Method == "mft" ? "NTFS file table" : "folder scan", items = r.Entries, note = r.Note }),
            });
        }
        catch (OperationCanceledException)
        {
            return Out.Error("Cancelled; the previous index is unchanged.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Out.Error(ex.Message);
        }
    }
}

[McpServerToolType]
public sealed class InspectionTools(IDuplicateFinderService duplicates, IForensicsService forensics)
{
    [McpServerTool(Name = "find_duplicates", Title = "Find duplicate files", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Finds files with identical content (size, quick hash, then SHA-256) in one or more folders and reports the space " +
                 "they waste, largest sets first. Junctions and links are not followed and hard links are not counted. Read-only: " +
                 "to remove copies, use the Duplicates tab in Winhance-FS, which re-checks every file and uses the Recycle Bin.")]
    public async Task<string> FindDuplicates(
        [Description("Folders to compare, e.g. [\"C:\\\\Users\\\\Me\\\\Pictures\", \"D:\\\\Backup\"].")] string[] folders,
        [Description("Ignore files smaller than this many megabytes (default 1).")] double minSizeMb = 1,
        [Description("Maximum duplicate sets to list (default 20).")] int maxSets = 20,
        CancellationToken cancellationToken = default)
    {
        var paths = (folders ?? Array.Empty<string>()).Select(Out.ExistingPath).ToList();
        if (paths.Count == 0 || paths.Any(p => p == null || !Directory.Exists(p)))
        {
            return Out.Error("Give one or more existing folders.");
        }

        var options = new DuplicateScanOptions { MinFileSize = Math.Max(1, (long)(minSizeMb * 1024 * 1024)) };
        var result = await duplicates.ScanForDuplicatesAsync(paths!, options, null, cancellationToken);
        return Out.Json(new
        {
            filesScanned = result.TotalFilesScanned,
            duplicateSets = result.Groups.Count,
            extraCopies = result.DuplicateCount,
            wastedSpace = Out.Size(result.WastedBytes),
            seconds = Math.Round(result.ScanDuration.TotalSeconds, 1),
            sets = result.Groups.Take(Math.Clamp(maxSets, 1, 200)).Select(g => new
            {
                size = Out.Size(g.FileSize),
                wasted = Out.Size(g.WastedBytes),
                sha256 = g.Hash,
                files = g.Files.Select(f => new { f.Path, modified = f.ModifiedTime.ToString("yyyy-MM-dd HH:mm") }),
            }),
        });
    }

    [McpServerTool(Name = "hidden_streams", Title = "Find hidden NTFS streams", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists NTFS alternate data streams, which Explorer never shows: download markers (Zone.Identifier, with the website " +
                 "a file came from), metadata from Windows and sync apps, unrecognized hidden data, and Windows programs hidden " +
                 "inside a stream (a malware technique). Most worrying first.")]
    public async Task<string> HiddenStreams(
        [Description("File or folder to check.")] string path,
        [Description("Include subfolders (default true).")] bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var full = Out.ExistingPath(path);
        if (full == null)
        {
            return Out.Error($"Not found: {path}");
        }

        var result = await forensics.ScanStreamsAsync(full, recursive, null, cancellationToken);
        if (!result.VolumeSupportsStreams)
        {
            return Out.Error(result.Error ?? "This drive does not support NTFS streams.");
        }

        return Out.Json(new
        {
            itemsScanned = result.ItemsScanned,
            streams = result.Streams.Count,
            hiddenPrograms = result.Streams.Count(s => s.Kind == StreamKind.HiddenExecutable),
            hiddenData = result.Streams.Count(s => s.Kind == StreamKind.HiddenData),
            downloadMarkers = result.Streams.Count(s => s.Kind == StreamKind.MarkOfTheWeb),
            list = result.Streams.OrderByDescending(s => s.Kind).Take(300).Select(s => new
            {
                stream = s.StreamPath,
                kind = s.Kind switch
                {
                    StreamKind.HiddenExecutable => "hidden program",
                    StreamKind.HiddenData => "hidden data",
                    StreamKind.MarkOfTheWeb => "download marker",
                    _ => "metadata",
                },
                size = Out.Size(s.Size),
                s.Description,
            }),
            error = result.Error,
        });
    }

    [McpServerTool(Name = "check_file_contents", Title = "Check for disguised or encrypted files", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Reads each file's header and measures its entropy to flag programs disguised as documents (a .jpg that is really " +
                 "an .exe), files that look encrypted (random content with no valid header, or a known ransomware extension), " +
                 "and wrong extensions. Only suspicious files are listed.")]
    public async Task<string> CheckFileContents(
        [Description("File or folder to check.")] string path,
        [Description("Include subfolders (default true).")] bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var full = Out.ExistingPath(path);
        if (full == null)
        {
            return Out.Error($"Not found: {path}");
        }

        var result = await forensics.ScanEntropyAsync(full, new EntropyScanOptions { Recursive = recursive, OnlySuspicious = true }, null, cancellationToken);
        return Out.Json(new
        {
            filesScanned = result.FilesScanned,
            filesSkipped = result.FilesSkipped,
            suspicious = result.Files.Count,
            files = result.Files.Take(300).Select(f => new
            {
                f.Path,
                verdict = f.Verdict switch
                {
                    FileVerdict.DisguisedExecutable => "disguised program",
                    FileVerdict.LikelyEncrypted => "likely encrypted",
                    FileVerdict.ExtensionMismatch => "wrong extension",
                    _ => f.Verdict.ToString(),
                },
                f.Reason,
                actualType = f.DetectedType,
                entropy = Math.Round(f.Entropy, 2),
                size = Out.Size(f.Size),
            }),
            error = result.Error,
        });
    }

    [McpServerTool(Name = "previous_versions", Title = "Previous versions from Windows snapshots", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists earlier copies of a file or folder kept in Windows snapshots (Volume Shadow Copies from System Restore " +
                 "and backups), compared with the current file. To restore one, use the Forensics tab in Winhance-FS.")]
    public async Task<string> PreviousVersions(
        [Description("File or folder path. It may already be deleted.")] string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Out.Error("Give a path.");
        }

        try
        {
            var full = Path.GetFullPath(path.Trim().Trim('"'));
            var versions = await forensics.GetPreviousVersionsAsync(full, cancellationToken);
            if (versions.Count == 0)
            {
                var protection = await forensics.GetVolumeProtectionAsync(cancellationToken);
                return Out.Json(new
                {
                    versions = 0,
                    note = "No earlier versions found.",
                    snapshotsPerDrive = protection.Select(p => new { drive = p.VolumeRoot, snapshots = p.SnapshotCount, newest = p.Newest?.ToString("yyyy-MM-dd HH:mm") }),
                });
            }

            return Out.Json(new
            {
                versions = versions.Count,
                list = versions.Select(v => new
                {
                    snapshot = v.Snapshot.Created.ToString("yyyy-MM-dd HH:mm"),
                    v.SnapshotPath,
                    size = Out.Size(v.Size),
                    modified = v.Modified.ToString("yyyy-MM-dd HH:mm"),
                    state = v.State switch
                    {
                        VersionState.DeletedNow => "deleted now",
                        VersionState.SameAsCurrent => "same as current",
                        _ => "different from current",
                    },
                }),
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or ArgumentException)
        {
            return Out.Error(ex.Message, "Reading snapshots usually needs Administrator rights.");
        }
    }

    [McpServerTool(Name = "recycle_bin", Title = "List the Recycle Bin", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists deleted items in the current user's Recycle Bin on every drive, newest first, with original location, " +
                 "deletion date and size. To restore, use the Forensics tab in Winhance-FS.")]
    public async Task<string> RecycleBin(
        [Description("Only items whose original path contains this text (optional).")] string? filter = null,
        [Description("Maximum items (default 50).")] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var items = await forensics.ListRecycleBinAsync(cancellationToken);
        var matching = items
            .Where(i => string.IsNullOrWhiteSpace(filter) || i.OriginalPath.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.Deleted)
            .ToList();
        return Out.Json(new
        {
            total = items.Count,
            matching = matching.Count,
            totalSize = Out.Size(matching.Sum(i => i.Size)),
            items = matching.Take(Math.Clamp(limit, 1, 500)).Select(i => new
            {
                i.Name,
                i.OriginalPath,
                deleted = i.Deleted.ToString("yyyy-MM-dd HH:mm"),
                size = Out.Size(i.Size),
                type = i.IsFolder ? "folder" : "file",
            }),
        });
    }
}
