using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// Environment &amp; PATH Doctor: scan, review problems, preview and apply repairs (with automatic backup,
    /// verification and rollback), resolve command conflicts, browse variables and restore backups.
    /// </summary>
    public partial class EnvironmentDoctorViewModel : ObservableObject
    {
        /// <summary>Fixes that are unambiguous and safe to pre-select. Deletions and judgement calls are opt-in.</summary>
        private static readonly HashSet<EnvIssueCode> Recommended = new()
        {
            EnvIssueCode.PathEntryMissing, EnvIssueCode.PathEntryDuplicate, EnvIssueCode.PathEntryDuplicateAcrossScopes,
            EnvIssueCode.PathEntryEmpty, EnvIssueCode.PathEntryQuoted, EnvIssueCode.PathEntryWhitespace, EnvIssueCode.PathEntryIsFile,
            EnvIssueCode.ExpandableValueStoredAsString, EnvIssueCode.EssentialSystemPathMissing, EnvIssueCode.PathExtMissingStandardExtensions,
            EnvIssueCode.CoreVariableMissing, EnvIssueCode.CoreVariableInvalid, EnvIssueCode.TempDirectoryInvalid,
            EnvIssueCode.HomeVariablePointsToBin, EnvIssueCode.WindowsAppsAliasShadowsRuntime, EnvIssueCode.PyLauncherVersionMissing,
        };

        private readonly IEnvironmentDoctorService? _doctor;
        private EnvDiagnosis? _diagnosis;

        public EnvironmentDoctorViewModel()
        {
            StatusMessage = "Design-time";
        }

        public EnvironmentDoctorViewModel(IEnvironmentDoctorService? doctor)
        {
            _doctor = doctor;
        }

        public ObservableCollection<EnvIssueItem> Issues { get; } = new();

        public ObservableCollection<EnvPathEntry> PathEntries { get; } = new();

        public ObservableCollection<EnvCommandItem> Commands { get; } = new();

        public ObservableCollection<EnvVariableItem> Variables { get; } = new();

        public ObservableCollection<EnvBackup> Backups { get; } = new();

        [ObservableProperty]
        private EnvIssueItem? _selectedIssue;

        [ObservableProperty]
        private EnvCommandItem? _selectedCommand;

        [ObservableProperty]
        private EnvBackup? _selectedBackup;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = "Click Scan to check your environment variables and PATH. Nothing changes until you choose Fix Selected.";

        [ObservableProperty]
        private string _summary = string.Empty;

        [ObservableProperty]
        private bool _isProblemsSection = true;

        [ObservableProperty]
        private bool _isPathSection;

        [ObservableProperty]
        private bool _isCommandsSection;

        [ObservableProperty]
        private bool _isVariablesSection;

        [ObservableProperty]
        private bool _isBackupsSection;

        [ObservableProperty]
        private bool _hasScanned;

        public int SelectedCount => Issues.Count(i => i.IsSelected);

        [RelayCommand]
        private async Task ScanAsync()
        {
            if (_doctor == null)
            {
                StatusMessage = "Environment Doctor is not available.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Scanning environment variables, PATH and installed tools...";
            try
            {
                _diagnosis = await _doctor.DiagnoseAsync();
                Populate(_diagnosis);
                LoadBackups();
                HasScanned = true;
                var d = _diagnosis;
                var errors = d.Issues.Count(i => i.Severity == EnvSeverity.Error);
                var warnings = d.Issues.Count(i => i.Severity == EnvSeverity.Warning);
                var info = d.Issues.Count(i => i.Severity == EnvSeverity.Info);
                Summary = $"{errors} error(s) · {warnings} warning(s) · {info} note(s)";
                StatusMessage = d.Issues.Count == 0
                    ? "No problems found."
                    : $"Found {d.Issues.Count} item(s); {SelectedCount} safe fix(es) pre-selected. Review, then click Fix Selected.";
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

        private void Populate(EnvDiagnosis d)
        {
            foreach (var old in Issues)
            {
                old.PropertyChanged -= OnIssuePropertyChanged;
            }

            Issues.Clear();
            foreach (var issue in d.Issues.OrderByDescending(i => i.Severity).ThenBy(i => i.Variable).ThenBy(i => i.Title))
            {
                var item = new EnvIssueItem(issue) { IsSelected = issue.Fix != null && Recommended.Contains(issue.Code) };
                item.PropertyChanged += OnIssuePropertyChanged;
                Issues.Add(item);
            }

            PathEntries.Clear();
            foreach (var e in d.PathEntries)
            {
                PathEntries.Add(e);
            }

            Commands.Clear();
            foreach (var c in d.Commands.OrderByDescending(c => c.AllMatches.Count).ThenBy(c => c.Command))
            {
                Commands.Add(new EnvCommandItem(c));
            }

            Variables.Clear();
            foreach (var v in d.Snapshot.Variables.OrderBy(v => v.Scope).ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
            {
                Variables.Add(new EnvVariableItem(v));
            }

            SelectedIssue = Issues.FirstOrDefault();
            SelectedCommand = Commands.FirstOrDefault(c => c.HasConflict) ?? Commands.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedCount));
        }

        private void OnIssuePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EnvIssueItem.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedCount));
            }
        }

        [RelayCommand]
        private void SelectRecommended()
        {
            foreach (var i in Issues)
            {
                i.IsSelected = i.CanFix && Recommended.Contains(i.Issue.Code);
            }
        }

        [RelayCommand]
        private void SelectAllFixable()
        {
            foreach (var i in Issues)
            {
                i.IsSelected = i.CanFix;
            }
        }

        [RelayCommand]
        private void SelectNone()
        {
            foreach (var i in Issues)
            {
                i.IsSelected = false;
            }
        }

        [RelayCommand]
        private async Task FixSelectedAsync()
        {
            if (_doctor == null || _diagnosis == null)
            {
                StatusMessage = "Scan first.";
                return;
            }

            var fixes = Issues.Where(i => i.IsSelected && i.Issue.Fix != null).Select(i => i.Issue.Fix!).ToList();
            if (fixes.Count == 0)
            {
                StatusMessage = "Select at least one problem that has a fix.";
                return;
            }

            await PreviewAndApplyAsync(_doctor.BuildPlan(_diagnosis.Snapshot, fixes), "Repair environment");
        }

        [RelayCommand]
        private async Task PreferMatchAsync(EnvCommandMatch? match)
        {
            if (_doctor == null || _diagnosis == null || match == null)
            {
                return;
            }

            var fixes = _doctor.CreatePreferFixes(_diagnosis, match.Command, match.Path);
            if (fixes.Count == 0)
            {
                StatusMessage = match.IsWinner
                    ? $"'{match.Command}' already runs {match.Path}."
                    : $"Cannot make {match.Path} win automatically (its folder is not on PATH in a movable position).";
                return;
            }

            await PreviewAndApplyAsync(_doctor.BuildPlan(_diagnosis.Snapshot, fixes), $"Make '{match.Command}' run {match.Path}");
        }

        private async Task PreviewAndApplyAsync(EnvRepairPlan plan, string title)
        {
            if (plan.Changes.Count == 0)
            {
                StatusMessage = plan.Refused.Count > 0 ? "Nothing to change: " + plan.Refused[0] : "Nothing to change.";
                if (plan.Refused.Count > 0)
                {
                    System.Windows.MessageBox.Show(string.Join("\n\n", plan.Refused), title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }

                return;
            }

            var text = new StringBuilder();
            text.AppendLine($"{plan.Changes.Count} variable(s) will change:");
            text.AppendLine();
            foreach (var c in plan.Changes.Take(12))
            {
                text.AppendLine(DescribeChange(c));
            }

            if (plan.Changes.Count > 12)
            {
                text.AppendLine($"... and {plan.Changes.Count - 12} more.");
            }

            if (plan.Refused.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("Skipped for safety:");
                foreach (var r in plan.Refused.Take(5))
                {
                    text.AppendLine("  • " + r);
                }
            }

            text.AppendLine();
            text.AppendLine("A full backup (JSON + .reg) is saved first. Every value is read back after writing; if anything does not match, all changes are rolled back automatically. Open terminals and apps must be restarted to see the change.");

            if (System.Windows.MessageBox.Show(text.ToString(), title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
            {
                StatusMessage = "Repair cancelled; nothing changed.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Backing up and applying...";
            try
            {
                var result = await _doctor!.ApplyAsync(plan);
                if (result.Success)
                {
                    await ScanAsync();
                    StatusMessage = $"Applied {result.ChangesApplied} change(s). Backup {result.Backup?.Id} saved — restore it from Backups to undo. Restart terminals to pick up the new values.";
                }
                else
                {
                    var msg = (result.RolledBack ? "Changes were rolled back. " : "Nothing was changed. ") + string.Join(" ", result.Errors.Take(3));
                    StatusMessage = msg;
                    System.Windows.MessageBox.Show(msg, "Repair did not complete", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Repair failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string DescribeChange(EnvChange c)
        {
            var label = $"{(c.Scope == EnvScope.Machine ? "System" : "User")} {c.Name}";
            if (c.IsDelete)
            {
                return $"• {label}: delete";
            }

            if (c.Name.Equals("Path", StringComparison.OrdinalIgnoreCase) || c.Name.Equals("PSModulePath", StringComparison.OrdinalIgnoreCase))
            {
                var before = (c.OldValue ?? string.Empty).Split(';').Where(e => e.Trim().Length > 0).ToList();
                var after = (c.NewValue ?? string.Empty).Split(';').Where(e => e.Trim().Length > 0).ToList();
                var removed = before.Except(after, StringComparer.OrdinalIgnoreCase).ToList();
                var added = after.Except(before, StringComparer.OrdinalIgnoreCase).ToList();
                var parts = new List<string>();
                if (removed.Count > 0)
                {
                    parts.Add($"remove {removed.Count} ({string.Join(", ", removed.Take(3))}{(removed.Count > 3 ? ", …" : string.Empty)})");
                }

                if (added.Count > 0)
                {
                    parts.Add($"add {string.Join(", ", added)}");
                }

                if (parts.Count == 0)
                {
                    parts.Add(c.OldKind != c.NewKind ? $"store as {c.NewKind}" : "reorder entries / remove duplicates");
                }

                return $"• {label}: {string.Join("; ", parts)}";
            }

            var oldShown = c.OldValue == null ? "(not set)" : EnvSecrets.Mask(c.Name, c.OldValue);
            var newShown = EnvSecrets.Mask(c.Name, c.NewValue!);
            return oldShown == newShown && c.OldKind != c.NewKind
                ? $"• {label}: store as {(c.NewKind == EnvValueKind.ExpandString ? "REG_EXPAND_SZ" : "REG_SZ")}"
                : $"• {label}: {oldShown} → {newShown}";
        }

        [RelayCommand]
        private async Task CreateBackupAsync()
        {
            if (_doctor == null)
            {
                return;
            }

            try
            {
                var b = await _doctor.CreateBackupAsync("Manual backup");
                LoadBackups();
                StatusMessage = $"Backup {b.Id} saved ({b.VariableCount} variables).";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Backup failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RestoreBackupAsync()
        {
            if (_doctor == null || SelectedBackup == null)
            {
                StatusMessage = "Select a backup to restore.";
                return;
            }

            var b = SelectedBackup;
            var answer = System.Windows.MessageBox.Show(
                $"Restore all User and System environment variables to the backup from {b.CreatedUtc.ToLocalTime():g}?\n\n\"{b.Reason}\"\n\nVariables added since then are removed. The current state is backed up first, so this can be undone too.",
                "Restore backup", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (answer != System.Windows.MessageBoxResult.Yes)
            {
                return;
            }

            IsBusy = true;
            try
            {
                var result = await _doctor.RestoreBackupAsync(b.Id);
                StatusMessage = result.Success
                    ? $"Restored backup {b.Id} ({result.ChangesApplied} change(s)). Restart terminals to pick up the values."
                    : (result.RolledBack ? "Restore rolled back: " : "Restore not applied: ") + string.Join(" ", result.Errors.Take(3));
                await ScanAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void OpenBackupFolder()
        {
            var dir = Backups.FirstOrDefault()?.JsonPath is { } p ? Path.GetDirectoryName(p) : null;
            if (dir != null && Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
            }
            else
            {
                StatusMessage = "No backups yet.";
            }
        }

        [RelayCommand]
        private void OpenWindowsEditor()
        {
            try
            {
                Process.Start(new ProcessStartInfo("rundll32.exe", "sysdm.cpl,EditEnvironmentVariables") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not open the Windows editor: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ExportReport()
        {
            if (_diagnosis == null)
            {
                StatusMessage = "Scan first.";
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"environment-report-{DateTime.Now:yyyyMMdd-HHmm}.txt",
                Filter = "Text report (*.txt)|*.txt",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Winhance-FS Environment Doctor report — {DateTime.Now:F}");
            sb.AppendLine($"{Summary}. Effective PATH length: {_diagnosis.EffectivePathLength:N0} characters. Secret-looking values are hidden.");
            sb.AppendLine();
            foreach (var i in Issues)
            {
                sb.AppendLine($"[{i.SeverityText}] {i.ScopeText} {i.Issue.Variable}: {i.Issue.Title}");
                sb.AppendLine($"    {i.Issue.Detail}");
                if (i.CanFix)
                {
                    sb.AppendLine($"    Fix: {i.FixText}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Command resolution (first match runs):");
            foreach (var c in Commands)
            {
                sb.AppendLine($"  {c.Command}: {string.Join(" | ", c.Matches.Select(m => m.Display))}");
            }

            sb.AppendLine();
            sb.AppendLine("PATH (System first, then User):");
            foreach (var e in PathEntries)
            {
                sb.AppendLine($"  {e.Scope,-7} {e.Index + 1,3}  {e.Status,-22} {e.Raw}");
            }

            File.WriteAllText(dialog.FileName, sb.ToString());
            StatusMessage = $"Report saved to {dialog.FileName}";
        }

        private void LoadBackups()
        {
            Backups.Clear();
            foreach (var b in _doctor?.ListBackups() ?? Array.Empty<EnvBackup>())
            {
                Backups.Add(b);
            }
        }
    }

    public partial class EnvIssueItem : ObservableObject
    {
        public EnvIssueItem(EnvIssue issue) => Issue = issue;

        public EnvIssue Issue { get; }

        [ObservableProperty]
        private bool _isSelected;

        public bool CanFix => Issue.Fix != null;

        public string SeverityText => Issue.Severity switch
        {
            EnvSeverity.Error => "Error",
            EnvSeverity.Warning => "Warning",
            _ => "Note",
        };

        public string SeverityColor => Issue.Severity switch
        {
            EnvSeverity.Error => "#FF6B6B",
            EnvSeverity.Warning => "#F5A623",
            _ => "#4ECDC4",
        };

        public string ScopeText => Issue.Scope switch
        {
            EnvScope.Machine => "System",
            EnvScope.User => "User",
            _ => "Both",
        };

        public string FixText => Issue.Fix?.Description is { Length: > 0 } d ? d : (CanFix ? "Fix" : "Manual — see details");
    }

    public sealed class EnvCommandItem
    {
        public EnvCommandItem(EnvCommandResolution r)
        {
            Command = r.Command;
            Winner = r.Winner ?? "(not found)";
            Matches = r.AllMatches.Select((m, i) => new EnvCommandMatch(r.Command, m, r.Versions.ElementAtOrDefault(i) ?? string.Empty, i == 0)).ToList();
        }

        public string Command { get; }

        public string Winner { get; }

        public IReadOnlyList<EnvCommandMatch> Matches { get; }

        public string CopiesText => Matches.Count == 1 ? "1 copy" : $"{Matches.Count} copies";

        public bool HasConflict => Matches.Count > 1;
    }

    public sealed record EnvCommandMatch(string Command, string Path, string Version, bool IsWinner)
    {
        public string Display => $"{(IsWinner ? "▶ " : string.Empty)}{Path}{(Version.Length > 0 ? $" ({Version})" : string.Empty)}";

        public string Role => IsWinner ? "Runs" : "Shadowed";
    }

    public sealed class EnvVariableItem
    {
        public EnvVariableItem(EnvVariable v)
        {
            Scope = v.Scope == EnvScope.Machine ? "System" : "User";
            Name = v.Name;
            Value = EnvSecrets.Mask(v.Name, v.RawValue);
            Kind = v.Kind == EnvValueKind.ExpandString ? "REG_EXPAND_SZ" : "REG_SZ";
        }

        public string Scope { get; }

        public string Name { get; }

        public string Value { get; }

        public string Kind { get; }
    }
}
