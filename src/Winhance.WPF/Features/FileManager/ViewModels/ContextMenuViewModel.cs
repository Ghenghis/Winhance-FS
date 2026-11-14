using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.WPF.Features.FileManager.ViewModels;
using CoreArchiveOptions = Winhance.Core.Features.FileManager.Models.ArchiveOptions;
using CoreExtractionOptions = Winhance.Core.Features.FileManager.Models.ExtractionOptions;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// ViewModel for dynamic context menus (WS-CTX features).
    /// File operations run directly against the Core services (IFileManagerService for delete/rename/create/
    /// properties, IClipboardService for copy/cut/paste, IArchiveService for compress/extract). Actions that
    /// need the hosting list (navigation, refresh, selection, pane comparison) are raised as events.
    /// </summary>
    public partial class ContextMenuViewModel : ObservableObject
    {
        private readonly IFileManagerService? _fileManagerService;
        private readonly IClipboardService? _clipboardService;
        private readonly IArchiveService? _archiveService;

        [ObservableProperty]
        private ObservableCollection<ContextMenuItemViewModel> _menuItems = new();

        [ObservableProperty]
        private ObservableCollection<FileItemViewModel> _selectedItems = new();

        [ObservableProperty]
        private FileItemViewModel? _rightClickedItem;

        [ObservableProperty]
        private bool _isRightPane;

        [ObservableProperty]
        private string _contextPath = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Raised when a folder is opened; the host should navigate to the given path.
        /// </summary>
        public event EventHandler<string>? NavigateRequested;

        /// <summary>
        /// Raised after the file system was changed; the host should reload the current folder.
        /// </summary>
        public event EventHandler? RefreshRequested;

        /// <summary>
        /// Raised for selection commands; the host sets <c>IsSelected</c> on each of its items to the predicate result.
        /// </summary>
        public event EventHandler<Func<FileItemViewModel, bool>>? SelectionRequested;

        /// <summary>
        /// Raised when the selection should be compared with the other pane.
        /// </summary>
        public event EventHandler? CompareWithOtherPanelRequested;

        public ContextMenuViewModel(IFileManagerService? fileManagerService,
                                   IClipboardService? clipboardService = null,
                                   IArchiveService? archiveService = null)
        {
            _fileManagerService = fileManagerService;
            _clipboardService = clipboardService;
            _archiveService = archiveService;
        }

        /// <summary>
        /// Update context menu based on selection (WS-CTX-001).
        /// </summary>
        public void UpdateContextMenu(ObservableCollection<FileItemViewModel> selectedItems,
                                    FileItemViewModel? rightClickedItem,
                                    bool isRightPane,
                                    string contextPath)
        {
            SelectedItems.Clear();
            foreach (var item in selectedItems)
            {
                SelectedItems.Add(item);
            }

            RightClickedItem = rightClickedItem;
            IsRightPane = isRightPane;
            ContextPath = contextPath;

            BuildMenuItems();
        }

        private void BuildMenuItems()
        {
            MenuItems.Clear();

            // Basic operations (always visible)
            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Open",
                Icon = "FolderOpen",
                Command = new RelayCommand(ExecuteOpen),
                InputGestureText = "Enter",
                IsEnabled = SelectedItems.Count <= 1 && (RightClickedItem?.IsDirectory == true || SelectedItems.Count == 1)
            });

            MenuItems.Add(new ContextMenuItemViewModel { IsSeparator = true });

            // Clipboard operations
            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Copy",
                Icon = "ContentCopy",
                Command = new RelayCommand(ExecuteCopy),
                InputGestureText = "Ctrl+C",
                IsEnabled = _clipboardService != null && (SelectedItems.Count > 0 || RightClickedItem != null)
            });

            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Cut",
                Icon = "ContentCut",
                Command = new RelayCommand(ExecuteCut),
                InputGestureText = "Ctrl+X",
                IsEnabled = _clipboardService != null && (SelectedItems.Count > 0 || RightClickedItem != null)
            });

            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Paste",
                Icon = "ContentPaste",
                Command = new AsyncRelayCommand(ExecutePasteAsync),
                InputGestureText = "Ctrl+V",
                IsEnabled = _clipboardService?.CanPaste == true
            });

            MenuItems.Add(new ContextMenuItemViewModel { IsSeparator = true });

            // File operations
            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Delete",
                Icon = "Delete",
                Command = new AsyncRelayCommand(ExecuteDeleteAsync),
                InputGestureText = "Del",
                IsEnabled = SelectedItems.Count > 0 || RightClickedItem != null
            });

            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Rename",
                Icon = "RenameBox",
                Command = new AsyncRelayCommand(ExecuteRenameAsync),
                InputGestureText = "F2",
                IsEnabled = (SelectedItems.Count == 1 && SelectedItems[0] != null) ||
                           (RightClickedItem != null && !RightClickedItem.IsParentDirectory)
            });

            MenuItems.Add(new ContextMenuItemViewModel { IsSeparator = true });

            // Create operations
            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "New Folder",
                Icon = "FolderPlus",
                Command = new AsyncRelayCommand(ExecuteNewFolderAsync),
                InputGestureText = "Ctrl+Shift+N",
                IsEnabled = true
            });

            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "New File",
                Icon = "FilePlus",
                Command = new RelayCommand(ExecuteNewFile),
                IsEnabled = true
            });

            // Dynamic submenu for "Create" (WS-CTX-005)
            var createSubmenu = new List<ContextMenuItemViewModel>
            {
                new() { Header = "Text Document", Command = new RelayCommand(() => CreateNewFile(".txt")) },
                new() { Header = "Word Document", Command = new RelayCommand(() => CreateNewFile(".docx")) },
                new() { Header = "Excel Spreadsheet", Command = new RelayCommand(() => CreateNewFile(".xlsx")) },
                new() { Header = "PowerPoint Presentation", Command = new RelayCommand(() => CreateNewFile(".pptx")) },
                new() { Header = "PDF Document", Command = new RelayCommand(() => CreateNewFile(".pdf")) },
                new() { Header = "Image File", Command = new RelayCommand(() => CreateNewFile(".png")) },
                new() { Header = "Zip Archive", Command = new RelayCommand(() => CreateNewFile(".zip")) }
            };

            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Create",
                Icon = "Plus",
                SubItems = createSubmenu
            });

            MenuItems.Add(new ContextMenuItemViewModel { IsSeparator = true });

            // Advanced operations (WS-CTX-010 to WS-CTX-015)
            if (SelectedItems.Count > 0)
            {
                MenuItems.Add(new ContextMenuItemViewModel
                {
                    Header = "Compress",
                    Icon = "ZipBox",
                    Command = new AsyncRelayCommand(ExecuteCompressAsync),
                    IsEnabled = _archiveService != null && SelectedItems.Count > 0
                });

                MenuItems.Add(new ContextMenuItemViewModel
                {
                    Header = "Extract",
                    Icon = "ZipBoxOutline",
                    Command = new AsyncRelayCommand(ExecuteExtractAsync),
                    IsEnabled = SelectedItems.Any(CanExtract)
                });

                MenuItems.Add(new ContextMenuItemViewModel
                {
                    Header = "Encrypt",
                    Icon = "Lock",
                    Command = new AsyncRelayCommand(ExecuteEncryptAsync),
                    IsEnabled = SelectedItems.Count > 0
                });

                MenuItems.Add(new ContextMenuItemViewModel { IsSeparator = true });

                // Selection operations submenu
                var selectionSubmenu = new List<ContextMenuItemViewModel>
                {
                    new() { Header = "Select All", Command = new RelayCommand(ExecuteSelectAll), InputGestureText = "Ctrl+A" },
                    new() { Header = "Invert Selection", Command = new RelayCommand(ExecuteInvertSelection), InputGestureText = "Ctrl+I" },
                    new() { Header = "Select Same Type", Command = new RelayCommand(ExecuteSelectSameType) },
                    new() { Header = "Select Same Size", Command = new RelayCommand(ExecuteSelectSameSize) },
                    new() { Header = "Select Same Date", Command = new RelayCommand(ExecuteSelectSameDate) }
                };

                MenuItems.Add(new ContextMenuItemViewModel
                {
                    Header = "Selection",
                    Icon = "CheckboxMultipleMarked",
                    SubItems = selectionSubmenu
                });
            }

            // Compare with other panel (WS-CTX-017)
            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Compare with Other Panel",
                Icon = "Compare",
                Command = new RelayCommand(ExecuteCompare),
                IsEnabled = SelectedItems.Count > 0
            });

            MenuItems.Add(new ContextMenuItemViewModel { IsSeparator = true });

            // System operations
            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Open in Terminal",
                Icon = "Console",
                Command = new RelayCommand(ExecuteOpenTerminal),
                IsEnabled = true
            });

            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Open in Explorer",
                Icon = "FolderOutline",
                Command = new RelayCommand(ExecuteOpenExplorer),
                IsEnabled = RightClickedItem != null || !string.IsNullOrEmpty(ContextPath)
            });

            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Copy Path",
                Icon = "ContentCopy",
                Command = new RelayCommand(ExecuteCopyPath),
                IsEnabled = RightClickedItem != null
            });

            MenuItems.Add(new ContextMenuItemViewModel { IsSeparator = true });

            // Properties
            MenuItems.Add(new ContextMenuItemViewModel
            {
                Header = "Properties",
                Icon = "InformationOutline",
                Command = new AsyncRelayCommand(ExecutePropertiesAsync),
                InputGestureText = "Alt+Enter",
                IsEnabled = RightClickedItem != null || SelectedItems.Count > 0
            });
        }

        // Command implementations
        private void ExecuteOpen()
        {
            var item = RightClickedItem ?? SelectedItems.FirstOrDefault();
            if (item == null) return;

            if (item.IsDirectory)
            {
                NavigateRequested?.Invoke(this, item.FullPath);
                return;
            }

            StartProcess(new ProcessStartInfo(item.FullPath) { UseShellExecute = true }, "open the file");
        }

        private void ExecuteCopy() => PutOnClipboard(cut: false);

        private void ExecuteCut() => PutOnClipboard(cut: true);

        private void PutOnClipboard(bool cut)
        {
            var paths = GetSelectedPaths();
            if (paths.Count == 0) return;

            if (_clipboardService == null)
            {
                ReportMissingService("clipboard");
                return;
            }

            if (cut)
            {
                _clipboardService.Cut(paths);
            }
            else
            {
                _clipboardService.Copy(paths);
            }

            StatusMessage = $"{(cut ? "Cut" : "Copied")} {paths.Count} item(s)";
        }

        private async Task ExecutePasteAsync()
        {
            var destination = GetTargetDirectory();
            if (destination == null) return;

            if (_clipboardService == null)
            {
                ReportMissingService("clipboard");
                return;
            }

            await RunFileOperationAsync("Paste", async () =>
            {
                var result = await _clipboardService.PasteAsync(destination);
                StatusMessage = result.Success
                    ? $"Pasted {result.ItemsPasted} item(s)"
                    : $"Pasted {result.ItemsPasted} item(s), {result.ItemsFailed} failed: {string.Join("; ", result.Errors.Select(e => e.Message))}";
            });
        }

        private async Task ExecuteDeleteAsync()
        {
            var paths = GetSelectedPaths();
            if (paths.Count == 0) return;

            if (_fileManagerService == null)
            {
                ReportMissingService("file manager");
                return;
            }

            var prompt = paths.Count == 1
                ? $"Move '{System.IO.Path.GetFileName(paths[0].TrimEnd('\\', '/'))}' to the Recycle Bin?"
                : $"Move {paths.Count} items to the Recycle Bin?";
            if (MessageBox.Show(prompt, "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            await RunFileOperationAsync("Delete", async () =>
            {
                var result = await _fileManagerService.RecycleAsync(paths);
                StatusMessage = result.Success
                    ? $"Deleted {result.ItemsProcessed} item(s)"
                    : $"Delete failed: {string.Join("; ", result.Errors)}";
            });
        }

        private async Task ExecuteRenameAsync()
        {
            var item = RightClickedItem ?? (SelectedItems.Count == 1 ? SelectedItems[0] : null);
            if (item == null || item.IsParentDirectory) return;

            if (_fileManagerService == null)
            {
                ReportMissingService("file manager");
                return;
            }

            var currentName = System.IO.Path.GetFileName(item.FullPath.TrimEnd('\\', '/'));
            var newName = Microsoft.VisualBasic.Interaction.InputBox("Enter new name:", "Rename", currentName)?.Trim();
            if (string.IsNullOrEmpty(newName) || newName == currentName) return;

            if (newName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            {
                ShowError("Rename", $"'{newName}' contains characters that are not allowed in file names.");
                return;
            }

            await RunFileOperationAsync("Rename", async () =>
            {
                var result = await _fileManagerService.RenameAsync(item.FullPath, newName);
                StatusMessage = result.Success
                    ? $"Renamed to {newName}"
                    : $"Rename failed: {(string.IsNullOrEmpty(result.Message) ? string.Join("; ", result.Errors) : result.Message)}";
            });
        }

        private async Task ExecuteNewFolderAsync()
        {
            var directory = GetTargetDirectory();
            if (directory == null) return;

            if (_fileManagerService == null)
            {
                ReportMissingService("file manager");
                return;
            }

            await RunFileOperationAsync("New Folder", async () =>
            {
                // The Core service picks a unique name when "New Folder" already exists.
                var result = await _fileManagerService.CreateDirectoryAsync(System.IO.Path.Combine(directory, "New Folder"));
                StatusMessage = result.Success ? result.Message : $"Create folder failed: {result.Message}";
            });
        }

        private void ExecuteNewFile() => CreateNewFile(".txt");

        private async Task ExecuteCompressAsync()
        {
            var paths = GetSelectedPaths();
            if (paths.Count == 0) return;

            if (_archiveService == null)
            {
                ReportMissingService("archive");
                return;
            }

            var directory = System.IO.Path.GetDirectoryName(paths[0].TrimEnd('\\', '/'));
            if (string.IsNullOrEmpty(directory))
            {
                directory = GetTargetDirectory();
            }

            if (string.IsNullOrEmpty(directory)) return;

            var baseName = paths.Count == 1
                ? System.IO.Path.GetFileName(paths[0].TrimEnd('\\', '/'))
                : "Archive";
            if (paths.Count == 1 && System.IO.File.Exists(paths[0]))
            {
                baseName = System.IO.Path.GetFileNameWithoutExtension(paths[0]);
            }

            var archivePath = GetUniquePath(System.IO.Path.Combine(directory, baseName + ".zip"));
            var archiveService = _archiveService;

            await RunFileOperationAsync("Compress", async () =>
            {
                var result = await Task.Run(() => archiveService.CreateArchiveAsync(
                    archivePath,
                    paths,
                    new CoreArchiveOptions { IncludeBaseFolder = true }));

                StatusMessage = result.Success
                    ? $"Created {System.IO.Path.GetFileName(archivePath)} ({result.FilesAdded} file(s))"
                    : $"Compress failed: {string.Join("; ", result.Errors)}";
            });
        }

        private async Task ExecuteExtractAsync()
        {
            if (_archiveService == null)
            {
                ReportMissingService("archive");
                return;
            }

            var archives = GetSelectedItems().Where(CanExtract).Select(i => i.FullPath).ToList();
            if (archives.Count == 0) return;

            var archiveService = _archiveService;
            await RunFileOperationAsync("Extract", async () =>
            {
                var extracted = 0;
                var errors = new List<string>();

                foreach (var archive in archives)
                {
                    var parent = System.IO.Path.GetDirectoryName(archive) ?? ContextPath;
                    var destination = GetUniquePath(System.IO.Path.Combine(parent, System.IO.Path.GetFileNameWithoutExtension(archive)));
                    var result = await Task.Run(() => archiveService.ExtractAllAsync(
                        archive,
                        destination,
                        new CoreExtractionOptions { OverwriteExisting = false, PreserveDirectoryStructure = true }));

                    extracted += result.EntriesExtracted;
                    errors.AddRange(result.Errors.Select(e => $"{System.IO.Path.GetFileName(archive)}: {e}"));
                }

                StatusMessage = errors.Count == 0
                    ? $"Extracted {extracted} file(s) from {archives.Count} archive(s)"
                    : $"Extracted {extracted} file(s); errors: {string.Join("; ", errors)}";
            });
        }

        private async Task ExecuteEncryptAsync()
        {
            var paths = GetSelectedPaths();
            if (paths.Count == 0) return;

            var confirm = MessageBox.Show(
                $"Encrypt {paths.Count} item(s) with Windows EFS?\n\nOnly your Windows account will be able to open the encrypted files.",
                "Encrypt",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            await RunFileOperationAsync("Encrypt", async () =>
            {
                var (encrypted, failures) = await Task.Run(() => EncryptWithEfs(paths));
                StatusMessage = failures.Count == 0
                    ? $"Encrypted {encrypted} file(s)"
                    : $"Encrypted {encrypted} file(s); failed: {string.Join("; ", failures)}";
            });
        }

        private void ExecuteSelectAll() => SelectionRequested?.Invoke(this, item => !item.IsParentDirectory);

        private void ExecuteInvertSelection() => SelectionRequested?.Invoke(this, item => !item.IsParentDirectory && !item.IsSelected);

        private void ExecuteSelectSameType() => SelectByType();

        private void ExecuteSelectSameSize() => SelectBySize();

        private void ExecuteSelectSameDate() => SelectByDate();

        private void ExecuteCompare() => CompareWithOtherPanelRequested?.Invoke(this, EventArgs.Empty);

        private void ExecuteOpenTerminal() => OpenTerminalAtPath();

        private void ExecuteOpenExplorer()
        {
            var item = RightClickedItem;
            if (item != null && !item.IsParentDirectory)
            {
                StartProcess(new ProcessStartInfo("explorer.exe", $"/select,\"{item.FullPath}\"") { UseShellExecute = true }, "open Explorer");
            }
            else if (System.IO.Directory.Exists(ContextPath))
            {
                StartProcess(new ProcessStartInfo("explorer.exe", $"\"{ContextPath}\"") { UseShellExecute = true }, "open Explorer");
            }
        }

        private void ExecuteCopyPath() => CopyPathToClipboard();

        private async Task ExecutePropertiesAsync()
        {
            var item = RightClickedItem ?? SelectedItems.FirstOrDefault();
            if (item == null || item.IsParentDirectory) return;

            if (_fileManagerService == null)
            {
                ReportMissingService("file manager");
                return;
            }

            await RunFileOperationAsync("Properties", async () =>
            {
                var properties = await _fileManagerService.GetPropertiesAsync(item.FullPath);
                var message =
                    $"Name: {properties.Name}\n" +
                    $"Location: {System.IO.Path.GetDirectoryName(properties.FullPath)}\n" +
                    $"Type: {properties.Type}\n" +
                    $"Size: {FormatSize(properties.Size)} ({properties.Size:N0} bytes)\n" +
                    $"Created: {properties.DateCreated:g}\n" +
                    $"Modified: {properties.DateModified:g}\n" +
                    $"Accessed: {properties.DateAccessed:g}\n" +
                    $"Attributes: {properties.Attributes}";
                if (!string.IsNullOrEmpty(properties.Hash))
                {
                    message += $"\nSHA-256: {properties.Hash}";
                }

                MessageBox.Show(message, $"{properties.Name} Properties", MessageBoxButton.OK, MessageBoxImage.Information);
            }, refresh: false);
        }

        private void CreateNewFile(string extension)
        {
            var directory = GetTargetDirectory();
            if (directory == null) return;

            try
            {
                var fullPath = GetUniquePath(System.IO.Path.Combine(directory, $"New File{extension}"));
                if (string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // An empty but valid ZIP archive.
                    using (System.IO.Compression.ZipFile.Open(fullPath, System.IO.Compression.ZipArchiveMode.Create))
                    {
                    }
                }
                else
                {
                    using (new System.IO.FileStream(fullPath, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write))
                    {
                    }
                }

                StatusMessage = $"Created {System.IO.Path.GetFileName(fullPath)}";
                RefreshRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ShowError("New File", ex.Message);
            }
        }

        private List<FileItemViewModel> GetSelectedItems()
        {
            var items = RightClickedItem != null && !SelectedItems.Contains(RightClickedItem)
                ? new List<FileItemViewModel> { RightClickedItem }
                : SelectedItems.ToList();
            return items.Where(i => i != null && !i.IsParentDirectory).ToList();
        }

        private List<string> GetSelectedPaths()
        {
            return GetSelectedItems().Select(i => i.FullPath).ToList();
        }

        /// <summary>
        /// The folder new items are created in / pasted into: the right-clicked folder, otherwise the current folder.
        /// </summary>
        private string? GetTargetDirectory()
        {
            if (RightClickedItem is { IsDirectory: true, IsParentDirectory: false } folder && System.IO.Directory.Exists(folder.FullPath))
            {
                return folder.FullPath;
            }

            return System.IO.Directory.Exists(ContextPath) ? ContextPath : null;
        }

        private bool CanExtract(FileItemViewModel item)
        {
            return !item.IsDirectory
                && !item.IsParentDirectory
                && _archiveService != null
                && _archiveService.CanExtractArchive(item.FullPath);
        }

        private FileItemViewModel? GetReferenceItem()
        {
            var item = RightClickedItem ?? SelectedItems.FirstOrDefault();
            return item == null || item.IsParentDirectory ? null : item;
        }

        private void SelectByType()
        {
            var reference = GetReferenceItem();
            if (reference == null) return;

            var isDirectory = reference.IsDirectory;
            var extension = reference.Extension ?? string.Empty;
            SelectionRequested?.Invoke(this, item =>
                !item.IsParentDirectory
                && item.IsDirectory == isDirectory
                && (isDirectory || string.Equals(item.Extension ?? string.Empty, extension, StringComparison.OrdinalIgnoreCase)));
        }

        private void SelectBySize()
        {
            var reference = GetReferenceItem();
            if (reference == null || reference.IsDirectory) return;

            var size = reference.Size;
            SelectionRequested?.Invoke(this, item => !item.IsParentDirectory && !item.IsDirectory && item.Size == size);
        }

        private void SelectByDate()
        {
            var reference = GetReferenceItem();
            if (reference == null) return;

            var date = reference.LastModified.Date;
            SelectionRequested?.Invoke(this, item => !item.IsParentDirectory && item.LastModified.Date == date);
        }

        private void OpenTerminalAtPath()
        {
            var directory = GetTargetDirectory();
            if (directory == null) return;

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k cd /d \"{directory}\"",
                WorkingDirectory = directory,
                UseShellExecute = true
            };
            StartProcess(startInfo, "open the terminal");
        }

        private void CopyPathToClipboard()
        {
            if (RightClickedItem != null)
            {
                Clipboard.SetText(RightClickedItem.FullPath);
                StatusMessage = "Path copied";
            }
        }

        /// <summary>
        /// Encrypts files with Windows EFS (the same mechanism as Explorer's "Encrypt contents to secure data").
        /// Folders are marked encrypted and every file below them is encrypted.
        /// </summary>
        private static (int Encrypted, List<string> Failures) EncryptWithEfs(IEnumerable<string> paths)
        {
            var encrypted = 0;
            var failures = new List<string>();

            void EncryptOne(string path)
            {
                try
                {
                    System.IO.File.Encrypt(path);
                    if (System.IO.File.Exists(path))
                    {
                        encrypted++;
                    }
                }
                catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or PlatformNotSupportedException or NotSupportedException)
                {
                    failures.Add($"{System.IO.Path.GetFileName(path)}: {ex.Message}");
                }
            }

            foreach (var path in paths)
            {
                if (System.IO.Directory.Exists(path))
                {
                    EncryptOne(path);
                    foreach (var file in System.IO.Directory.EnumerateFiles(path, "*", System.IO.SearchOption.AllDirectories))
                    {
                        EncryptOne(file);
                    }
                }
                else if (System.IO.File.Exists(path))
                {
                    EncryptOne(path);
                }
            }

            return (encrypted, failures);
        }

        private async Task RunFileOperationAsync(string operation, Func<Task> action, bool refresh = true)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                StatusMessage = $"{operation} failed: {ex.Message}";
                ShowError(operation, ex.Message);
            }
            finally
            {
                if (refresh)
                {
                    RefreshRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void StartProcess(ProcessStartInfo startInfo, string description)
        {
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ShowError("Context Menu", $"Unable to {description}: {ex.Message}");
            }
        }

        private void ReportMissingService(string serviceName)
        {
            StatusMessage = $"The {serviceName} service is not available.";
            ShowError("Context Menu", StatusMessage);
        }

        private static void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private static string GetUniquePath(string path)
        {
            if (!System.IO.File.Exists(path) && !System.IO.Directory.Exists(path))
            {
                return path;
            }

            var directory = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            var extension = System.IO.Path.GetExtension(path);
            var counter = 2;
            string candidate;
            do
            {
                candidate = System.IO.Path.Combine(directory, $"{name} ({counter}){extension}");
                counter++;
            }
            while (System.IO.File.Exists(candidate) || System.IO.Directory.Exists(candidate));

            return candidate;
        }

        private static string FormatSize(long bytes)
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
    }

    /// <summary>
    /// ViewModel for a context menu item.
    /// </summary>
    public partial class ContextMenuItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _header = string.Empty;

        [ObservableProperty]
        private string _icon = string.Empty;

        [ObservableProperty]
        private ICommand? _command;

        [ObservableProperty]
        private string _inputGestureText = string.Empty;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private bool _isSeparator;

        [ObservableProperty]
        private List<ContextMenuItemViewModel>? _subItems;
    }

    /// <summary>
    /// Extension methods for FileItemViewModel.
    /// </summary>
    public static class FileItemViewModelExtensions
    {
        public static bool IsArchive(this FileItemViewModel item)
        {
            var extension = item.Extension?.ToLowerInvariant();
            return extension == ".zip" || extension == ".rar" || extension == ".7z" ||
                   extension == ".tar" || extension == ".gz" || extension == ".bz2";
        }
    }
}
