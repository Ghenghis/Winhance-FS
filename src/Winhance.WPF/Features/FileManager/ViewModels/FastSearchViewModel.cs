using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.WPF.Features.FileManager.ViewModels;

/// <summary>
/// Search tab: instant file-name search over whole drives (read from the NTFS file table) and folders.
/// </summary>
public partial class FastSearchViewModel : ObservableObject
{
    private const int ResultLimit = 500;
    private readonly INexusIndexerService? _engine;
    private readonly object _resultsLock = new();
    private CancellationTokenSource? _buildCts;
    private CancellationTokenSource? _searchCts;
    private bool _loadStarted;

    [ObservableProperty]
    private string _query = string.Empty;

    [ObservableProperty]
    private FastSearchRow? _selectedResult;

    [ObservableProperty]
    private string _resultSummary = string.Empty;

    [ObservableProperty]
    private string _indexSummary = "No index yet";

    [ObservableProperty]
    private string _statusMessage = "Pick drives or folders and click Build Index.";

    [ObservableProperty]
    private bool _isBuilding;

    [ObservableProperty]
    private bool _hasIndex;

    [ObservableProperty]
    private string _newFolder = string.Empty;

    [ObservableProperty]
    private string? _selectedFolder;

    [ObservableProperty]
    private SortChoice _selectedSort = SortChoices[0];

    public static IReadOnlyList<SortChoice> SortChoices { get; } = new[]
    {
        new SortChoice("Best match", NexusSort.Relevance),
        new SortChoice("Largest first", NexusSort.Size),
        new SortChoice("Newest first", NexusSort.Modified),
    };

    public FastSearchViewModel(INexusIndexerService? engine)
    {
        _engine = engine;
        BindingOperations.EnableCollectionSynchronization(Results, _resultsLock);
        foreach (var drive in SafeDrives())
        {
            Drives.Add(new DriveChoice(drive.Root, drive.Label, drive.IsFixed));
        }

        if (_engine == null || !_engine.IsAvailable)
        {
            StatusMessage = _engine?.UnavailableReason ?? "The search engine is not available.";
        }
    }

    public ObservableCollection<FastSearchRow> Results { get; } = new();

    public ObservableCollection<DriveChoice> Drives { get; } = new();

    public ObservableCollection<string> Folders { get; } = new();

    public bool IsEngineAvailable => _engine?.IsAvailable == true;

    public string EngineText => _engine?.IsAvailable == true ? $"Rust engine {_engine.EngineVersion}" : "Engine missing";

    public static string SyntaxHelp =>
        "tax 2024 — all words  ·  \"exact phrase\"  ·  *.iso  IMG_????.jpg — wildcards  ·  projects\\ readme — path contains  ·  " +
        "!backup — exclude  ·  ext:pdf;docx  ·  file:  folder:";

    /// <summary>Loads the saved index the first time the tab is shown.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (_loadStarted || _engine == null || !_engine.IsAvailable)
        {
            return;
        }

        _loadStarted = true;
        if (_engine.HasIndex)
        {
            ShowIndex(_engine.Current!);
            return;
        }

        if (!File.Exists(_engine.IndexFilePath))
        {
            return;
        }

        StatusMessage = "Loading the saved index...";
        try
        {
            var info = await _engine.LoadSavedIndexAsync();
            if (info != null)
            {
                ShowIndex(info);
                StatusMessage = "Ready. Type to search.";
                await RunSearchAsync(Query);
            }
            else
            {
                StatusMessage = "The saved index could not be read. Click Build Index to make a new one.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"The saved index could not be loaded: {ex.Message}";
        }
    }

    partial void OnQueryChanged(string value) => _ = DebouncedSearchAsync(value);

    partial void OnSelectedSortChanged(SortChoice value) => _ = DebouncedSearchAsync(Query);

