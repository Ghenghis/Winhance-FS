using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces;

/// <summary>
/// Instant file-name search over whole drives and folders, powered by the Rust engine (nexus_core.dll).
/// NTFS drives are read straight from the Master File Table (a few seconds for millions of files, needs
/// Administrator rights); folders and other drives are scanned. The index is saved to disk and reloaded at
/// start-up, so searching works immediately.
/// </summary>
public interface INexusIndexerService
{
    /// <summary>Gets a value indicating whether the native engine was loaded.</summary>
    bool IsAvailable { get; }

    /// <summary>Gets why the engine is unavailable, or null.</summary>
    string? UnavailableReason { get; }

    /// <summary>Gets the engine version, or null when unavailable.</summary>
    string? EngineVersion { get; }

    /// <summary>Gets a value indicating whether an index is loaded and can be searched.</summary>
    bool HasIndex { get; }

    /// <summary>Gets a value indicating whether an index is being built.</summary>
    bool IsBuilding { get; }

    /// <summary>Gets the loaded index, or null.</summary>
    NexusIndexInfo? Current { get; }

    /// <summary>Gets where the index is saved.</summary>
    string IndexFilePath { get; }

    /// <summary>Raised when an index is built or loaded.</summary>
    event EventHandler? IndexChanged;

    /// <summary>Loads the saved index, if one exists. Returns null when there is none or it cannot be read.</summary>
    Task<NexusIndexInfo?> LoadSavedIndexAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a new index of the given drives (<c>C:\</c>) or folders and saves it. The previous index stays
    /// searchable until the new one is complete. <paramref name="progress"/> receives the number of items found
    /// so far. Throws <see cref="OperationCanceledException"/> when cancelled.
    /// </summary>
    Task<NexusIndexInfo> BuildIndexAsync(IReadOnlyList<string> roots, IProgress<long>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches file and folder names. All words must appear in the name; <c>"quoted text"</c> is one term;
    /// <c>*</c> and <c>?</c> match the whole name; a term with <c>\</c> matches the full path; <c>!word</c>
    /// excludes; <c>ext:pdf;docx</c>, <c>file:</c> and <c>folder:</c> filter. Sorted by
    /// <see cref="NexusSort.Size"/> or <see cref="NexusSort.Modified"/>, an empty query matches everything.
    /// </summary>
    Task<NexusSearchResult> SearchAsync(string query, int limit = 200, CancellationToken cancellationToken = default, NexusSort sort = NexusSort.Relevance);
}

/// <summary>Order of search results.</summary>
public enum NexusSort
{
    /// <summary>Best name match first.</summary>
    Relevance,

    /// <summary>Largest first.</summary>
    Size,

    /// <summary>Most recently modified first.</summary>
    Modified,
}

/// <summary>A loaded index.</summary>
public sealed record NexusIndexInfo(
    long Entries,
    long Files,
    long Folders,
    DateTime BuiltUtc,
    long BuildMilliseconds,
    IReadOnlyList<NexusRootInfo> Roots);

/// <summary>One indexed drive or folder.</summary>
/// <param name="Method">"mft" (read from the drive's file table) or "scan" (folder walk).</param>
/// <param name="Note">Why the faster method could not be used, if it could not.</param>
public sealed record NexusRootInfo(string Path, string Method, long Entries, string? Note);

/// <summary>Search results; <see cref="Total"/> counts every match, <see cref="Results"/> the best ones.</summary>
public sealed record NexusSearchResult(long Total, long Milliseconds, IReadOnlyList<NexusFileEntry> Results);

/// <summary>A file or folder found by the search engine.</summary>
public sealed class NexusFileEntry
{
    public string Path { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Folder { get; init; } = string.Empty;

    public bool IsDirectory { get; init; }

    public long Size { get; init; }

    public DateTime? Modified { get; init; }

    /// <summary>Gets a value indicating whether the item was deleted or moved after the index was built.</summary>
    public bool Missing { get; init; }

    public string? Extension
    {
        get
        {
            if (IsDirectory)
            {
                return null;
            }

            var dot = Name.LastIndexOf('.');
            return dot > 0 && dot < Name.Length - 1 ? Name[(dot + 1)..].ToLowerInvariant() : null;
        }
    }
}
