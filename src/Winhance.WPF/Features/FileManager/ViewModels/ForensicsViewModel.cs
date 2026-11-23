using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// Forensics: hidden NTFS streams (incl. "downloaded from the internet" markers), content checks for encrypted or
    /// disguised files, previous versions from Windows snapshots, and Recycle Bin recovery.
    /// Scans are read-only; removing streams and restoring files always ask first and never overwrite silently.
    /// </summary>
    public partial class ForensicsViewModel : ObservableObject
    {
        private readonly IForensicsService? _forensics;
        private readonly object _sync = new();
        private string _streamsSummary = string.Empty;
        private string _contentSummary = string.Empty;
        private string _versionsSummary = string.Empty;
        private string _recycleSummary = string.Empty;
        private CancellationTokenSource? _cts;

        public ForensicsViewModel()
        {
            StatusMessage = "Design-time";
            RecycleView = CollectionViewSource.GetDefaultView(RecycleItems);
        }

        public ForensicsViewModel(IForensicsService? forensics)
        {
            _forensics = forensics;
            foreach (var collection in new System.Collections.IEnumerable[] { Streams, Inspections, Protection, Versions, RecycleItems })
            {
                BindingOperations.EnableCollectionSynchronization(collection, _sync); // results may arrive off the UI thread
            }

            RecycleView = CollectionViewSource.GetDefaultView(RecycleItems);
            RecycleView.Filter = o => o is RecycleRow r && (RecycleFilter.Length == 0
                || r.Item.OriginalPath.Contains(RecycleFilter, StringComparison.OrdinalIgnoreCase));
        }

        public ObservableCollection<StreamRow> Streams { get; } = new();

        public ObservableCollection<InspectionRow> Inspections { get; } = new();

        public ObservableCollection<VolumeProtection> Protection { get; } = new();

        public ObservableCollection<VersionRow> Versions { get; } = new();

        public ObservableCollection<RecycleRow> RecycleItems { get; } = new();

        public ICollectionView RecycleView { get; }

        [ObservableProperty]
        private string _targetPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) is { Length: > 0 } home ? Path.Combine(home, "Downloads") : string.Empty;

        [ObservableProperty]
        private bool _recursive = true;

        [ObservableProperty]
        private bool _showAllFiles;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Pick a file or folder, then run a check. Checks only read; nothing changes unless you confirm.";

        [ObservableProperty]
        private string _summary = string.Empty;

        [ObservableProperty]
        private StreamRow? _selectedStream;

        [ObservableProperty]
        private string _streamPreview = string.Empty;

        [ObservableProperty]
        private InspectionRow? _selectedInspection;

        [ObservableProperty]
        private VersionRow? _selectedVersion;

        [ObservableProperty]
        private string _versionsNote = "Choose a file or folder and click Find Versions to see earlier copies Windows kept in its snapshots.";

        [ObservableProperty]
        private RecycleRow? _selectedRecycleItem;

        [ObservableProperty]
        private string _recycleFilter = string.Empty;

        [ObservableProperty]
        private bool _isStreamsSection = true;

        [ObservableProperty]
        private bool _isContentSection;

        [ObservableProperty]
        private bool _isVersionsSection;

        [ObservableProperty]
        private bool _isRecycleSection;

        partial void OnRecycleFilterChanged(string value) => RecycleView.Refresh();

        partial void OnSelectedStreamChanged(StreamRow? value) => _ = LoadPreviewAsync(value);

        // The header summary always describes the section on screen.
        partial void OnIsStreamsSectionChanged(bool value)
        {
            if (value)
            {
                Summary = _streamsSummary;
            }
        }

        partial void OnIsContentSectionChanged(bool value)
        {
            if (value)
            {
                Summary = _contentSummary;
            }
        }

        partial void OnIsVersionsSectionChanged(bool value)
        {
            if (value)
            {
                Summary = _versionsSummary;
            }

            if (value && Protection.Count == 0 && !IsBusy)
            {
                _ = LoadProtectionAsync();
            }
        }

        partial void OnIsRecycleSectionChanged(bool value)
        {
            if (value)
            {
                Summary = _recycleSummary;
            }

            if (value && RecycleItems.Count == 0 && !IsBusy)
            {
                _ = LoadRecycleBinAsync();
            }
        }

        // ---------------------------------------------------------------- Target

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Choose a folder to check" };
            if (dialog.ShowDialog() == true)
            {
                TargetPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void BrowseFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Choose a file to check", CheckFileExists = false };
            if (dialog.ShowDialog() == true)
            {
                TargetPath = dialog.FileName;
            }
        }

        private string? ValidTarget(bool mayBeMissing = false)
        {
            var path = TargetPath.Trim().Trim('"');
            if (path.Length == 0)
            {
                StatusMessage = "Enter or browse to a file or folder first";
                return null;
            }

            try
            {
                path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                StatusMessage = $"Not a valid path: {ex.Message}";
                return null;
            }

            if (!mayBeMissing && !File.Exists(path) && !Directory.Exists(path))
            {
                StatusMessage = $"Not found: {path}";
                return null;
            }

            return path;
        }

        private CancellationToken Begin(string message)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            IsBusy = true;
            StatusMessage = message;
            return _cts.Token;
        }

        [RelayCommand]
        private void Cancel() => _cts?.Cancel();

        // ---------------------------------------------------------------- Hidden streams

        [RelayCommand]
        private async Task ScanStreamsAsync()
        {
            var path = ValidTarget();
            if (path == null || _forensics == null)
            {
                return;
            }

            IsStreamsSection = true;
            var ct = Begin($"Looking for hidden streams in {path}...");
            Streams.Clear();
            StreamPreview = string.Empty;
            try
            {
                var progress = new Progress<string>(m => StatusMessage = m);
                var result = await _forensics.ScanStreamsAsync(path, Recursive, progress, ct);
                foreach (var s in result.Streams)
                {
                    Streams.Add(new StreamRow(s));
                }

                var exe = result.Streams.Count(s => s.Kind == StreamKind.HiddenExecutable);
                var hidden = result.Streams.Count(s => s.Kind == StreamKind.HiddenData);
                var motw = result.Streams.Count(s => s.Kind == StreamKind.MarkOfTheWeb);
                _streamsSummary = Summary = $"{Plural(result.Streams.Count, "stream")} · {Plural(exe, "hidden program")} · {hidden} hidden data · {Plural(motw, "download")}";
                StatusMessage = result.Error
                    ?? (result.Streams.Count == 0
                        ? $"Checked {result.ItemsScanned:N0} items: no hidden streams."
                        : $"Checked {result.ItemsScanned:N0} items" + (result.ItemsSkipped > 0 ? $" ({result.ItemsSkipped} unreadable)" : string.Empty) +
                          (exe > 0 ? ". WARNING: programs are hidden inside streams - scan them with your antivirus." : ". Select a stream to see what's inside."));
                SelectedStream = Streams.FirstOrDefault();
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Stream scan failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadPreviewAsync(StreamRow? row)
        {
            if (row == null || _forensics == null)
            {
                StreamPreview = string.Empty;
                return;
            }

            try
            {
                var text = await _forensics.ReadStreamPreviewAsync(row.Stream, 2048);
                if (ReferenceEquals(SelectedStream, row))
                {
                    StreamPreview = text.Length == 0 ? "(empty)" : text;
                }
            }
            catch (Exception ex)
            {
                StreamPreview = $"Can't read this stream: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SelectDownloadMarkers() => SelectStreams(s => s.Kind == StreamKind.MarkOfTheWeb);

        [RelayCommand]
        private void SelectHiddenData() => SelectStreams(s => s.Kind is StreamKind.HiddenData or StreamKind.HiddenExecutable);

        [RelayCommand]
        private void SelectNoStreams() => SelectStreams(_ => false);

        private void SelectStreams(Func<AlternateStream, bool> predicate)
        {
            foreach (var row in Streams)
            {
                row.IsSelected = predicate(row.Stream);
            }

            StatusMessage = $"{Streams.Count(r => r.IsSelected)} stream(s) selected";
        }

        [RelayCommand]
        private async Task RemoveSelectedStreamsAsync()
        {
            if (_forensics == null)
            {
                return;
            }

            var chosen = Streams.Where(r => r.IsSelected).ToList();
            if (chosen.Count == 0)
            {
                StatusMessage = "Tick the streams to remove first";
                return;
            }

            var motw = chosen.Count(r => r.Stream.Kind == StreamKind.MarkOfTheWeb);
            var message = $"Remove {chosen.Count} hidden stream(s) from {chosen.Select(r => r.Stream.FilePath).Distinct().Count()} file(s)?\n\n" +
                          "The files themselves are not changed - only the extra data attached to them.\n" +
                          (motw > 0 ? $"\n{motw} are download markers: Windows will stop warning before opening those files. Only do this for files you trust.\n" : string.Empty) +
                          "\nThis cannot be undone.";
            if (System.Windows.MessageBox.Show(message, "Remove hidden streams", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
            {
                StatusMessage = "Cancelled - nothing was removed";
                return;
            }

            var ct = Begin("Removing streams...");
            try
            {
                var result = await _forensics.RemoveStreamsAsync(chosen.Select(r => r.Stream), ct);
                // Re-read each touched file so the list shows exactly what is still there.
                foreach (var file in chosen.GroupBy(r => r.Stream.FilePath, StringComparer.OrdinalIgnoreCase))
                {
                    var still = (await _forensics.ScanStreamsAsync(file.Key, recursive: false, null, ct)).Streams
                        .Where(x => string.Equals(x.FilePath, file.Key, StringComparison.OrdinalIgnoreCase))
                        .Select(x => x.StreamName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    foreach (var row in file.Where(r => !still.Contains(r.Stream.StreamName)))
                    {
                        Streams.Remove(row);
                    }
                }

                StatusMessage = $"Removed {result.Succeeded} stream(s)" + (result.Failed > 0 ? $"; {result.Failed} not removed: {result.Errors[0]}" : ".");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Remove failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ---------------------------------------------------------------- Content check

        [RelayCommand]
        private async Task ScanContentAsync()
        {
            var path = ValidTarget();
            if (path == null || _forensics == null)
            {
                return;
            }

            IsContentSection = true;
            var ct = Begin($"Checking file contents in {path}...");
            Inspections.Clear();
            try
            {
                var progress = new Progress<string>(m => StatusMessage = m);
                var result = await _forensics.ScanEntropyAsync(path, new EntropyScanOptions { Recursive = Recursive, OnlySuspicious = !ShowAllFiles }, progress, ct);
                foreach (var f in result.Files)
                {
                    Inspections.Add(new InspectionRow(f));
                }

                var disguised = result.Files.Count(f => f.Verdict == FileVerdict.DisguisedExecutable);
                var encrypted = result.Files.Count(f => f.Verdict == FileVerdict.LikelyEncrypted);
                var mismatch = result.Files.Count(f => f.Verdict == FileVerdict.ExtensionMismatch);
                _contentSummary = Summary = $"{Plural(result.FilesScanned, "file")} · {Plural(disguised, "disguised program")} · {encrypted} likely encrypted · {mismatch} wrong extension";
                StatusMessage = result.Error
                    ?? (disguised + encrypted > 0
                        ? "WARNING: some files look encrypted or disguised. If many of your documents are listed as encrypted, disconnect from the network and check for ransomware."
                        : $"Checked {result.FilesScanned:N0} files" + (result.FilesSkipped > 0 ? $" ({result.FilesSkipped} unreadable)" : string.Empty) + ": nothing suspicious.");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Content check failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ---------------------------------------------------------------- Previous versions

        [RelayCommand]
        private async Task LoadProtectionAsync()
        {
            if (_forensics == null)
            {
                return;
            }

            try
            {
                Protection.Clear();
                foreach (var p in await _forensics.GetVolumeProtectionAsync())
                {
                    Protection.Add(p);
                }

                if (Protection.All(p => p.SnapshotCount == 0))
                {
                    VersionsNote = "Windows has no snapshots on any drive, so there are no previous versions to restore. Turn on System Protection (button below) so future changes can be undone.";
                }
            }
            catch (Exception ex)
            {
                VersionsNote = ex.Message;
            }
        }

        [RelayCommand]
        private async Task FindVersionsAsync()
        {
            var path = ValidTarget(mayBeMissing: true);
            if (path == null || _forensics == null)
            {
                return;
            }

            IsVersionsSection = true;
            var ct = Begin($"Looking for earlier copies of {path}...");
            Versions.Clear();
            try
            {
                await LoadProtectionAsync();
                var versions = await _forensics.GetPreviousVersionsAsync(path, ct);
                foreach (var v in versions)
                {
                    Versions.Add(new VersionRow(v));
                }

                _versionsSummary = Summary = Plural(versions.Count, "earlier version");
                var snapshots = Protection.Where(p => path.StartsWith(p.VolumeRoot, StringComparison.OrdinalIgnoreCase)).Sum(p => p.SnapshotCount);
                VersionsNote = versions.Count > 0
                    ? $"{versions.Count} earlier version(s). Restore a Copy saves it next to the original; nothing is overwritten unless you choose Replace."
                    : snapshots == 0
                        ? $"No snapshots exist for {Path.GetPathRoot(path)}, so Windows kept no earlier copies. Turn on System Protection to keep them from now on."
                        : $"None of the {snapshots} snapshot(s) contain this item (it may be newer than the oldest snapshot).";
                StatusMessage = VersionsNote;
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled";
            }
            catch (Exception ex)
            {
                VersionsNote = StatusMessage = $"Could not list previous versions: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RestoreVersionCopyAsync()
        {
            var row = SelectedVersion;
            if (row == null || _forensics == null)
            {
                StatusMessage = "Select a version first";
                return;
            }

            var v = row.Version;
            var stamp = v.Snapshot.Created.ToString("yyyy-MM-dd HHmm");
            var dir = Path.GetDirectoryName(v.OriginalPath) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(v.OriginalPath);
            var ext = v.IsFolder ? string.Empty : Path.GetExtension(v.OriginalPath);
            var suggested = Path.Combine(dir, $"{name} (restored {stamp}){ext}");

            string? destination;
            if (v.IsFolder)
            {
                destination = Directory.Exists(dir) && !Directory.Exists(suggested) ? suggested : null;
                if (destination == null)
                {
                    var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Choose where to put the restored folder" };
                    if (dialog.ShowDialog() != true)
                    {
                        return;
                    }

                    destination = Path.Combine(dialog.FolderName, $"{name} (restored {stamp})");
                }
            }
            else
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save the restored copy as",
                    InitialDirectory = Directory.Exists(dir) ? dir : null,
                    FileName = Path.GetFileName(suggested),
                    OverwritePrompt = true,
                };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                destination = dialog.FileName;
            }

            await RestoreVersionAsync(v, destination, overwrite: !v.IsFolder && File.Exists(destination));
        }

        [RelayCommand]
        private async Task ReplaceWithVersionAsync()
        {
            var row = SelectedVersion;
            if (row == null || _forensics == null)
            {
                StatusMessage = "Select a version first";
                return;
            }

            var v = row.Version;
            if (v.IsFolder)
            {
                StatusMessage = "Folders are restored as a copy next to the original; use Restore a Copy.";
                return;
            }

            var message = $"Replace the current\n{v.OriginalPath}\nwith the version from {v.Snapshot.Created:g} ({FormatBytes(v.Size)}, modified {v.Modified:g})?\n\n" +
                          "The current file will be overwritten. If unsure, use Restore a Copy instead.";
            if (System.Windows.MessageBox.Show(message, "Replace with previous version", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
            {
                StatusMessage = "Cancelled - nothing was changed";
                return;
            }

            await RestoreVersionAsync(v, v.OriginalPath, overwrite: true);
        }

        private async Task RestoreVersionAsync(PreviousVersion v, string destination, bool overwrite)
        {
            var ct = Begin($"Restoring to {destination}...");
            try
            {
                var result = await _forensics!.RestorePreviousVersionAsync(v, destination, overwrite, ct);
                StatusMessage = result.Success ? $"Restored to {result.RestoredPath}" : $"Not restored: {result.Error}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Restore failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenSystemProtection()
        {
            try
            {
                Process.Start(new ProcessStartInfo("SystemPropertiesProtection.exe") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not open System Protection: {ex.Message}";
            }
        }

        // ---------------------------------------------------------------- Recycle Bin

        [RelayCommand]
        private async Task LoadRecycleBinAsync()
        {
            if (_forensics == null)
            {
                return;
            }

            IsRecycleSection = true;
            var ct = Begin("Reading the Recycle Bin...");
            try
            {
                RecycleItems.Clear();
                foreach (var item in await _forensics.ListRecycleBinAsync(ct))
                {
                    RecycleItems.Add(new RecycleRow(item));
                }

                _recycleSummary = Summary = $"{Plural(RecycleItems.Count, "deleted item")} · {FormatBytes(RecycleItems.Sum(r => r.Item.Size))}";
                StatusMessage = RecycleItems.Count == 0
                    ? "Your Recycle Bin is empty. Files deleted with Shift+Delete or emptied from the bin may still be in Previous Versions."
                    : $"{RecycleItems.Count:N0} item(s), {FormatBytes(RecycleItems.Sum(r => r.Item.Size))}. Tick items and click Restore, or Restore To... to choose a folder.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not read the Recycle Bin: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task RestoreRecycledAsync() => RestoreRecycledToAsync(null);

        [RelayCommand]
        private async Task RestoreRecycledToFolderAsync()
        {
            if (!ChosenRecycleItems().Any())
            {
                StatusMessage = "Tick the items to restore first";
                return;
            }

            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Restore the ticked items into this folder" };
            if (dialog.ShowDialog() == true)
            {
                await RestoreRecycledToAsync(dialog.FolderName);
            }
        }

        private List<RecycleRow> ChosenRecycleItems()
        {
            var ticked = RecycleItems.Where(r => r.IsSelected).ToList();
            return ticked.Count > 0 || SelectedRecycleItem == null ? ticked : new List<RecycleRow> { SelectedRecycleItem };
        }

        private async Task RestoreRecycledToAsync(string? folder)
        {
            if (_forensics == null)
            {
                return;
            }

            var chosen = ChosenRecycleItems();
            if (chosen.Count == 0)
            {
                StatusMessage = "Tick the items to restore first";
                return;
            }

            var ct = Begin($"Restoring {chosen.Count} item(s)...");
            var restored = 0;
            var errors = new List<string>();
            try
            {
                foreach (var row in chosen)
                {
                    var dest = folder == null ? null : Path.Combine(folder, row.Item.Name);
                    var result = await _forensics.RestoreFromRecycleBinAsync(row.Item, dest, ct);
                    if (result.Success)
                    {
                        restored++;
                        RecycleItems.Remove(row);
                    }
                    else
                    {
                        errors.Add($"{row.Item.Name}: {result.Error}");
                    }
                }

                StatusMessage = $"Restored {restored} item(s)" + (errors.Count > 0 ? $"; {errors.Count} not restored - {errors[0]}" : ".");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Restore failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // ---------------------------------------------------------------- Shared

        [RelayCommand]
        private void OpenLocation(string? path)
        {
            path ??= SelectedStream?.Stream.FilePath ?? SelectedInspection?.Inspection.Path;
            if (string.IsNullOrEmpty(path) || (!File.Exists(path) && !Directory.Exists(path)))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not open Explorer: {ex.Message}";
            }
        }

        internal static string FormatBytes(long bytes) => DuplicateFinderViewModel.FormatBytes(bytes);

        private static string Plural(int count, string noun) => $"{count:N0} {noun}{(count == 1 ? string.Empty : "s")}";
    }

    public partial class StreamRow : ObservableObject
    {
        public StreamRow(AlternateStream stream) => Stream = stream;

        public AlternateStream Stream { get; }

        [ObservableProperty]
        private bool _isSelected;

        public string KindText => Stream.Kind switch
        {
            StreamKind.HiddenExecutable => "Hidden program",
            StreamKind.HiddenData => "Hidden data",
            StreamKind.MarkOfTheWeb => "Download marker",
            _ => "Metadata",
        };

        public string KindColor => Stream.Kind switch
        {
            StreamKind.HiddenExecutable => "#FF6B6B",
            StreamKind.HiddenData => "#F5A623",
            StreamKind.MarkOfTheWeb => "#4FC3F7",
            _ => "#9E9E9E",
        };

        public string FileName => Path.GetFileName(Stream.FilePath.TrimEnd('\\')) is { Length: > 0 } n ? n : Stream.FilePath;

        public string Folder => Path.GetDirectoryName(Stream.FilePath) ?? string.Empty;

        public string SizeText => ForensicsViewModel.FormatBytes(Stream.Size);
    }

    public sealed class InspectionRow
    {
        public InspectionRow(FileInspection inspection) => Inspection = inspection;

        public FileInspection Inspection { get; }

        public string VerdictText => Inspection.Verdict switch
        {
            FileVerdict.DisguisedExecutable => "Disguised program",
            FileVerdict.LikelyEncrypted => "Likely encrypted",
            FileVerdict.ExtensionMismatch => "Wrong extension",
            FileVerdict.Compressed => "Compressed",
            _ => "Normal",
        };

        public string VerdictColor => Inspection.Verdict switch
        {
            FileVerdict.DisguisedExecutable => "#FF6B6B",
            FileVerdict.LikelyEncrypted => "#FF6B6B",
            FileVerdict.ExtensionMismatch => "#F5A623",
            FileVerdict.Compressed => "#4FC3F7",
            _ => "#66BB6A",
        };

        public string FileName => Path.GetFileName(Inspection.Path);

        public string Folder => Path.GetDirectoryName(Inspection.Path) ?? string.Empty;

        public string EntropyText => $"{Inspection.Entropy:0.00} / 8";

        /// <summary>0-100 for the entropy bar.</summary>
        public double EntropyPercent => Inspection.Entropy / 8 * 100;

        public string SizeText => ForensicsViewModel.FormatBytes(Inspection.Size);

        public string DetectedText => Inspection.DetectedType ?? "Unknown";
    }

    public sealed class VersionRow
    {
        public VersionRow(PreviousVersion version) => Version = version;

        public PreviousVersion Version { get; }

        public DateTime SnapshotTime => Version.Snapshot.Created;

        public DateTime Modified => Version.Modified;

        public string SizeText => Version.IsFolder ? "Folder" : ForensicsViewModel.FormatBytes(Version.Size);

        public string StateText => Version.State switch
        {
            VersionState.SameAsCurrent => "Same as now",
            VersionState.DeletedNow => "Deleted since",
            _ => "Different",
        };
    }

    public partial class RecycleRow : ObservableObject
    {
        public RecycleRow(RecycleBinItem item) => Item = item;

        public RecycleBinItem Item { get; }

        [ObservableProperty]
        private bool _isSelected;

        public string Name => Item.Name;

        public string OriginalFolder => Path.GetDirectoryName(Item.OriginalPath) ?? string.Empty;

        public DateTime Deleted => Item.Deleted;

        public string SizeText => Item.IsFolder ? $"Folder, {ForensicsViewModel.FormatBytes(Item.Size)}" : ForensicsViewModel.FormatBytes(Item.Size);

        public bool OriginalPlaceTaken => File.Exists(Item.OriginalPath) || Directory.Exists(Item.OriginalPath);
    }
}