    private async Task DebouncedSearchAsync(string text)
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(120, cts.Token);
            await RunSearchAsync(text, cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>Runs a search now (used by Enter and by tests).</summary>
    [RelayCommand]
    private Task SearchNow() => RunSearchAsync(Query);

    private async Task RunSearchAsync(string text, CancellationToken ct = default)
    {
        if (_engine == null || !_engine.HasIndex)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                ResultSummary = _engine?.IsAvailable == true ? "Build an index first" : string.Empty;
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(text) && SelectedSort.Sort == NexusSort.Relevance)
        {
            lock (_resultsLock)
            {
                Results.Clear();
            }

            ResultSummary = string.Empty;
            return;
        }

        try
        {
            var result = await _engine.SearchAsync(text, ResultLimit, ct, SelectedSort.Sort);
            ct.ThrowIfCancellationRequested();
            lock (_resultsLock)
            {
                Results.Clear();
                foreach (var entry in result.Results)
                {
                    Results.Add(new FastSearchRow(entry));
                }
            }

            ResultSummary = result.Total == 0
                ? $"No matches · {result.Milliseconds} ms"
                : result.Total > result.Results.Count
                    ? $"{result.Total:N0} matches · {result.Milliseconds} ms · showing the best {result.Results.Count:N0}"
                    : $"{Plural(result.Total, "match", "matches")} · {result.Milliseconds} ms";
            SelectedResult = Results.FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ResultSummary = $"Search failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddFolder()
    {
        var path = NewFolder.Trim().Trim('"');
        if (path.Length == 0)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Choose folders to include in the index", Multiselect = true };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            foreach (var folder in dialog.FolderNames)
            {
                AddFolderPath(folder);
            }

            return;
        }

        if (!Directory.Exists(path))
        {
            StatusMessage = $"Folder not found: {path}";
            return;
        }

        AddFolderPath(path);
        NewFolder = string.Empty;
    }

    private void AddFolderPath(string path)
    {
        var full = Path.GetFullPath(path).TrimEnd('\\');
        if (full.Length == 2 && full[1] == ':')
        {
            var drive = Drives.FirstOrDefault(d => d.Root.StartsWith(full, StringComparison.OrdinalIgnoreCase));
            if (drive != null)
            {
                drive.IsSelected = true;
                return;
            }

            full += "\\";
        }

        if (!Folders.Contains(full, StringComparer.OrdinalIgnoreCase))
        {
            Folders.Add(full);
        }

        SelectedFolder = Folders.First(f => string.Equals(f, full, StringComparison.OrdinalIgnoreCase));
        StatusMessage = $"Added {full}. Click Build Index to include it.";
    }

    [RelayCommand]
    private void RemoveFolder()
    {
        if (SelectedFolder != null)
        {
            Folders.Remove(SelectedFolder);
            SelectedFolder = Folders.FirstOrDefault();
        }
    }

    /// <summary>Roots that will be indexed: ticked drives plus added folders.</summary>
    public IReadOnlyList<string> SelectedRoots =>
        Drives.Where(d => d.IsSelected).Select(d => d.Root).Concat(Folders).ToList();

    [RelayCommand]
    private async Task BuildIndexAsync()
    {
        if (_engine == null || !_engine.IsAvailable)
        {
            StatusMessage = _engine?.UnavailableReason ?? "The search engine is not available.";
            return;
        }

        var roots = SelectedRoots;
        if (roots.Count == 0)
        {
            StatusMessage = "Tick at least one drive or add a folder.";
            return;
        }

        _buildCts = new CancellationTokenSource();
        IsBuilding = true;
        StatusMessage = $"Indexing {string.Join(", ", roots)}...";
        var started = Stopwatch.StartNew();
        var progress = new Progress<long>(n =>
        {
            if (IsBuilding)
            {
                StatusMessage = $"Indexing {string.Join(", ", roots)}... {n:N0} items found ({started.Elapsed.TotalSeconds:0} s)";
            }
        });
        try
        {
            var info = await _engine.BuildIndexAsync(roots, progress, _buildCts.Token);
            ShowIndex(info);
            var notes = info.Roots.Where(r => r.Note != null).Select(r => $"{r.Path}: {r.Note}").ToList();
            StatusMessage = $"Indexed {info.Files + info.Folders:N0} items in {info.BuildMilliseconds / 1000.0:0.0} s." +
                            (notes.Count > 0 ? " " + string.Join(" ", notes) : string.Empty);
            await RunSearchAsync(Query);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = _engine.HasIndex ? "Cancelled. The previous index is still in use." : "Cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Indexing failed: {ex.Message}";
        }
        finally
        {
            IsBuilding = false;
            _buildCts?.Dispose();
            _buildCts = null;
        }
    }

    [RelayCommand]
    private void CancelBuild() => _buildCts?.Cancel();

    private void ShowIndex(NexusIndexInfo info)
    {
        HasIndex = true;
        var where = string.Join(", ", info.Roots.Select(r => r.Method == "mft" ? $"{r.Path} (drive table)" : r.Path));
        IndexSummary = $"{info.Files + info.Folders:N0} items · {where} · built {Ago(info.BuiltUtc)} in {info.BuildMilliseconds / 1000.0:0.0} s";
        foreach (var drive in Drives)
        {
            drive.IsSelected = info.Roots.Any(r => string.Equals(r.Path, drive.Root, StringComparison.OrdinalIgnoreCase));
        }

        Folders.Clear();
        foreach (var root in info.Roots.Where(r => !Drives.Any(d => string.Equals(d.Root, r.Path, StringComparison.OrdinalIgnoreCase))))
        {
            Folders.Add(root.Path);
        }

        SelectedFolder = Folders.FirstOrDefault();
    }

    [RelayCommand]
    private void OpenItem(FastSearchRow? row)
    {
        row ??= SelectedResult;
        if (row == null)
        {
            return;
        }

        if (row.Entry.Missing || !(File.Exists(row.Entry.Path) || Directory.Exists(row.Entry.Path)))
        {
            StatusMessage = $"{row.Entry.Path} no longer exists. Build the index again to refresh it.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(row.Entry.Path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenLocation(FastSearchRow? row)
    {
        row ??= SelectedResult;
        if (row == null)
        {
            return;
        }

        try
        {
            var args = File.Exists(row.Entry.Path) || Directory.Exists(row.Entry.Path)
                ? $"/select,\"{row.Entry.Path}\""
                : $"\"{row.Entry.Folder}\"";
            Process.Start(new ProcessStartInfo("explorer.exe", args) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not open the folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CopyPath(FastSearchRow? row)
    {
        row ??= SelectedResult;
        if (row == null)
        {
            return;
        }

        try
        {
            Clipboard.SetText(row.Entry.Path);
            StatusMessage = $"Copied: {row.Entry.Path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not copy: {ex.Message}";
        }
    }

    internal static string Plural(long n, string one, string many) => $"{n:N0} {(n == 1 ? one : many)}";

    internal static string Ago(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1)
        {
            return "just now";
        }

        if (span.TotalHours < 1)
        {
            return $"{(int)span.TotalMinutes} min ago";
        }

        if (span.TotalDays < 1)
        {
            return $"{(int)span.TotalHours} h ago";
        }

        return utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }

    private static IEnumerable<(string Root, string Label, bool IsFixed)> SafeDrives()
    {
        foreach (var d in DriveInfo.GetDrives())
        {
            (string, string, bool)? item = null;
            try
            {
                if (d.IsReady && d.DriveType is System.IO.DriveType.Fixed or System.IO.DriveType.Removable)
                {
                    // Ticked by default: fixed NTFS drives (read from the file table in seconds). Others are walked
                    // folder by folder, which can be slow for cloud or network-backed drives, so they are opt-in.
                    var ntfs = string.Equals(d.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);
                    item = (d.Name, $"{d.Name.TrimEnd('\\')} ({d.DriveFormat})", ntfs && d.DriveType == System.IO.DriveType.Fixed);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            if (item != null)
            {
                yield return item.Value;
            }
        }
    }
}

/// <summary>An entry of the sort list.</summary>
public sealed record SortChoice(string Label, NexusSort Sort);

/// <summary>A drive that can be ticked for indexing.</summary>
public partial class DriveChoice : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public DriveChoice(string root, string label, bool selected)
    {
        Root = root;
        Label = label;
        _isSelected = selected;
    }

    public string Root { get; }

    public string Label { get; }

    public override string ToString() => Label;
}

/// <summary>One search result row.</summary>
public sealed class FastSearchRow
{
    public FastSearchRow(NexusFileEntry entry) => Entry = entry;

    public NexusFileEntry Entry { get; }

    public string Name => Entry.Name;

    public string Folder => Entry.Folder;

    public string Icon => Entry.IsDirectory ? "Folder" : "FileOutline";

    public string SizeText => Entry.IsDirectory ? string.Empty : DuplicateFinderViewModel.FormatBytes(Entry.Size);

    public string ModifiedText => Entry.Missing ? "deleted or moved" : Entry.Modified?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;

    public double Opacity => Entry.Missing ? 0.5 : 1.0;
}
