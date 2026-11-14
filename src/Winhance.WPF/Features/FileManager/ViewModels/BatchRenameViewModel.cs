using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// ViewModel for the batch rename feature.
    /// </summary>
    public partial class BatchRenameViewModel : ObservableObject
    {
        private readonly IBatchRenameService? _batchRenameService;

        [ObservableProperty]
        private string _sourcePath = string.Empty;

        [ObservableProperty]
        private string _filterPattern = "*.*";

        [ObservableProperty]
        private bool _includeSubfolders;

        [ObservableProperty]
        private ObservableCollection<string> _selectedFiles = new();

        [ObservableProperty]
        private ObservableCollection<RenameRuleViewModel> _rules = new();

        [ObservableProperty]
        private ObservableCollection<RenamePreviewItemViewModel> _previewItems = new();

        [ObservableProperty]
        private ObservableCollection<RenamePresetViewModel> _presets = new();

        [ObservableProperty]
        private RenamePresetViewModel? _selectedPreset;

        [ObservableProperty]
        private int _totalFiles;

        [ObservableProperty]
        private int _conflictCount;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private string? _lastTransactionId;

        [ObservableProperty]
        private bool _canUndo;

        public BatchRenameViewModel()
        {
            // Design-time constructor
            LoadDesignTimeData();
        }

        public BatchRenameViewModel(IBatchRenameService? batchRenameService)
        {
            _batchRenameService = batchRenameService;
            Rules.CollectionChanged += OnRulesCollectionChanged;
            _ = LoadPresetsAsync();
        }

        private CancellationTokenSource? _previewDebounce;

        // Typing a path (or changing the filter / subfolder option) reloads the file list and preview.
        partial void OnSourcePathChanged(string value) => ReloadFilesAndPreview();

        partial void OnFilterPatternChanged(string value) => ReloadFilesAndPreview();

        partial void OnIncludeSubfoldersChanged(bool value) => ReloadFilesAndPreview();

        private System.Collections.Generic.Dictionary<string, string>? _lastRenameMap;

        /// <summary>
        /// After a rename or undo the old paths no longer exist. Re-read the source folder when there is one;
        /// otherwise map the individually added files to their new paths.
        /// </summary>
        private void ReloadFileList(System.Collections.Generic.IReadOnlyDictionary<string, string>? pathMap)
        {
            if (!string.IsNullOrWhiteSpace(SourcePath) && System.IO.Directory.Exists(SourcePath))
            {
                LoadFilesFromSource();
            }
            else
            {
                var updated = SelectedFiles
                    .Select(p => pathMap != null && pathMap.TryGetValue(p, out var mapped) ? mapped : p)
                    .Where(System.IO.File.Exists)
                    .ToList();
                SelectedFiles.Clear();
                foreach (var p in updated)
                {
                    SelectedFiles.Add(p);
                }

                TotalFiles = SelectedFiles.Count;
            }

            SchedulePreviewRefresh();
        }

        private void ReloadFilesAndPreview()
        {
            if (string.IsNullOrWhiteSpace(SourcePath))
            {
                return;
            }

            if (!System.IO.Directory.Exists(SourcePath))
            {
                SelectedFiles.Clear();
                PreviewItems.Clear();
                TotalFiles = 0;
                StatusMessage = $"Folder not found: {SourcePath}";
                return;
            }

            LoadFilesFromSource();
            SchedulePreviewRefresh();
        }

        private void OnRulesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (RenameRuleViewModel rule in e.OldItems)
                {
                    rule.PropertyChanged -= OnRulePropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (RenameRuleViewModel rule in e.NewItems)
                {
                    rule.PropertyChanged += OnRulePropertyChanged;
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Clear() does not report old items; nothing to unsubscribe from cleared rules that are gone.
                SchedulePreviewRefresh();
            }
        }

        // Editing a rule (find/replace text, enabled, etc.) updates the live preview.
        private void OnRulePropertyChanged(object? sender, PropertyChangedEventArgs e) => SchedulePreviewRefresh();

        /// <summary>Debounced preview refresh so typing in a rule doesn't rescan on every keystroke.</summary>
        private void SchedulePreviewRefresh()
        {
            _previewDebounce?.Cancel();
            var cts = new CancellationTokenSource();
            _previewDebounce = cts;
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                if (dispatcher == null)
                {
                    await RefreshPreviewAsync();
                    return;
                }

                await dispatcher.InvokeAsync(async () =>
                {
                    if (!cts.IsCancellationRequested)
                    {
                        await RefreshPreviewAsync();
                    }
                });
            });
        }

        private void LoadDesignTimeData()
        {
            PreviewItems.Add(new RenamePreviewItemViewModel
            {
                OriginalName = "IMG_20260115_001.jpg",
                NewName = "Vacation_001.jpg",
                HasConflict = false,
            });
            PreviewItems.Add(new RenamePreviewItemViewModel
            {
                OriginalName = "IMG_20260115_002.jpg",
                NewName = "Vacation_002.jpg",
                HasConflict = false,
            });

            Rules.Add(new RenameRuleViewModel { RuleType = RenameRuleType.FindReplace, FindText = "IMG_", ReplaceText = "Vacation_" });
        }

        [RelayCommand]
        private void BrowseSource()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Source Folder",
                InitialDirectory = string.IsNullOrEmpty(SourcePath)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    : SourcePath,
            };

            if (dialog.ShowDialog() == true)
            {
                SourcePath = dialog.FolderName;
                LoadFilesFromSource();
            }
        }

        [RelayCommand]
        private void BrowseFiles()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Files to Rename",
                Multiselect = true,
                Filter = "All Files (*.*)|*.*",
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    if (!SelectedFiles.Contains(file))
                    {
                        SelectedFiles.Add(file);
                    }
                }
                TotalFiles = SelectedFiles.Count;
                _ = RefreshPreviewAsync();
            }
        }

        private void LoadFilesFromSource()
        {
            if (string.IsNullOrEmpty(SourcePath) || !System.IO.Directory.Exists(SourcePath))
            {
                return;
            }

            SelectedFiles.Clear();
            var searchOption = IncludeSubfolders
                ? System.IO.SearchOption.AllDirectories
                : System.IO.SearchOption.TopDirectoryOnly;

            try
            {
                var pattern = string.IsNullOrWhiteSpace(FilterPattern) ? "*.*" : FilterPattern.Trim();
                var files = System.IO.Directory.GetFiles(SourcePath, pattern, searchOption);
                foreach (var file in files)
                {
                    SelectedFiles.Add(file);
                }
                TotalFiles = SelectedFiles.Count;
                StatusMessage = $"Loaded {TotalFiles} files";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading files: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task LoadPresetsAsync()
        {
            if (_batchRenameService == null)
            {
                return;
            }

            Presets.Clear();
            var presets = await _batchRenameService.GetPresetsAsync();
            foreach (var preset in presets)
            {
                Presets.Add(new RenamePresetViewModel
                {
                    Name = preset.Name,
                    Description = preset.Description,
                    IsBuiltIn = preset.IsBuiltIn,
                    Rules = preset.Rules.Select(r => new RenameRuleViewModel
                    {
                        RuleType = r.Type,
                        FindText = r.FindText,
                        ReplaceText = r.ReplaceText,
                        UseRegex = r.UseRegex,
                    }).ToList(),
                });
            }
        }

        [RelayCommand]
        private void AddRule(string? ruleTypeString)
        {
            if (string.IsNullOrEmpty(ruleTypeString))
            {
                return;
            }

            if (!Enum.TryParse<RenameRuleType>(ruleTypeString, out var ruleType))
            {
                return;
            }

            var rule = new RenameRuleViewModel
            {
                RuleType = ruleType,
                Order = Rules.Count + 1,
                Enabled = true,
            };

            // Defaults that produce a sensible result as soon as the rule is added.
            switch (ruleType)
            {
                case RenameRuleType.Counter:
                    rule.Position = TextPosition.Suffix;
                    rule.CounterPrefix = "_";
                    break;
                case RenameRuleType.DateTime:
                    rule.Position = TextPosition.Prefix;
                    break;
                case RenameRuleType.ChangeCase:
                    rule.CaseType = CaseType.TitleCase;
                    break;
            }

            Rules.Add(rule);
            _ = RefreshPreviewAsync();
        }

        [RelayCommand]
        private void RemoveRule(RenameRuleViewModel rule)
        {
            Rules.Remove(rule);
            ReorderRules();
            _ = RefreshPreviewAsync();
        }

        [RelayCommand]
        private void MoveRuleUp(RenameRuleViewModel rule)
        {
            var index = Rules.IndexOf(rule);
            if (index > 0)
            {
                Rules.Move(index, index - 1);
                ReorderRules();
                _ = RefreshPreviewAsync();
            }
        }

        [RelayCommand]
        private void MoveRuleDown(RenameRuleViewModel rule)
        {
            var index = Rules.IndexOf(rule);
            if (index < Rules.Count - 1)
            {
                Rules.Move(index, index + 1);
                ReorderRules();
                _ = RefreshPreviewAsync();
            }
        }

        private void ReorderRules()
        {
            for (int i = 0; i < Rules.Count; i++)
            {
                Rules[i].Order = i + 1;
            }
        }

        [RelayCommand]
        private void ClearAllRules()
        {
            Rules.Clear();
            PreviewItems.Clear();
            ConflictCount = 0;
        }

        [RelayCommand]
        private void LoadPreset(RenamePresetViewModel preset)
        {
            Rules.Clear();
            foreach (var rule in preset.Rules)
            {
                Rules.Add(new RenameRuleViewModel
                {
                    RuleType = rule.RuleType,
                    Order = Rules.Count + 1,
                    Enabled = rule.Enabled,
                    FindText = rule.FindText,
                    ReplaceText = rule.ReplaceText,
                    UseRegex = rule.UseRegex,
                    TextToAdd = rule.TextToAdd,
                    Position = rule.Position,
                    CaseType = rule.CaseType,
                    CounterStart = rule.CounterStart,
                    CounterStep = rule.CounterStep,
                    CounterPadding = rule.CounterPadding,
                });
            }

            _ = RefreshPreviewAsync();
        }

        [RelayCommand]
        public async Task RefreshPreviewAsync()
        {
            if (SelectedFiles.Count == 0 || Rules.Count == 0)
            {
                PreviewItems.Clear();
                ConflictCount = 0;
                return;
            }

            IsLoading = true;
            StatusMessage = "Generating preview...";

            try
            {
                if (_batchRenameService != null)
                {
                    var rules = Rules.Where(r => r.Enabled).OrderBy(r => r.Order).Select(r => r.ToRule()).ToList();

                    var previews = await _batchRenameService.PreviewRenameAsync(SelectedFiles, rules);

                    PreviewItems.Clear();
                    ConflictCount = 0;

                    foreach (var preview in previews)
                    {
                        var item = new RenamePreviewItemViewModel
                        {
                            Index = PreviewItems.Count + 1,
                            OriginalName = preview.OriginalName,
                            OriginalPath = preview.OriginalPath,
                            NewName = preview.NewName,
                            NewPath = preview.NewPath,
                            HasConflict = preview.HasConflict,
                            ConflictReason = preview.ConflictReason,
                            WillChange = preview.WillChange,
                        };
                        PreviewItems.Add(item);

                        if (preview.HasConflict)
                        {
                            ConflictCount++;
                        }
                    }
                }
                else
                {
                    // Fallback preview generation
                    await GenerateFallbackPreviewAsync();
                }

                TotalFiles = PreviewItems.Count;
                StatusMessage = ConflictCount > 0
                    ? $"{ConflictCount} conflicts detected"
                    : $"{TotalFiles} files ready to rename";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GenerateFallbackPreviewAsync()
        {
            await Task.Run(() =>
            {
                var enabledRules = Rules.Where(r => r.Enabled).OrderBy(r => r.Order).ToList();
                var newNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int counter = enabledRules.FirstOrDefault(r => r.RuleType == RenameRuleType.Counter)?.CounterStart ?? 1;

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    PreviewItems.Clear();
                    ConflictCount = 0;
                });

                foreach (var filePath in SelectedFiles)
                {
                    var originalName = System.IO.Path.GetFileName(filePath);
                    var directory = System.IO.Path.GetDirectoryName(filePath) ?? string.Empty;
                    var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(originalName);
                    var extension = System.IO.Path.GetExtension(originalName);

                    var newName = nameWithoutExt;
                    var newExt = extension;

                    // Apply each rule in order
                    foreach (var rule in enabledRules)
                    {
                        switch (rule.RuleType)
                        {
                            case RenameRuleType.FindReplace:
                                if (!string.IsNullOrEmpty(rule.FindText))
                                {
                                    if (rule.UseRegex)
                                    {
                                        try
                                        {
                                            newName = System.Text.RegularExpressions.Regex.Replace(
                                                newName, rule.FindText, rule.ReplaceText ?? string.Empty);
                                        }
                                        catch (ArgumentException)
                                        {
                                            // Invalid regular expression typed by the user: leave the name unchanged.
                                        }
                                    }
                                    else
                                    {
                                        newName = newName.Replace(rule.FindText, rule.ReplaceText ?? string.Empty,
                                            rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                                    }
                                }
                                break;

                            case RenameRuleType.AddText:
                                if (!string.IsNullOrEmpty(rule.TextToAdd))
                                {
                                    newName = rule.Position == TextPosition.Prefix
                                        ? rule.TextToAdd + newName
                                        : newName + rule.TextToAdd;
                                }
                                break;

                            case RenameRuleType.RemoveText:
                                if (!string.IsNullOrEmpty(rule.FindText))
                                {
                                    newName = newName.Replace(rule.FindText, string.Empty);
                                }
                                break;

                            case RenameRuleType.Counter:
                                var counterStr = counter.ToString().PadLeft(rule.CounterPadding, '0');
                                newName = rule.Position == TextPosition.Prefix
                                    ? $"{rule.CounterPrefix}{counterStr}{rule.CounterSuffix}{newName}"
                                    : $"{newName}{rule.CounterPrefix}{counterStr}{rule.CounterSuffix}";
                                counter += rule.CounterStep;
                                break;

                            case RenameRuleType.ChangeCase:
                                newName = rule.CaseType switch
                                {
                                    CaseType.UpperCase => newName.ToUpperInvariant(),
                                    CaseType.LowerCase => newName.ToLowerInvariant(),
                                    CaseType.TitleCase => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(newName.ToLowerInvariant()),
                                    _ => newName,
                                };
                                break;

                            case RenameRuleType.ChangeExtension:
                                if (!string.IsNullOrEmpty(rule.NewExtension))
                                {
                                    newExt = rule.NewExtension.StartsWith(".") ? rule.NewExtension : "." + rule.NewExtension;
                                }
                                break;

                            case RenameRuleType.DateTime:
                                var fileInfo = new System.IO.FileInfo(filePath);
                                var dateStr = fileInfo.LastWriteTime.ToString(rule.DateFormat);
                                newName = rule.Position == TextPosition.Prefix
                                    ? $"{dateStr}_{newName}"
                                    : $"{newName}_{dateStr}";
                                break;
                        }
                    }

                    var finalNewName = newName + newExt;
                    var newPath = System.IO.Path.Combine(directory, finalNewName);
                    var hasConflict = newNames.Contains(finalNewName) ||
                                     (finalNewName != originalName && System.IO.File.Exists(newPath));
                    var conflictReason = hasConflict ? "Name already exists" : string.Empty;

                    newNames.Add(finalNewName);

                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        PreviewItems.Add(new RenamePreviewItemViewModel
                        {
                            OriginalName = originalName,
                            OriginalPath = filePath,
                            NewName = finalNewName,
                            NewPath = newPath,
                            HasConflict = hasConflict,
                            ConflictReason = conflictReason,
                            WillChange = finalNewName != originalName,
                        });

                        if (hasConflict)
                        {
                            ConflictCount++;
                        }
                    });
                }
            });
        }

        [RelayCommand]
        private async Task ApplyRenameAsync()
        {
            if (SelectedFiles.Count == 0 || Rules.Count == 0)
            {
                return;
            }

            if (ConflictCount > 0)
            {
                StatusMessage = "Cannot rename: conflicts exist";
                return;
            }

            if (_batchRenameService == null)
            {
                await ApplyRenameFallbackAsync();
                return;
            }

            // Service-based implementation continues below
            if (ConflictCount > 0)
            {
                StatusMessage = "Cannot rename: conflicts exist";
                return;
            }

            IsLoading = true;
            StatusMessage = "Renaming files...";
            _lastRenameMap = PreviewItems
                .Where(p => !string.IsNullOrEmpty(p.OriginalPath) && !string.IsNullOrEmpty(p.NewPath))
                .GroupBy(p => p.OriginalPath, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().NewPath, StringComparer.OrdinalIgnoreCase);

            try
            {
                var rules = Rules.Where(r => r.Enabled).OrderBy(r => r.Order).Select(r => r.ToRule()).ToList();

                var result = await _batchRenameService.ExecuteRenameAsync(SelectedFiles, rules);

                if (result.Success)
                {
                    LastTransactionId = result.TransactionId;
                    CanUndo = true;
                    StatusMessage = $"Successfully renamed {result.FilesRenamed} files";
                    ReloadFileList(_lastRenameMap);
                }
                else
                {
                    StatusMessage = $"Renamed {result.FilesRenamed}, failed {result.FilesFailed}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ApplyRenameFallbackAsync()
        {
            IsLoading = true;
            StatusMessage = "Renaming files...";
            int renamed = 0;
            int failed = 0;
            var undoLog = new System.Collections.Generic.List<(string oldPath, string newPath)>();

            try
            {
                foreach (var preview in PreviewItems.Where(p => p.WillChange && !p.HasConflict))
                {
                    try
                    {
                        if (System.IO.File.Exists(preview.OriginalPath))
                        {
                            System.IO.File.Move(preview.OriginalPath, preview.NewPath);
                            undoLog.Add((preview.NewPath, preview.OriginalPath));
                            renamed++;
                        }
                    }
                    catch
                    {
                        failed++;
                    }
                }

                if (renamed > 0)
                {
                    // Store undo log for potential undo
                    _undoLog = undoLog;
                    CanUndo = true;
                    LastTransactionId = Guid.NewGuid().ToString();
                }

                StatusMessage = failed == 0
                    ? $"Successfully renamed {renamed} files"
                    : $"Renamed {renamed}, failed {failed}";

                ReloadFileList(_lastRenameMap);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private System.Collections.Generic.List<(string oldPath, string newPath)>? _undoLog;

        [RelayCommand]
        private async Task UndoLastBatchAsync()
        {
            // First try fallback undo
            if (_undoLog != null && _undoLog.Count > 0)
            {
                await UndoFallbackAsync();
                return;
            }

            if (_batchRenameService == null || string.IsNullOrEmpty(LastTransactionId))
            {
                return;
            }

            IsLoading = true;
            StatusMessage = "Undoing rename...";

            try
            {
                var result = await _batchRenameService!.UndoRenameAsync(LastTransactionId!);

                if (result.Success)
                {
                    StatusMessage = $"Successfully restored {result.FilesRenamed} files";
                    LastTransactionId = null;
                    CanUndo = false;
                    ReloadFileList(_lastRenameMap?.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    StatusMessage = $"Undo failed: restored {result.FilesRenamed}, failed {result.FilesFailed}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UndoFallbackAsync()
        {
            IsLoading = true;
            StatusMessage = "Undoing rename...";
            int restored = 0;
            int failed = 0;

            try
            {
                if (_undoLog != null)
                {
                    foreach (var (oldPath, newPath) in _undoLog)
                    {
                        try
                        {
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Move(oldPath, newPath);
                                restored++;
                            }
                        }
                        catch
                        {
                            failed++;
                        }
                    }
                }

                StatusMessage = failed == 0
                    ? $"Successfully restored {restored} files"
                    : $"Restored {restored}, failed {failed}";

                _undoLog = null;
                LastTransactionId = null;
                CanUndo = false;
                ReloadFileList(_lastRenameMap?.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Undo error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }

            await Task.CompletedTask;
        }

        [RelayCommand]
        private void AddFiles(System.Collections.IList files)
        {
            foreach (string file in files)
            {
                if (!SelectedFiles.Contains(file))
                {
                    SelectedFiles.Add(file);
                }
            }

            TotalFiles = SelectedFiles.Count;
            _ = RefreshPreviewAsync();
        }

        [RelayCommand]
        private void ClearFiles()
        {
            SelectedFiles.Clear();
            PreviewItems.Clear();
            TotalFiles = 0;
            ConflictCount = 0;
        }
    }

    /// <summary>
    /// ViewModel for a rename rule.
    /// </summary>
    public partial class RenameRuleViewModel : ObservableObject
    {
        [ObservableProperty]
        private RenameRuleType _ruleType;

        [ObservableProperty]
        private int _order;

        [ObservableProperty]
        private bool _enabled = true;

        [ObservableProperty]
        private string _findText = string.Empty;

        [ObservableProperty]
        private string _replaceText = string.Empty;

        [ObservableProperty]
        private bool _useRegex;

        [ObservableProperty]
        private bool _caseSensitive;

        [ObservableProperty]
        private string _textToAdd = string.Empty;

        [ObservableProperty]
        private TextPosition _position;

        [ObservableProperty]
        private int _positionIndex;

        [ObservableProperty]
        private CaseType _caseType;

        [ObservableProperty]
        private int _counterStart = 1;

        [ObservableProperty]
        private int _counterStep = 1;

        [ObservableProperty]
        private int _counterPadding = 3;

        [ObservableProperty]
        private string _counterPrefix = string.Empty;

        [ObservableProperty]
        private string _counterSuffix = string.Empty;

        [ObservableProperty]
        private string _newExtension = string.Empty;

        [ObservableProperty]
        private string _dateFormat = "yyyy-MM-dd";

        [ObservableProperty]
        private DateSource _dateSource = DateSource.FileModified;

        [ObservableProperty]
        private string _removePattern = string.Empty;

        public static TextPosition[] PositionOptions { get; } = { TextPosition.Prefix, TextPosition.Suffix, TextPosition.AtIndex };

        public static CaseType[] CaseOptions { get; } = (CaseType[])Enum.GetValues(typeof(CaseType));

        public static DateSource[] DateSourceOptions { get; } = { DateSource.FileModified, DateSource.FileCreated, DateSource.CurrentDate };

        /// <summary>Builds the backend rule with every setting the user can edit.</summary>
        public RenameRule ToRule() => new()
        {
            Type = RuleType,
            Order = Order,
            Enabled = Enabled,
            FindText = FindText,
            ReplaceText = ReplaceText,
            UseRegex = UseRegex || RuleType == RenameRuleType.RegularExpression,
            CaseSensitive = CaseSensitive,
            TextToAdd = TextToAdd,
            Position = Position,
            PositionIndex = PositionIndex,
            RemovePattern = RemovePattern,
            CaseType = CaseType,
            CounterStart = CounterStart,
            CounterStep = CounterStep,
            CounterPadding = CounterPadding,
            CounterPrefix = CounterPrefix,
            CounterSuffix = CounterSuffix,
            NewExtension = NewExtension,
            DateFormat = DateFormat,
            DateSource = DateSource,
        };

        public string RuleTypeDisplay => RuleType switch
        {
            RenameRuleType.FindReplace => "Find & Replace",
            RenameRuleType.AddText => "Add Text",
            RenameRuleType.RemoveText => "Remove Text",
            RenameRuleType.Counter => "Counter",
            RenameRuleType.ChangeCase => "Change Case",
            RenameRuleType.ChangeExtension => "Change Extension",
            RenameRuleType.DateTime => "Date/Time",
            RenameRuleType.MetadataExtract => "Metadata",
            RenameRuleType.RegularExpression => "Regex",
            _ => "Unknown",
        };
    }

    /// <summary>
    /// ViewModel for a rename preview item.
    /// </summary>
    public partial class RenamePreviewItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _index;

        [ObservableProperty]
        private string _originalName = string.Empty;

        [ObservableProperty]
        private string _originalPath = string.Empty;

        [ObservableProperty]
        private string _newName = string.Empty;

        [ObservableProperty]
        private string _newPath = string.Empty;

        [ObservableProperty]
        private bool _hasConflict;

        [ObservableProperty]
        private string _conflictReason = string.Empty;

        [ObservableProperty]
        private bool _willChange;

        public string StatusIcon => HasConflict ? "AlertCircle" : (WillChange ? "Check" : "Minus");

        public string StatusColor => HasConflict ? "#FF6B6B" : (WillChange ? "#4ECDC4" : "#888888");
    }

    /// <summary>
    /// ViewModel for a rename preset.
    /// </summary>
    public partial class RenamePresetViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private bool _isBuiltIn;

        public System.Collections.Generic.List<RenameRuleViewModel> Rules { get; set; } = new();
    }
}
