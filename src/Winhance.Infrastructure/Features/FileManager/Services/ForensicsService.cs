using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services;

/// <summary>
/// File forensics: alternate data streams (this file), content/entropy inspection, shadow-copy previous versions and
/// Recycle Bin recovery (the other partial files). No raw-disk access: everything goes through the file system.
/// </summary>
public sealed partial class ForensicsService : IForensicsService
{
    private static readonly HashSet<string> KnownStreams = new(StringComparer.OrdinalIgnoreCase)
    {
        "SmartScreen", "OECustomProperty", "encryptable", "favicon", "AFP_AfpInfo", "AFP_Resource", "ms-properties",
        "Win32App_1", "\u0005SummaryInformation", "\u0005DocumentSummaryInformation",
        "{4c8cc155-6c1e-11d1-8e41-00c04fb9386d}", "Zone.Identifier.bak",
    };

    private static readonly string[] KnownStreamPrefixes = { "com.dropbox.", "com.apple.", "OneDrive", "\u0005" };

    private readonly ILogService? _log;
    private readonly Func<CancellationToken, Task<IReadOnlyList<ShadowCopyInfo>>> _shadowSource;
    private readonly Func<IEnumerable<string>> _recycleBinFolders;

    public ForensicsService(ILogService? log)
        : this(log, null, null)
    {
    }

    /// <param name="log">Optional log.</param>
    /// <param name="shadowSource">Snapshot source (default: WMI Win32_ShadowCopy). Injected by tests.</param>
    /// <param name="recycleBinFolders">Recycle Bin folders to read (default: the current user's on every drive).</param>
    public ForensicsService(
        ILogService? log,
        Func<CancellationToken, Task<IReadOnlyList<ShadowCopyInfo>>>? shadowSource,
        Func<IEnumerable<string>>? recycleBinFolders)
    {
        _log = log;
        _shadowSource = shadowSource ?? QueryShadowCopiesAsync;
        _recycleBinFolders = recycleBinFolders ?? DefaultRecycleBinFolders;
    }

    public Task<StreamScanResult> ScanStreamsAsync(string path, bool recursive, IProgress<string>? progress = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Task.Run(() => ScanStreams(Path.GetFullPath(path), recursive, progress, ct), ct);
    }

