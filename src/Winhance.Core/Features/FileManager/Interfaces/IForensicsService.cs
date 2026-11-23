using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces;

/// <summary>What an NTFS alternate data stream most likely is.</summary>
public enum StreamKind
{
    /// <summary>Zone.Identifier: Windows' "downloaded from the internet" marker (Mark of the Web).</summary>
    MarkOfTheWeb,

    /// <summary>A stream written by Windows or a well-known app (thumbnails, Dropbox, document summary...).</summary>
    KnownMetadata,

    /// <summary>Unrecognized data hidden next to a file.</summary>
    HiddenData,

    /// <summary>A Windows program hidden inside a stream: a classic malware hiding place.</summary>
    HiddenExecutable,
}

/// <summary>A named stream attached to a file or folder (<c>file.txt:name</c>).</summary>
public sealed record AlternateStream(string FilePath, string StreamName, long Size, StreamKind Kind, string Description)
{
    public string StreamPath => FilePath + ":" + StreamName;
}

/// <summary>Parsed Zone.Identifier contents.</summary>
public sealed record ZoneIdentifier(int? ZoneId, string? HostUrl, string? ReferrerUrl)
{
    public string ZoneName => ZoneId switch
    {
        0 => "this computer",
        1 => "local intranet",
        2 => "trusted sites",
        3 => "the internet",
        4 => "restricted sites",
        _ => "an unknown zone",
    };
}

public sealed record StreamScanResult(
    IReadOnlyList<AlternateStream> Streams,
    int ItemsScanned,
    int ItemsSkipped,
    bool VolumeSupportsStreams,
    string? Error = null);

public sealed record ForensicsActionResult(int Succeeded, IReadOnlyList<string> Errors)
{
    public int Failed => Errors.Count;
}

/// <summary>Verdict of a content/entropy inspection, most worrying last.</summary>
public enum FileVerdict
{
    Normal,
    Compressed,
    ExtensionMismatch,
    LikelyEncrypted,
    DisguisedExecutable,
}

public sealed record FileInspection(
    string Path,
    long Size,
    double Entropy,
    string Extension,
    string? DetectedType,
    FileVerdict Verdict,
    string Reason);

public sealed record EntropyScanOptions
{
    public bool Recursive { get; init; } = true;

    /// <summary>Bytes sampled per file (start, middle and end for larger files).</summary>
    public int SampleBytes { get; init; } = 1024 * 1024;

    /// <summary>Bits per byte above which data is treated as compressed or encrypted (max 8).</summary>
    public double HighEntropyThreshold { get; init; } = 7.5;

    /// <summary>Only return files whose verdict is not <see cref="FileVerdict.Normal"/> or <see cref="FileVerdict.Compressed"/>.</summary>
    public bool OnlySuspicious { get; init; } = true;

    public int MaxFiles { get; init; } = 200_000;
}

public sealed record EntropyScanResult(IReadOnlyList<FileInspection> Files, int FilesScanned, int FilesSkipped, string? Error = null);

/// <summary>A Volume Shadow Copy (System Restore / Previous Versions snapshot).</summary>
public sealed record ShadowCopyInfo(string Id, string DeviceObject, string VolumeRoot, DateTime Created);

public enum VersionState
{
    SameAsCurrent,
    Different,
    DeletedNow,
}

/// <summary>A file or folder as it was in one shadow copy.</summary>
public sealed record PreviousVersion(
    ShadowCopyInfo Snapshot,
    string OriginalPath,
    string SnapshotPath,
    bool IsFolder,
    long Size,
    DateTime Modified,
    VersionState State);

/// <summary>Protection status of a drive: whether Windows is keeping snapshots of it.</summary>
public sealed record VolumeProtection(string VolumeRoot, int SnapshotCount, DateTime? Newest);

/// <summary>An item in the current user's Recycle Bin (parsed from its <c>$I</c> record).</summary>
public sealed record RecycleBinItem(
    string OriginalPath,
    DateTime Deleted,
    long Size,
    bool IsFolder,
    string DataPath,
    string InfoPath)
{
    public string Name => System.IO.Path.GetFileName(OriginalPath.TrimEnd('\\'));
}

public sealed record RestoreResult(bool Success, string? RestoredPath, string? Error);

/// <summary>
/// File forensics: hidden NTFS streams, content/entropy checks (encrypted or disguised files), previous versions
/// from Volume Shadow Copies, and Recycle Bin recovery. Everything is read-only except the explicit remove/restore
/// methods, which never overwrite an existing file unless told to.
/// </summary>
public interface IForensicsService
{
    Task<StreamScanResult> ScanStreamsAsync(string path, bool recursive, IProgress<string>? progress = null, CancellationToken ct = default);

    /// <summary>First bytes of a stream as text (non-printable characters shown as dots).</summary>
    Task<string> ReadStreamPreviewAsync(AlternateStream stream, int maxBytes = 4096, CancellationToken ct = default);

    ZoneIdentifier? ReadZoneIdentifier(string filePath);

    /// <summary>Delete the given streams (the file itself is untouched). Removing Zone.Identifier "unblocks" a download.</summary>
    Task<ForensicsActionResult> RemoveStreamsAsync(IEnumerable<AlternateStream> streams, CancellationToken ct = default);

    Task<FileInspection> InspectFileAsync(string path, EntropyScanOptions? options = null, CancellationToken ct = default);

    Task<EntropyScanResult> ScanEntropyAsync(string path, EntropyScanOptions options, IProgress<string>? progress = null, CancellationToken ct = default);

    Task<IReadOnlyList<ShadowCopyInfo>> ListShadowCopiesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<VolumeProtection>> GetVolumeProtectionAsync(CancellationToken ct = default);

    Task<IReadOnlyList<PreviousVersion>> GetPreviousVersionsAsync(string path, CancellationToken ct = default);

    /// <summary>Copy a previous version to <paramref name="destinationPath"/>. Folders are copied recursively.</summary>
    Task<RestoreResult> RestorePreviousVersionAsync(PreviousVersion version, string destinationPath, bool overwrite = false, CancellationToken ct = default);

    Task<IReadOnlyList<RecycleBinItem>> ListRecycleBinAsync(CancellationToken ct = default);

    /// <summary>Put an item back at its original path (or <paramref name="destinationPath"/>). Never overwrites.</summary>
    Task<RestoreResult> RestoreFromRecycleBinAsync(RecycleBinItem item, string? destinationPath = null, CancellationToken ct = default);
}
