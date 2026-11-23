using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services;

/// <summary>
/// File-name search backed by the Rust engine in nexus_core.dll (see <c>src/nexus_core/src/ffi/search_api.rs</c>).
/// The engine holds one index per process; this service is registered as a singleton.
/// </summary>
public sealed class NexusIndexerService : INexusIndexerService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private readonly ILogService? _log;
    private readonly SemaphoreSlim _buildLock = new(1, 1);
    private NexusIndexInfo? _current;
    private volatile bool _isBuilding;

    public NexusIndexerService(ILogService? logService)
        : this(logService, DefaultIndexPath())
    {
    }

    public NexusIndexerService(ILogService? logService, string indexFilePath)
    {
        _log = logService;
        IndexFilePath = indexFilePath;
        try
        {
            using var doc = JsonDocument.Parse(Call(Native.nexus_engine_version()));
            EngineVersion = doc.RootElement.GetProperty("version").GetString();
            IsAvailable = true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            UnavailableReason = "The search engine (nexus_core.dll) is missing or does not match this version of Winhance-FS.";
            _log?.Log(LogLevel.Warning, $"nexus_core unavailable: {ex.Message}");
        }
    }

    public event EventHandler? IndexChanged;

    public bool IsAvailable { get; }

    public string? UnavailableReason { get; }

    public string? EngineVersion { get; }

    public bool HasIndex => _current != null;

    public bool IsBuilding => _isBuilding;

    public NexusIndexInfo? Current => _current;

    public string IndexFilePath { get; }

    /// <summary>
    /// <c>%LOCALAPPDATA%\Winhance-FS\search-index.bin</c>, shared by the app and the MCP server. The
    /// <c>WINHANCE_FS_INDEX</c> environment variable overrides it.
    /// </summary>
    public static string DefaultIndexPath() =>
        Environment.GetEnvironmentVariable("WINHANCE_FS_INDEX") is { Length: > 0 } custom
            ? Path.GetFullPath(Environment.ExpandEnvironmentVariables(custom))
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Winhance-FS", "search-index.bin");

    public async Task<NexusIndexInfo?> LoadSavedIndexAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || !File.Exists(IndexFilePath))
        {
            return null;
        }

        var reply = await Task.Run(() => Call(Native.nexus_index_load(IndexFilePath)), cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(reply);
        if (!Ok(doc, out var error))
        {
            _log?.Log(LogLevel.Warning, $"Saved search index not loaded: {error}");
            return null;
        }

        SetCurrent(ParseInfo(doc.RootElement));
        return _current;
    }

    public async Task<NexusIndexInfo> BuildIndexAsync(IReadOnlyList<string> roots, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        if (roots == null || roots.Count == 0)
        {
            throw new ArgumentException("Choose at least one drive or folder to index.", nameof(roots));
        }

        await _buildLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        _isBuilding = true;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = JsonSerializer.Serialize(new { roots, save_to = IndexFilePath });
            var build = Task.Run(() => Call(Native.nexus_index_build(request)), CancellationToken.None);
            using (cancellationToken.Register(Native.nexus_index_cancel))
            {
                while (!build.IsCompleted)
                {
                    await Task.WhenAny(build, Task.Delay(200, CancellationToken.None)).ConfigureAwait(false);
                    progress?.Report((long)Native.nexus_index_progress());
                }
            }

            using var doc = JsonDocument.Parse(await build.ConfigureAwait(false));
            var root = doc.RootElement;
            if (root.TryGetProperty("cancelled", out var c) && c.ValueKind == JsonValueKind.True)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (!Ok(doc, out var error))
            {
                throw new InvalidOperationException(error);
            }

            if (root.TryGetProperty("saveError", out var saveError))
            {
                _log?.Log(LogLevel.Warning, $"Search index built but not saved: {saveError.GetString()}");
            }

            var info = ParseInfo(root);
            SetCurrent(info);
            _log?.Log(LogLevel.Info, $"Search index: {info.Entries:N0} items in {info.BuildMilliseconds} ms ({string.Join(", ", info.Roots.Select(r => $"{r.Path} {r.Method}"))})");
            return info;
        }
        finally
        {
            _isBuilding = false;
            _buildLock.Release();
        }
    }

    public async Task<NexusSearchResult> SearchAsync(string query, int limit = 200, CancellationToken cancellationToken = default, NexusSort sort = NexusSort.Relevance)
    {
        query ??= string.Empty;
        if (!IsAvailable || _current == null || (string.IsNullOrWhiteSpace(query) && sort == NexusSort.Relevance))
        {
            return new NexusSearchResult(0, 0, Array.Empty<NexusFileEntry>());
        }

        var request = JsonSerializer.Serialize(new { query, limit, sort = sort.ToString().ToLowerInvariant() });
        var reply = await Task.Run(() => Call(Native.nexus_index_search(request)), cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(reply);
        if (!Ok(doc, out var error))
        {
            throw new InvalidOperationException(error);
        }

        var root = doc.RootElement;
        var results = root.GetProperty("results").EnumerateArray().Select(r => new NexusFileEntry
        {
            Path = r.GetProperty("path").GetString() ?? string.Empty,
            Name = r.GetProperty("name").GetString() ?? string.Empty,
            Folder = r.GetProperty("folder").GetString() ?? string.Empty,
            IsDirectory = r.GetProperty("isDir").GetBoolean(),
            Size = r.GetProperty("size").GetInt64(),
            Modified = r.TryGetProperty("modified", out var m) ? DateTimeOffset.FromUnixTimeSeconds(m.GetInt64()).LocalDateTime : null,
            Missing = r.GetProperty("missing").GetBoolean(),
        }).ToList();
        return new NexusSearchResult(root.GetProperty("total").GetInt64(), root.GetProperty("ms").GetInt64(), results);
    }

    private void EnsureAvailable()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(UnavailableReason);
        }
    }

    private void SetCurrent(NexusIndexInfo info)
    {
        _current = info;
        IndexChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool Ok(JsonDocument doc, out string error)
    {
        var root = doc.RootElement;
        if (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
        {
            error = string.Empty;
            return true;
        }

        error = root.TryGetProperty("error", out var e) ? e.GetString() ?? "Unknown error" : "Unknown error";
        return false;
    }

    private static NexusIndexInfo ParseInfo(JsonElement root)
    {
        var roots = root.GetProperty("roots").EnumerateArray().Select(r => new NexusRootInfo(
            r.GetProperty("path").GetString() ?? string.Empty,
            r.GetProperty("method").GetString() ?? "scan",
            r.GetProperty("entries").GetInt64(),
            r.TryGetProperty("error", out var e) ? e.GetString() : null)).ToList();
        return new NexusIndexInfo(
            root.GetProperty("entries").GetInt64(),
            root.GetProperty("files").GetInt64(),
            root.GetProperty("folders").GetInt64(),
            DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("built").GetInt64()).UtcDateTime,
            root.GetProperty("ms").GetInt64(),
            roots);
    }

    /// <summary>Copies a string returned by the engine and releases it.</summary>
    private static string Call(IntPtr reply)
    {
        if (reply == IntPtr.Zero)
        {
            return "{\"ok\":false,\"error\":\"No reply from the search engine\"}";
        }

        try
        {
            return Marshal.PtrToStringUTF8(reply) ?? string.Empty;
        }
        finally
        {
            Native.nexus_free_string(reply);
        }
    }

    private static class Native
    {
        private const string Dll = "nexus_core";

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nexus_engine_version();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nexus_index_build([MarshalAs(UnmanagedType.LPUTF8Str)] string request);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nexus_index_cancel();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong nexus_index_progress();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nexus_index_load([MarshalAs(UnmanagedType.LPUTF8Str)] string path);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr nexus_index_search([MarshalAs(UnmanagedType.LPUTF8Str)] string request);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nexus_free_string(IntPtr text);
    }
}