    private StreamScanResult ScanStreams(string root, bool recursive, IProgress<string>? progress, CancellationToken ct)
    {
        if (!File.Exists(root) && !Directory.Exists(root))
        {
            return new StreamScanResult(Array.Empty<AlternateStream>(), 0, 0, true, $"'{root}' does not exist.");
        }

        if (!SupportsStreams(root))
        {
            return new StreamScanResult(Array.Empty<AlternateStream>(), 0, 0, false, "This drive's file system (not NTFS/ReFS) has no alternate data streams.");
        }

        var streams = new List<AlternateStream>();
        int scanned = 0, skipped = 0;
        foreach (var item in EnumerateItems(root, recursive))
        {
            ct.ThrowIfCancellationRequested();
            scanned++;
            if (scanned % 500 == 0)
            {
                progress?.Report($"Checked {scanned:N0} items, {streams.Count:N0} hidden stream(s) so far...");
            }

            try
            {
                foreach (var (name, size) in EnumerateStreams(item))
                {
                    streams.Add(Classify(item, name, size));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                skipped++;
            }
        }

        _log?.Log(LogLevel.Info, $"Stream scan of {root}: {scanned} items, {streams.Count} streams, {skipped} skipped");
        var ordered = streams.OrderByDescending(s => s.Kind).ThenBy(s => s.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
        return new StreamScanResult(ordered, scanned, skipped, true);
    }

    public async Task<string> ReadStreamPreviewAsync(AlternateStream stream, int maxBytes = 4096, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var buffer = new byte[Math.Clamp(maxBytes, 1, 1024 * 1024)];
        int read;
        await using (var fs = new FileStream(LongPath(stream.StreamPath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true))
        {
            read = await fs.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, ct);
        }

        return ToPrintable(buffer.AsSpan(0, read));
    }

    public ZoneIdentifier? ReadZoneIdentifier(string filePath)
    {
        try
        {
            var text = File.ReadAllText(LongPath(filePath + ":Zone.Identifier"));
            return ParseZoneIdentifier(text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static ZoneIdentifier ParseZoneIdentifier(string text)
    {
        int? zone = null;
        string? host = null, referrer = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            var eq = line.IndexOf('=', StringComparison.Ordinal);
            if (eq <= 0)
            {
                continue;
            }

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (key.Equals("ZoneId", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out var z))
            {
                zone = z;
            }
            else if (key.Equals("HostUrl", StringComparison.OrdinalIgnoreCase))
            {
                host = value;
            }
            else if (key.Equals("ReferrerUrl", StringComparison.OrdinalIgnoreCase))
            {
                referrer = value;
            }
        }

        return new ZoneIdentifier(zone, host, referrer);
    }

    public Task<ForensicsActionResult> RemoveStreamsAsync(IEnumerable<AlternateStream> streams, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(streams);
        var list = streams.ToList();
        return Task.Run(
            () =>
            {
                int removed = 0;
                var errors = new List<string>();
                foreach (var s in list)
                {
                    ct.ThrowIfCancellationRequested();

                    // Only ever a named stream: an empty or malformed name would address the file's main content.
                    if (string.IsNullOrWhiteSpace(s.StreamName) || s.StreamName.IndexOfAny(new[] { ':', '\\', '/' }) >= 0)
                    {
                        errors.Add($"{s.FilePath}: refused to remove a stream with an invalid name.");
                        continue;
                    }

                    try
                    {
                        if (!EnumerateStreams(s.FilePath).Any(x => x.Name.Equals(s.StreamName, StringComparison.OrdinalIgnoreCase)))
                        {
                            errors.Add($"{s.StreamPath}: already gone.");
                            continue;
                        }

                        File.Delete(LongPath(s.StreamPath));
                        if (EnumerateStreams(s.FilePath).Any(x => x.Name.Equals(s.StreamName, StringComparison.OrdinalIgnoreCase)))
                        {
                            errors.Add($"{s.StreamPath}: still present after removal.");
                            continue;
                        }

                        removed++;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        errors.Add($"{s.StreamPath}: {ex.Message}");
                    }
                }

                _log?.Log(LogLevel.Info, $"Removed {removed} alternate data stream(s), {errors.Count} not removed");
                return new ForensicsActionResult(removed, errors);
            },
            ct);
    }

    private AlternateStream Classify(string item, string name, long size)
    {
        if (name.Equals("Zone.Identifier", StringComparison.OrdinalIgnoreCase))
        {
            var zone = ReadZoneIdentifier(item);
            var from = zone?.HostUrl ?? zone?.ReferrerUrl;
            var desc = zone == null
                ? "Mark of the Web (download marker)"
                : $"Downloaded from {zone.ZoneName}" + (string.IsNullOrEmpty(from) ? string.Empty : $": {from}");
            return new AlternateStream(item, name, size, StreamKind.MarkOfTheWeb, desc);
        }

        if (StartsWithMz(item, name))
        {
            return new AlternateStream(item, name, size, StreamKind.HiddenExecutable, "A Windows program is hidden in this stream - a common malware trick. Scan it with your antivirus.");
        }

        if (KnownStreams.Contains(name) || KnownStreamPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            return new AlternateStream(item, name, size, StreamKind.KnownMetadata, "Metadata written by Windows or a sync/document app (normally harmless).");
        }

        return new AlternateStream(item, name, size, StreamKind.HiddenData, $"Unrecognized data hidden next to the file ({FormatBytes(size)}).");
    }

    private static bool StartsWithMz(string item, string name)
    {
        try
        {
            using var fs = new FileStream(LongPath(item + ":" + name), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return fs.ReadByte() == 'M' && fs.ReadByte() == 'Z';
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static IEnumerable<string> EnumerateItems(string root, bool recursive)
    {
        yield return root;
        if (!Directory.Exists(root))
        {
            yield break;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = recursive,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };
        foreach (var entry in Directory.EnumerateFileSystemEntries(root, "*", options))
        {
            yield return entry;
        }
    }

    /// <summary>Named data streams of a file or folder (the unnamed main stream is excluded).</summary>
    internal static List<(string Name, long Size)> EnumerateStreams(string path)
    {
        var result = new List<(string, long)>();
        var handle = FindFirstStreamW(LongPath(path), 0, out var data, 0);
        if (handle == InvalidHandle)
        {
            var error = Marshal.GetLastWin32Error();
            if (error is ErrorHandleEof or ErrorInvalidParameter)
            {
                return result; // no streams at all (folders without named streams report one of these)
            }

            throw new IOException($"Can't read streams of {path} (error {error}).");
        }

        try
        {
            do
            {
                // Names look like ":Zone.Identifier:$DATA"; the main stream is "::$DATA".
                var raw = data.cStreamName ?? string.Empty;
                if (raw.StartsWith(':') && !raw.StartsWith("::", StringComparison.Ordinal))
                {
                    var end = raw.LastIndexOf(':');
                    var name = end > 0 ? raw[1..end] : raw[1..];
                    result.Add((name, data.StreamSize));
                }
            }
            while (FindNextStreamW(handle, out data));
        }
        finally
        {
            FindClose(handle);
        }

        return result;
    }

    private static bool SupportsStreams(string path)
    {
        try
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root) || root.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return true; // network share: let the server answer
            }

            var format = new DriveInfo(root).DriveFormat;
            return format.Equals("NTFS", StringComparison.OrdinalIgnoreCase) || format.Equals("ReFS", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    internal static string LongPath(string path)
    {
        if (path.Length < 248 || path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return path;
        }

        return path.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + path[2..] : @"\\?\" + path;
    }

    private static string ToPrintable(ReadOnlySpan<byte> bytes)
    {
        // UTF-16 text (e.g. many metadata streams) starts with a BOM or has zero high bytes.
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Clean(Encoding.Unicode.GetString(bytes[2..]));
        }

        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            sb.Append(b is >= 32 and < 127 or (byte)'\n' or (byte)'\r' or (byte)'\t' ? (char)b : '.');
        }

        return sb.ToString();

        static string Clean(string s) => new(s.Select(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t' ? '.' : c).ToArray());
    }

    internal static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var order = 0;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.#} {sizes[order]}";
    }

    private const int ErrorHandleEof = 38;
    private const int ErrorInvalidParameter = 87;
    private static readonly IntPtr InvalidHandle = new(-1);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Win32FindStreamData
    {
        public long StreamSize;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
        public string cStreamName;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindFirstStreamW(string lpFileName, int infoLevel, out Win32FindStreamData data, uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FindNextStreamW(IntPtr handle, out Win32FindStreamData data);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FindClose(IntPtr handle);
}
