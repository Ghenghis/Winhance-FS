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
    /// Duplicate Finder: scan folders for files with identical content (size, quick hash, then SHA-256), mark copies by
    /// a keep rule or by hand, and move the marked copies to the Recycle Bin. Every group keeps at least one copy and
    /// every file is re-verified right before it is deleted.
    /// </summary>
    public partial class DuplicateFinderViewModel : ObservableObject
    {
        public static readonly IReadOnlyList<SizeOption> SizeOptions = new[]
        {
            new SizeOption("Any size", 1),
            new SizeOption("100 KB and larger", 100L * 1024),
            new SizeOption("1 MB and larger", 1024L * 1024),
            new SizeOption("10 MB and larger", 10L * 1024 * 1024),
            new SizeOption("100 MB and larger", 100L * 1024 * 1024),
        };

        public static readonly IReadOnlyList<KeepRule> KeepRules = new[]
        {
            new KeepRule("Keep the newest copy", DuplicateKeepStrategy.KeepNewest),
            new KeepRule("Keep the oldest copy", DuplicateKeepStrategy.KeepOldest),
            new KeepRule("Keep the shortest path", DuplicateKeepStrategy.KeepShortestPath),
            new KeepRule("Keep the copy in the first folder listed", DuplicateKeepStrategy.KeepInFolder),
        };

        private readonly IDuplicateFinderService? _finder;
        private CancellationTokenSource? _cts;

        public DuplicateFinderViewModel()
        {
            StatusMessage = "Design-time";
            FilesView = CreateView();
        }

        public DuplicateFinderViewModel(IDuplicateFinderService? finder)
        {
            _finder = finder;
            FilesView = CreateView();
        }

        public ObservableCollection<string> Folders { get; } = new();

        public ObservableCollection<DuplicateRow> Files { get; } = new();

        /// <summary>Files grouped by duplicate set (group header shows copies and wasted space).</summary>
        public ICollectionView FilesView { get; }

        [ObservableProperty]
        private string _newFolder = string.Empty;

        [ObservableProperty]
        private string? _selectedFolder;

        [ObservableProperty]
        private SizeOption _selectedSize = SizeOptions[1];

        [ObservableProperty]
        private KeepRule _selectedRule = KeepRules[0];

        [ObservableProperty]
        private bool _includeHidden;

        [ObservableProperty]
        private string _extensions = string.Empty;

        [ObservableProperty]
        private DuplicateRow? _selectedFile;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private string _summary = string.Empty;

        [ObservableProperty]
        private string _markedSummary = "0 marked";

        [ObservableProperty]
        private string _statusMessage = "Add one or more folders, then click Find Duplicates. Nothing is deleted until you confirm.";

        public int GroupCount => Files.Select(f => f.Group).Distinct().Count();

        private readonly object _filesLock = new();

        private ICollectionView CreateView()
        {
            BindingOperations.EnableCollectionSynchronization(Files, _filesLock); // results may arrive off the UI thread
            var view = CollectionViewSource.GetDefaultView(Files);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DuplicateRow.GroupHeader)));
            return view;
        }

        [RelayCommand]
        private void AddFolder()
        {
            var path = NewFolder.Trim().Trim('"');
            if (path.Length == 0)
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Choose folders to search for duplicates", Multiselect = true };
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                foreach (var folder in dialog.FolderNames)
                {
                    TryAdd(folder);
                }

                return;
            }

            if (TryAdd(path))
            {
                NewFolder = string.Empty;
            }
        }

        private bool TryAdd(string path)
        {
            if (!Directory.Exists(path))
            {
                StatusMessage = $"Folder not found: {path}";
                return false;
            }

            var full = Path.GetFullPath(path).TrimEnd('\\');
            if (full.Length == 2 && full[1] == ':')
            {
                full += "\\";
            }

            if (Folders.Any(f => string.Equals(f, full, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = $"Already in the list: {full}";
                return false;
            }

            Folders.Add(full);
            StatusMessage = $"{Folders.Count} folder(s) to search. Click Find Duplicates.";
            return true;
        }

        [RelayCommand]
        private void RemoveFolder()
        {
            if (SelectedFolder != null)
            {
                Folders.Remove(SelectedFolder);
            }
        }

        [RelayCommand]
        private async Task ScanAsync()
        {
            if (_finder == null)
            {
                StatusMessage = "Duplicate finder service is not available";
                return;
            }

            if (!string.IsNullOrWhiteSpace(NewFolder))
            {
                AddFolder();
            }

            if (Folders.Count == 0)
            {
                StatusMessage = "Add a folder to search first";
                return;
            }

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;
            IsBusy = true;
            Progress = 0;
            Files.Clear();
            Summary = string.Empty;
            UpdateMarked();
            StatusMessage = "Listing files...";

            var options = new DuplicateScanOptions
            {
                MinFileSize = SelectedSize.Bytes,
                IncludeHiddenFiles = IncludeHidden,
                IncludeSystemFiles = false,
                VerifyWithFullHash = true,
                IncludeExtensions = ParseExtensions(Extensions),
            };
            var progress = new Progress<Winhance.Core.Features.FileManager.Interfaces.DuplicateScanProgress>(p =>
            {
                Progress = p.PercentComplete;
                StatusMessage = p.TotalFiles > 0
                    ? $"{p.Phase}: {p.FilesProcessed:N0} of {p.TotalFiles:N0} candidates ({p.CurrentFile})"
                    : p.Phase + "...";
            });

            try
            {
                var folders = Folders.ToList();
                var result = await Task.Run(() => _finder.ScanForDuplicatesAsync(folders, options, progress, ct), ct);
                var index = 0;
                foreach (var group in result.Groups)
                {
                    index++;
                    foreach (var entry in group.Files.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase))
                    {
                        Files.Add(new DuplicateRow(group, entry, index, UpdateMarked));
                    }
                }

                Progress = 100;
                OnPropertyChanged(nameof(GroupCount));
                Summary = result.Groups.Count == 0
                    ? "No duplicates"
                    : $"{result.Groups.Count:N0} {(result.Groups.Count == 1 ? "set" : "sets")} · {result.DuplicateCount:N0} extra {(result.DuplicateCount == 1 ? "copy" : "copies")} · {result.FormattedWastedSpace} reclaimable";
                StatusMessage = ct.IsCancellationRequested
                    ? "Scan cancelled; showing what was found so far."
                    : result.Groups.Count == 0
                        ? $"Checked {result.TotalFilesScanned:N0} files: no duplicates found."
                        : $"Checked {result.TotalFilesScanned:N0} files in {result.ScanDuration.TotalSeconds:0.0}s. Choose a keep rule and click Mark, or tick copies yourself.";
                FilesView.Refresh();
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Scan cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Scan failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            _cts?.Cancel();
        }

        /// <summary>Mark every copy except the one the keep rule picks, in every set.</summary>
        [RelayCommand]
        private void ApplyRule()
        {
            foreach (var set in Files.GroupBy(f => f.Group))
            {
                var keeper = PickKeeper(set.ToList(), SelectedRule.Strategy);
                foreach (var row in set)
                {
                    row.SetMarked(!ReferenceEquals(row, keeper));
                }
            }

            UpdateMarked();
            StatusMessage = Files.Count == 0 ? "Nothing to mark yet: run a scan first." : $"{SelectedRule.Label}: marked {Files.Count(f => f.IsMarked):N0} copies. Review, then click Delete Marked.";
        }

        [RelayCommand]
        private void ClearMarks()
        {
            foreach (var row in Files)
            {
                row.SetMarked(false);
            }

            UpdateMarked();
        }

        private DuplicateRow PickKeeper(List<DuplicateRow> set, DuplicateKeepStrategy strategy) => strategy switch
        {
            DuplicateKeepStrategy.KeepNewest => set.OrderByDescending(r => r.Entry.ModifiedTime).First(),
            DuplicateKeepStrategy.KeepOldest => set.OrderBy(r => r.Entry.ModifiedTime).First(),
            DuplicateKeepStrategy.KeepShortestPath => set.OrderBy(r => r.Entry.Path.Length).ThenBy(r => r.Entry.Path, StringComparer.OrdinalIgnoreCase).First(),
            DuplicateKeepStrategy.KeepInFolder => set.OrderBy(r => FolderRank(r.Entry.Path)).ThenBy(r => r.Entry.Path, StringComparer.OrdinalIgnoreCase).First(),
            _ => set[0],
        };

        private int FolderRank(string path)
        {
            for (var i = 0; i < Folders.Count; i++)
            {
                var root = Folders[i].EndsWith('\\') ? Folders[i] : Folders[i] + "\\";
                if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        [RelayCommand]
        private async Task DeleteMarkedAsync()
        {
            if (_finder == null)
            {
                return;
            }

            var marked = Files.Where(f => f.IsMarked).ToList();
            if (marked.Count == 0)
            {
                StatusMessage = "Nothing is marked. Use Mark (keep rule) or tick the copies to remove.";
                return;
            }

            var allMarkedSets = Files.GroupBy(f => f.Group).Count(g => g.All(r => r.IsMarked));
            var bytes = marked.Sum(r => r.Group.FileSize);
            var message = $"Move {marked.Count:N0} duplicate file(s) ({FormatBytes(bytes)}) to the Recycle Bin?\n\n" +
                          "Every file is re-checked right before it is removed: it must still match the copy being kept byte for byte.\n" +
                          (allMarkedSets > 0 ? $"\n{allMarkedSets} set(s) have every copy marked; those are skipped so one copy always remains.\n" : string.Empty) +
                          "\nYou can restore them from the Recycle Bin.";
            if (System.Windows.MessageBox.Show(message, "Delete duplicates", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) != System.Windows.MessageBoxResult.Yes)
            {
                StatusMessage = "Cancelled - nothing was deleted";
                return;
            }

            IsBusy = true;
            StatusMessage = "Re-checking and moving marked copies to the Recycle Bin...";
            try
            {
                var groups = Files.Select(f => f.Group).Distinct().ToList();
                var result = await Task.Run(() => _finder.DeleteMarkedAsync(groups, useRecycleBin: true));
                var deleted = new HashSet<string>(result.DeletedPaths, StringComparer.OrdinalIgnoreCase);
                foreach (var row in Files.Where(r => deleted.Contains(r.Entry.Path)).ToList())
                {
                    row.Group.Files.Remove(row.Entry);
                    Files.Remove(row);
                }

                // A set with one file left is no longer a duplicate.
                foreach (var row in Files.GroupBy(f => f.Group).Where(g => g.Count() < 2).SelectMany(g => g).ToList())
                {
                    Files.Remove(row);
                }

                foreach (var row in Files)
                {
                    row.Refresh();
                }

                OnPropertyChanged(nameof(GroupCount));
                UpdateMarked();
                FilesView.Refresh();
                Summary = GroupCount == 0 ? "No duplicates left" : $"{GroupCount:N0} sets left";
                StatusMessage = $"Moved {result.DeletedCount:N0} file(s) to the Recycle Bin, freed {FormatBytes(result.BytesRecovered)}" +
                                (result.SkippedCount > 0 ? $"; skipped {result.SkippedCount} (safety checks)" : string.Empty) +
                                (result.FailedCount > 0 ? $"; {result.FailedCount} failed" : string.Empty) + ".";
                if (result.Errors.Count > 0)
                {
                    System.Windows.MessageBox.Show(string.Join("\n", result.Errors.Take(20)) + (result.Errors.Count > 20 ? $"\n...and {result.Errors.Count - 20} more" : string.Empty), "Some files were not deleted", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenLocation(DuplicateRow? row)
        {
            row ??= SelectedFile;
            if (row == null || !File.Exists(row.Entry.Path))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{row.Entry.Path}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not open Explorer: {ex.Message}";
            }
        }

        private void UpdateMarked()
        {
            var marked = Files.Where(f => f.IsMarked).ToList();
            MarkedSummary = marked.Count == 0 ? "0 marked" : $"{marked.Count:N0} marked · {FormatBytes(marked.Sum(r => r.Group.FileSize))}";
        }

        private static HashSet<string>? ParseExtensions(string text)
        {
            var set = text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().TrimStart('*').TrimStart('.').ToLowerInvariant())
                .Where(e => e.Length > 0)
                .ToHashSet();
            return set.Count == 0 ? null : set;
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

            return $"{size:0.##} {sizes[order]}";
        }
    }

    public sealed record SizeOption(string Label, long Bytes);

    public sealed record KeepRule(string Label, DuplicateKeepStrategy Strategy);

    /// <summary>One file in a duplicate set.</summary>
    public partial class DuplicateRow : ObservableObject
    {
        private readonly Action _onMarkedChanged;
        private readonly int _index;

        public DuplicateRow(DuplicateFileGroup group, DuplicateFileEntry entry, int index, Action onMarkedChanged)
        {
            Group = group;
            Entry = entry;
            _index = index;
            _onMarkedChanged = onMarkedChanged;
            entry.MarkedForDeletion = false;
        }

        public DuplicateFileGroup Group { get; }

        public DuplicateFileEntry Entry { get; }

        public string Name => Entry.Name;

        public string Folder => Entry.Directory;

        public string SizeText => Group.FormattedSize;

        public DateTime Modified => Entry.ModifiedTime;

        public string GroupHeader => $"Set {_index}: {Group.Files.Count} copies of {Group.FormattedSize} - {Group.FormattedWasted} reclaimable  ({Group.Hash[..Math.Min(12, Group.Hash.Length)].ToLowerInvariant()})";

        [ObservableProperty]
        private bool _isMarked;

        private bool _bulkUpdate;

        partial void OnIsMarkedChanged(bool value)
        {
            Entry.MarkedForDeletion = value;
            if (!_bulkUpdate)
            {
                _onMarkedChanged();
            }
        }

        /// <summary>Change the mark without recomputing the totals (the caller does that once).</summary>
        internal void SetMarked(bool value)
        {
            _bulkUpdate = true;
            try
            {
                IsMarked = value;
            }
            finally
            {
                _bulkUpdate = false;
            }
        }

        internal void Refresh() => OnPropertyChanged(nameof(GroupHeader));
    }
}
