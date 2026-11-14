using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CoreAddressBarService = Winhance.Core.Features.FileManager.Interfaces.IAddressBarService;
using CoreColumnDefinition = Winhance.Core.Features.FileManager.Interfaces.ColumnDefinition;
using CoreColumnService = Winhance.Core.Features.FileManager.Interfaces.IColumnService;
using CoreDuplicateFinderService = Winhance.Core.Features.FileManager.Interfaces.IDuplicateFinderService;
using CoreFavoritesService = Winhance.Core.Features.FileManager.Interfaces.IFavoritesService;
using CoreFileManagerService = Winhance.Core.Features.FileManager.Interfaces.IFileManagerService;
using CoreFileItem = Winhance.Core.Features.FileManager.Models.FileItem;
using CoreFileTab = Winhance.Core.Features.FileManager.Interfaces.FileTab;
using CoreOperationQueueService = Winhance.Core.Features.FileManager.Interfaces.IOperationQueueService;
using CorePreviewService = Winhance.Core.Features.FileManager.Interfaces.IPreviewService;
using CoreSearchService = Winhance.Core.Features.FileManager.Interfaces.ISearchService;
using CoreSelectionService = Winhance.Core.Features.FileManager.Interfaces.ISelectionService;
using CoreTabService = Winhance.Core.Features.FileManager.Interfaces.ITabService;
using CoreViewMode = Winhance.Core.Features.FileManager.Interfaces.ViewMode;
using CoreViewModeService = Winhance.Core.Features.FileManager.Interfaces.IViewModeService;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    public interface IOperationQueueService
    {
        ObservableCollection<Winhance.Core.Features.FileManager.Interfaces.FileOperation> Operations { get; }
        Winhance.Core.Features.FileManager.Interfaces.FileOperation? CurrentOperation { get; }
        event EventHandler<OperationQueuedEventArgs>? OperationQueued;
        event EventHandler<OperationStartedEventArgs>? OperationStarted;
        event EventHandler<OperationCompletedEventArgs>? OperationCompleted;
        event EventHandler<OperationProgressEventArgs>? OperationProgress;
        event EventHandler<OperationFailedEventArgs>? OperationFailed;
        event EventHandler? QueuePaused;
        event EventHandler? QueueResumed;
        Winhance.Core.Features.FileManager.Interfaces.FileOperation QueueCopy(IEnumerable<string> sources, string destination);
        Winhance.Core.Features.FileManager.Interfaces.FileOperation QueueMove(IEnumerable<string> sources, string destination);
        Winhance.Core.Features.FileManager.Interfaces.FileOperation QueueDelete(IEnumerable<string> paths, bool permanent = false);
        void Pause(Winhance.Core.Features.FileManager.Interfaces.FileOperation operation);
        void Resume(Winhance.Core.Features.FileManager.Interfaces.FileOperation operation);
        void Cancel(Winhance.Core.Features.FileManager.Interfaces.FileOperation operation);
        Task<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>> GetQueuedOperationsAsync();
        Task<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>> GetActiveOperationsAsync();
        Task<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>> GetCompletedOperationsAsync(int count);
        Task PauseQueueAsync();
        Task ResumeQueueAsync();
        Task CancelOperationAsync(string operationId);
        Task RetryOperationAsync(string operationId);
        Task ClearCompletedOperationsAsync();
        Task ClearQueueAsync();
        Task QueueCompressOperationAsync(IEnumerable<string> paths, string? destinationArchive = null);
        Task QueueExtractOperationAsync(IEnumerable<string> archivePaths, string? destinationFolder = null);
        Task QueueEncryptOperationAsync(IEnumerable<string> paths, string? password = null);
    }

    public interface ISpaceAnalyzerService
    {
        Task<IReadOnlyList<DiskDriveInfo>> GetDrivesAsync();
        Task<IReadOnlyList<SpaceUsageItem>> AnalyzeDriveAsync(string rootPath);
        Task<FolderAnalysisResult> AnalyzeFolderAsync(string folderPath);
        Task<IReadOnlyList<LargeFileItem>> FindLargestFilesAsync(FileSearchOptions options);
        Task<IReadOnlyList<DuplicateSpaceGroup>> FindDuplicateSpaceAsync(string path);
        Task<ScanResult> ScanAsync(string path, ScanOptions options, IProgress<ScanProgress>? progress = null);
        Task<IReadOnlyList<DiskUsageChartItem>> GetDiskUsageChartAsync(string rootPath);
        Task<IReadOnlyList<FileTypeItem>> GetFileTypeDistributionAsync(string path);
        Task<IReadOnlyList<TimelineItem>> GetFileTimelineAsync(string path, DateTime startDate, DateTime endDate, TimelineGranularity granularity);
        Task<IReadOnlyList<NetworkDrive>> ScanNetworkDrivesAsync();
        Task<IReadOnlyList<NetworkConnection>> GetNetworkConnectionsAsync(string drivePath);
    }

    public static class FileManagerViewModelServiceExtensions
    {
        private static readonly ConditionalWeakTable<CoreViewModeService, ViewModeSettings> ViewModeSettingsByService = new();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        public static Task<CoreFileTab> CreateTabAsync(this CoreTabService tabService, string path)
        {
            return Task.FromResult(tabService.CreateTab(path));
        }

        public static void ReorderTab(this CoreTabService tabService, int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= tabService.Tabs.Count || toIndex < 0 || toIndex >= tabService.Tabs.Count)
            {
                return;
            }

            if (fromIndex == toIndex)
            {
                return;
            }

            tabService.Tabs.Move(fromIndex, toIndex);
        }

        public static Task MoveTabBetweenPanelsAsync(this CoreTabService tabService, string tabId, string sourcePanel, string targetPanel)
        {
            var tab = tabService.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null)
            {
                tab.TagData["SourcePanel"] = sourcePanel;
                tab.TagData["Panel"] = targetPanel;
            }

            return Task.CompletedTask;
        }

        public static Task<CoreFileTab?> DuplicateTabAsync(this CoreTabService tabService, string tabId)
        {
            var tab = tabService.Tabs.FirstOrDefault(t => t.Id == tabId);
            return Task.FromResult(tab == null ? null : tabService.DuplicateTab(tab));
        }

        public static Task<IReadOnlyList<CoreFileTab>> GetAllTabsAsync(this CoreTabService tabService)
        {
            return Task.FromResult<IReadOnlyList<CoreFileTab>>(tabService.Tabs.ToList());
        }

        public static Task CloseTabAsync(this CoreTabService tabService, string tabId)
        {
            var tab = tabService.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null)
            {
                tabService.CloseTab(tab);
            }

            return Task.CompletedTask;
        }

        public static Task SetActiveTabAsync(this CoreTabService tabService, string tabId)
        {
            var tab = tabService.Tabs.FirstOrDefault(t => t.Id == tabId);
            if (tab != null)
            {
                tabService.ActiveTab = tab;
            }

            return Task.CompletedTask;
        }

        public static Task NextTabAsync(this CoreTabService tabService)
        {
            var next = tabService.GetNextTab(tabService.ActiveTab);
            if (next != null)
            {
                tabService.ActiveTab = next;
            }

            return Task.CompletedTask;
        }

        public static Task PreviousTabAsync(this CoreTabService tabService)
        {
            var previous = tabService.GetPreviousTab(tabService.ActiveTab);
            if (previous != null)
            {
                tabService.ActiveTab = previous;
            }

            return Task.CompletedTask;
        }

        public static Task SaveSessionAsync(this CoreTabService tabService)
        {
            return tabService.SaveSessionAsync("Default");
        }

        public static Task<bool> RestoreSessionAsync(this CoreTabService tabService)
        {
            var sessionName = tabService.GetSavedSessions().FirstOrDefault() ?? "Default";
            return tabService.LoadSessionAsync(sessionName);
        }

        public static Task ClearSessionAsync(this CoreTabService tabService)
        {
            foreach (var session in tabService.GetSavedSessions().ToList())
            {
                tabService.DeleteSession(session);
            }

            return Task.CompletedTask;
        }

        public static void SetSelection(this CoreSelectionService selectionService, IEnumerable<string> paths)
        {
            selectionService.SelectMany(paths.Select(CreateFileItemFromPath), clearExisting: true);
        }

        public static IEnumerable<string> GetSelectedPaths(this CoreSelectionService selectionService)
        {
            return selectionService.SelectedItems.Select(i => i.FullPath).ToArray();
        }

        public static void AddToSelection(this CoreSelectionService selectionService, string path)
        {
            selectionService.Select(CreateFileItemFromPath(path), clearExisting: false);
        }

        public static void AddToSelection(this CoreSelectionService selectionService, IEnumerable<string> paths)
        {
            selectionService.SelectMany(paths.Select(CreateFileItemFromPath), clearExisting: false);
        }

        public static void RemoveFromSelection(this CoreSelectionService selectionService, string path)
        {
            var item = selectionService.SelectedItems.FirstOrDefault(i => i.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                selectionService.Deselect(item);
            }
        }

        public static bool IsSelected(this CoreSelectionService selectionService, string path)
        {
            return selectionService.SelectedItems.Any(i => i.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        public static IReadOnlyList<string> SelectByPattern(
            this CoreSelectionService selectionService,
            IEnumerable<string> paths,
            PatternOptions options)
        {
            var pattern = options.UseRegex && options.Pattern.StartsWith("regex:", StringComparison.OrdinalIgnoreCase)
                ? options.Pattern["regex:".Length..]
                : "^" + Regex.Escape(options.Pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";

            var regexOptions = options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var regex = new Regex(pattern, regexOptions);

            return paths
                .Where(path => regex.IsMatch(Path.GetFileName(path)))
                .ToArray();
        }

        public static async Task SaveSelectionPresetAsync(this CoreSelectionService selectionService, SelectionPreset preset)
        {
            selectionService.SetSelection(preset.Paths);
            await selectionService.SaveSelectionSetAsync(preset.Name);
        }

        public static Task<IReadOnlyList<SelectionPreset>> GetSelectionPresetsAsync(this CoreSelectionService selectionService)
        {
            var presets = selectionService.GetSelectionSets()
                .Select(s => new SelectionPreset
                {
                    Id = s.Name,
                    Name = s.Name,
                    Paths = s.Paths.ToArray(),
                    CreatedAt = s.CreatedAt
                })
                .ToArray();

            return Task.FromResult<IReadOnlyList<SelectionPreset>>(presets);
        }

        public static Task DeleteSelectionPresetAsync(this CoreSelectionService selectionService, string presetId)
        {
            selectionService.DeleteSelectionSet(presetId);
            return Task.CompletedTask;
        }

        public static IReadOnlyList<CoreFileItem> GetSelectedItems(this CoreSelectionService selectionService)
        {
            return selectionService.SelectedItems.ToList();
        }

        public static Task<ViewModeSettings> GetCurrentSettingsAsync(this CoreViewModeService viewModeService)
        {
            var settings = ViewModeSettingsByService.GetValue(viewModeService, service => new ViewModeSettings
            {
                ViewMode = ToUiViewMode(service.CurrentViewMode),
                IconSize = service.GetIconSize(),
                ThumbnailSize = service.GetThumbnailSize()
            });

            settings.ViewMode = ToUiViewMode(viewModeService.CurrentViewMode);
            settings.IconSize = viewModeService.GetIconSize();
            settings.ThumbnailSize = viewModeService.GetThumbnailSize();

            return Task.FromResult(settings);
        }

        public static async Task SetViewModeAsync(this CoreViewModeService viewModeService, string mode)
        {
            var settings = ViewModeSettingsByService.GetOrCreateValue(viewModeService);
            settings.ViewMode = mode;
            viewModeService.CurrentViewMode = ToCoreViewMode(mode);
            await viewModeService.SavePreferencesAsync();
        }

        public static async Task SetShowFileExtensionsAsync(this CoreViewModeService viewModeService, bool value)
        {
            ViewModeSettingsByService.GetOrCreateValue(viewModeService).ShowFileExtensions = value;
            await viewModeService.SavePreferencesAsync();
        }

        public static async Task SetShowHiddenFilesAsync(this CoreViewModeService viewModeService, bool value)
        {
            ViewModeSettingsByService.GetOrCreateValue(viewModeService).ShowHiddenFiles = value;
            await viewModeService.SavePreferencesAsync();
        }

        public static async Task SetShowSystemFilesAsync(this CoreViewModeService viewModeService, bool value)
        {
            ViewModeSettingsByService.GetOrCreateValue(viewModeService).ShowSystemFiles = value;
            await viewModeService.SavePreferencesAsync();
        }

        public static async Task SetShowThumbnailsAsync(this CoreViewModeService viewModeService, bool value)
        {
            ViewModeSettingsByService.GetOrCreateValue(viewModeService).ShowThumbnails = value;
            await viewModeService.SavePreferencesAsync();
        }

        public static async Task SetIconSizeAsync(this CoreViewModeService viewModeService, int size)
        {
            ViewModeSettingsByService.GetOrCreateValue(viewModeService).IconSize = size;
            viewModeService.SetIconSize(size);
            await viewModeService.SavePreferencesAsync();
        }

        public static async Task SetThumbnailSizeAsync(this CoreViewModeService viewModeService, int size)
        {
            ViewModeSettingsByService.GetOrCreateValue(viewModeService).ThumbnailSize = size;
            viewModeService.SetThumbnailSize(size);
            await viewModeService.SavePreferencesAsync();
        }

        public static async Task SetGroupByAsync(this CoreViewModeService viewModeService, string groupBy)
        {
            var settings = ViewModeSettingsByService.GetOrCreateValue(viewModeService);
            settings.GroupBy = groupBy;
            settings.GroupItems = !string.Equals(groupBy, "None", StringComparison.OrdinalIgnoreCase);
            await viewModeService.SavePreferencesAsync();
        }

        public static Task SaveAsDefaultAsync(this CoreViewModeService viewModeService)
        {
            viewModeService.DefaultViewMode = viewModeService.CurrentViewMode;
            return viewModeService.SavePreferencesAsync();
        }

        public static async Task ResetToDefaultAsync(this CoreViewModeService viewModeService)
        {
            viewModeService.CurrentViewMode = viewModeService.DefaultViewMode;
            viewModeService.SetIconSize(48);
            viewModeService.SetThumbnailSize(96);

            var settings = ViewModeSettingsByService.GetOrCreateValue(viewModeService);
            settings.ViewMode = ToUiViewMode(viewModeService.CurrentViewMode);
            settings.IconSize = viewModeService.GetIconSize();
            settings.ThumbnailSize = viewModeService.GetThumbnailSize();
            settings.ShowFileExtensions = true;
            settings.ShowHiddenFiles = false;
            settings.ShowSystemFiles = false;
            settings.ShowThumbnails = true;
            settings.GroupItems = false;
            settings.GroupBy = "None";

            await viewModeService.SavePreferencesAsync();
        }

        public static Task<IReadOnlyList<ColumnConfigurationItem>> GetAvailableColumnsAsync(this CoreColumnService columnService)
        {
            var columns = columnService.AvailableColumns
                .OrderBy(c => c.DisplayIndex)
                .Select(ToColumnConfigurationItem)
                .ToArray();

            return Task.FromResult<IReadOnlyList<ColumnConfigurationItem>>(columns);
        }

        public static async Task SaveConfigurationAsync(this CoreColumnService columnService, IReadOnlyList<object> configuration)
        {
            foreach (var item in configuration)
            {
                var name = GetObjectProperty<string>(item, "Name") ?? GetObjectProperty<string>(item, "Id");
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var isVisible = GetObjectProperty<bool?>(item, "IsVisible") ?? true;
                var width = GetObjectProperty<double?>(item, "Width");
                var order = GetObjectProperty<int?>(item, "Order");

                if (isVisible)
                {
                    columnService.ShowColumn(name);
                }
                else
                {
                    columnService.HideColumn(name);
                }

                if (width.HasValue && width.Value > 0)
                {
                    columnService.SetColumnWidth(name, width.Value);
                }

                if (order.HasValue)
                {
                    columnService.MoveColumn(name, order.Value);
                }
            }

            await columnService.SavePreferencesAsync();
        }

        public static async Task ResetToDefaultAsync(this CoreColumnService columnService)
        {
            columnService.ResetToDefaults();
            await columnService.SavePreferencesAsync();
        }

        public static Task ShowMessageAsync(this CoreAddressBarService addressBarService, string message)
        {
            System.Windows.MessageBox.Show(
                message,
                "Winhance File Manager",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        public static Task ShowErrorAsync(this CoreAddressBarService addressBarService, string message)
        {
            System.Windows.MessageBox.Show(
                message,
                "Winhance File Manager",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return Task.CompletedTask;
        }

        public static Task<IReadOnlyList<string>> GetSubFoldersAsync(this CoreAddressBarService addressBarService, string path)
        {
            if (!Directory.Exists(path))
            {
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }

            var folders = Directory.EnumerateDirectories(path)
                .OrderBy(folder => Path.GetFileName(folder), StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return Task.FromResult<IReadOnlyList<string>>(folders);
        }

        public static async Task AddFavoriteAsync(this CoreFavoritesService favoritesService, string path)
        {
            favoritesService.AddFavorite(path);
            await favoritesService.SaveAsync();
        }

        public static async Task AddRecentLocationAsync(this CoreFavoritesService favoritesService, string path)
        {
            favoritesService.RecordVisit(path);
            await favoritesService.SaveAsync();
        }

        public static Task RenameDirectoryAsync(this CoreFileManagerService fileManagerService, string path, string newPath)
        {
            return fileManagerService.RenameAsync(path, Path.GetFileName(newPath));
        }

        public static Task RenameFileAsync(this CoreFileManagerService fileManagerService, string path, string newPath)
        {
            return fileManagerService.RenameAsync(path, Path.GetFileName(newPath));
        }

        public static Task DeleteDirectoryAsync(this CoreFileManagerService fileManagerService, string path, bool permanent = false)
        {
            return fileManagerService.DeleteFilesAsync(new[] { path }, permanent);
        }

        public static Task DeleteFileAsync(this CoreFileManagerService fileManagerService, string path, bool permanent = false)
        {
            return fileManagerService.DeleteFilesAsync(new[] { path }, permanent);
        }

        public static async Task NavigateToDirectoryAsync(this CoreFileManagerService fileManagerService, string path)
        {
            await fileManagerService.GetDirectoryContentsAsync(path);
        }

        public static async Task CreateShortcutAsync(this CoreFileManagerService fileManagerService, string targetPath, string shortcutPath)
        {
            await Task.Run(() =>
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell")
                    ?? throw new InvalidOperationException("Windows Script Host is not available.");
                dynamic shell = Activator.CreateInstance(shellType)
                    ?? throw new InvalidOperationException("Unable to create Windows Script Host shell.");
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Directory.Exists(targetPath)
                    ? targetPath
                    : Path.GetDirectoryName(targetPath) ?? string.Empty;
                shortcut.Save();
            });
        }

        public static Task CreateHardLinkAsync(this CoreFileManagerService fileManagerService, string existingFilePath, string linkPath)
        {
            if (!File.Exists(existingFilePath))
            {
                throw new FileNotFoundException("Hard link target file does not exist.", existingFilePath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(linkPath) ?? Directory.GetCurrentDirectory());

            if (!CreateHardLink(linkPath, existingFilePath, IntPtr.Zero))
            {
                throw new IOException($"Unable to create hard link. Win32 error: {Marshal.GetLastWin32Error()}");
            }

            return Task.CompletedTask;
        }

        public static Task CreateSymbolicLinkAsync(this CoreFileManagerService fileManagerService, string targetPath, string linkPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(linkPath) ?? Directory.GetCurrentDirectory());

            if (Directory.Exists(targetPath))
            {
                Directory.CreateSymbolicLink(linkPath, targetPath);
            }
            else
            {
                File.CreateSymbolicLink(linkPath, targetPath);
            }

            return Task.CompletedTask;
        }

        public static async Task CreateJunctionAsync(this CoreFileManagerService fileManagerService, string targetPath, string junctionPath)
        {
            if (!Directory.Exists(targetPath))
            {
                throw new DirectoryNotFoundException($"Junction target directory does not exist: {targetPath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(junctionPath) ?? Directory.GetCurrentDirectory());
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c mklink /J \"{junctionPath}\" \"{targetPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });

            if (process == null)
            {
                throw new InvalidOperationException("Unable to start mklink process.");
            }

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new IOException(string.IsNullOrWhiteSpace(error) ? "Junction creation failed." : error.Trim());
            }
        }

        public static Task<FileAttributeInfo> GetFileAttributesAsync(this CoreFileManagerService fileManagerService, string path)
        {
            var attributes = File.GetAttributes(path);
            return Task.FromResult(new FileAttributeInfo
            {
                IsArchive = attributes.HasFlag(FileAttributes.Archive),
                IsHidden = attributes.HasFlag(FileAttributes.Hidden),
                IsReadOnly = attributes.HasFlag(FileAttributes.ReadOnly),
                IsSystem = attributes.HasFlag(FileAttributes.System),
                IsCompressed = attributes.HasFlag(FileAttributes.Compressed),
                IsEncrypted = attributes.HasFlag(FileAttributes.Encrypted)
            });
        }

        public static Task<FileTimestampInfo> GetFileTimestampsAsync(this CoreFileManagerService fileManagerService, string path)
        {
            return Task.FromResult(new FileTimestampInfo
            {
                Created = File.GetCreationTime(path),
                Modified = File.GetLastWriteTime(path),
                Accessed = File.GetLastAccessTime(path)
            });
        }

        public static async Task SplitFileAsync(this CoreFileManagerService fileManagerService, string path, long chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");
            }

            await using var input = File.OpenRead(path);
            var buffer = new byte[Math.Min(chunkSize, 1024 * 1024)];
            var part = 1;

            while (input.Position < input.Length)
            {
                var partPath = $"{path}.part{part:000}";
                await using var output = File.Create(partPath);
                long written = 0;

                while (written < chunkSize && input.Position < input.Length)
                {
                    var readSize = (int)Math.Min(buffer.Length, chunkSize - written);
                    var bytesRead = await input.ReadAsync(buffer.AsMemory(0, readSize));
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await output.WriteAsync(buffer.AsMemory(0, bytesRead));
                    written += bytesRead;
                }

                part++;
            }
        }

        public static async Task JoinFilesAsync(this CoreFileManagerService fileManagerService, IEnumerable<string> partPaths, string outputPath)
        {
            var orderedParts = partPaths
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (orderedParts.Length == 0)
            {
                throw new ArgumentException("At least one part file is required.", nameof(partPaths));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory());
            await using var output = File.Create(outputPath);

            foreach (var partPath in orderedParts)
            {
                await using var input = File.OpenRead(partPath);
                await input.CopyToAsync(output);
            }
        }

        public static async Task<string> CalculateChecksumAsync(this CoreFileManagerService fileManagerService, string path, string algorithm)
        {
            using var hashAlgorithm = algorithm.ToUpperInvariant() switch
            {
                "MD5" => (HashAlgorithm)MD5.Create(),
                "SHA1" => SHA1.Create(),
                "SHA256" => SHA256.Create(),
                "SHA512" => SHA512.Create(),
                _ => throw new NotSupportedException($"Checksum algorithm is not supported: {algorithm}")
            };

            await using var stream = File.OpenRead(path);
            var hash = await hashAlgorithm.ComputeHashAsync(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static async Task SecureDeleteAsync(this CoreFileManagerService fileManagerService, string path, int passes)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    await fileManagerService.SecureDeleteAsync(file, passes);
                }

                Directory.Delete(path, recursive: true);
                return;
            }

            if (!File.Exists(path))
            {
                return;
            }

            var fileInfo = new FileInfo(path);
            var length = fileInfo.Length;
            var buffer = new byte[1024 * 1024];

            for (var pass = 0; pass < Math.Max(1, passes); pass++)
            {
                await using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
                long written = 0;
                while (written < length)
                {
                    RandomNumberGenerator.Fill(buffer);
                    var toWrite = (int)Math.Min(buffer.Length, length - written);
                    await stream.WriteAsync(buffer.AsMemory(0, toWrite));
                    written += toWrite;
                }

                await stream.FlushAsync();
            }

            File.Delete(path);
        }

        public static async Task WipeFreeSpaceAsync(this CoreFileManagerService fileManagerService, string path)
        {
            var root = Directory.Exists(path) ? Path.GetPathRoot(path) : Path.GetPathRoot(Path.GetFullPath(path));
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new InvalidOperationException("Unable to resolve drive root for free-space wipe.");
            }

            var wipeDirectory = Directory.Exists(path) ? path : root;
            var tempPath = Path.Combine(wipeDirectory, $".winhance-free-space-wipe-{Guid.NewGuid():N}.tmp");
            var buffer = new byte[1024 * 1024];

            try
            {
                RandomNumberGenerator.Fill(buffer);
                await using var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

                while (true)
                {
                    try
                    {
                        await stream.WriteAsync(buffer);
                    }
                    catch (IOException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        public static async Task<ComparisonResult> CompareFilesAsync(
            this CoreFileManagerService fileManagerService,
            string leftFilePath,
            string rightFilePath,
            ComparisonMode mode)
        {
            var result = new ComparisonResult
            {
                LeftFile = leftFilePath,
                RightFile = rightFilePath,
                ComparisonTime = DateTime.Now
            };

            switch (mode)
            {
                case ComparisonMode.Size:
                    result.AreEqual = new FileInfo(leftFilePath).Length == new FileInfo(rightFilePath).Length;
                    break;
                case ComparisonMode.Checksum:
                    result.AreEqual = string.Equals(
                        await fileManagerService.CalculateChecksumAsync(leftFilePath, "SHA256"),
                        await fileManagerService.CalculateChecksumAsync(rightFilePath, "SHA256"),
                        StringComparison.OrdinalIgnoreCase);
                    break;
                case ComparisonMode.Attributes:
                    result.AreEqual = File.GetAttributes(leftFilePath) == File.GetAttributes(rightFilePath);
                    break;
                case ComparisonMode.Timestamps:
                    result.AreEqual = File.GetLastWriteTimeUtc(leftFilePath) == File.GetLastWriteTimeUtc(rightFilePath);
                    break;
                default:
                    result.AreEqual = await FilesHaveSameContent(leftFilePath, rightFilePath);
                    break;
            }

            if (!result.AreEqual)
            {
                result.Differences.Add($"{mode} differs.");
            }

            return result;
        }

        public static async Task<FileProperties> GetFilePropertiesAsync(this CoreFileManagerService fileManagerService, string path)
        {
            var coreProperties = await fileManagerService.GetPropertiesAsync(path);
            var versionInfo = File.Exists(path) ? FileVersionInfo.GetVersionInfo(path) : null;

            return new FileProperties
            {
                Name = coreProperties.Name,
                FullPath = coreProperties.FullPath,
                Size = coreProperties.Size,
                Created = coreProperties.DateCreated,
                Modified = coreProperties.DateModified,
                Accessed = coreProperties.DateAccessed,
                Attributes = coreProperties.Attributes,
                SHA256 = string.IsNullOrWhiteSpace(coreProperties.Hash) ? null : coreProperties.Hash,
                Version = versionInfo?.FileVersion,
                Company = versionInfo?.CompanyName,
                Description = versionInfo?.FileDescription
            };
        }

        public static Task UpdateFilePropertiesAsync(this CoreFileManagerService fileManagerService, string path, FileProperties properties)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                throw new FileNotFoundException("Path does not exist.", path);
            }

            File.SetAttributes(path, properties.Attributes);
            File.SetCreationTime(path, properties.Created);
            File.SetLastWriteTime(path, properties.Modified);
            File.SetLastAccessTime(path, properties.Accessed);
            return Task.CompletedTask;
        }

        public static bool HasPreview(this CorePreviewService previewService, CoreFileItem fileItem)
        {
            return !fileItem.IsDirectory && previewService.CanPreview(fileItem.FullPath);
        }

        public static ImageSource? GetPreviewImage(this CorePreviewService previewService, CoreFileItem fileItem)
        {
            if (!previewService.HasPreview(fileItem))
            {
                return null;
            }

            if (previewService.GetPreviewType(fileItem.FullPath) != Winhance.Core.Features.FileManager.Interfaces.PreviewType.Image)
            {
                return null;
            }

            try
            {
                return LoadBitmapFromFile(fileItem.FullPath);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<ImageSource?> GeneratePreviewAsync(this CorePreviewService previewService, CoreFileItem fileItem)
        {
            if (!previewService.HasPreview(fileItem))
            {
                return null;
            }

            if (previewService.GetPreviewType(fileItem.FullPath) != Winhance.Core.Features.FileManager.Interfaces.PreviewType.Image)
            {
                return null;
            }

            await using var stream = await previewService.GetImagePreviewAsync(fileItem.FullPath);
            return stream == null ? null : LoadBitmapFromStream(stream);
        }

        public static Task<IReadOnlyList<SearchTemplate>> GetSearchTemplatesAsync(this CoreSearchService searchService)
        {
            return Task.FromResult<IReadOnlyList<SearchTemplate>>(Array.Empty<SearchTemplate>());
        }

        public static Task<IReadOnlyList<object>> AdvancedSearchAsync(this CoreSearchService searchService, AdvancedSearchQuery query)
        {
            return Task.FromResult<IReadOnlyList<object>>(Array.Empty<object>());
        }

        public static Task DeleteSearchTemplateAsync(this CoreSearchService searchService, string templateId)
        {
            return Task.CompletedTask;
        }

        public static Task<IReadOnlyList<DuplicateGroup>> FindDuplicatesAsync(
            this CoreDuplicateFinderService duplicateFinderService,
            DuplicateSearchOptions options,
            IProgress<DuplicateScanProgress>? progress = null)
        {
            return Task.FromResult<IReadOnlyList<DuplicateGroup>>(Array.Empty<DuplicateGroup>());
        }

        public static void CancelScan(this CoreDuplicateFinderService duplicateFinderService)
        {
        }

        public static Task DeleteFilesAsync(this CoreDuplicateFinderService duplicateFinderService, IReadOnlyList<DuplicateFile> files)
        {
            return Task.CompletedTask;
        }

        public static Task MoveFilesAsync(this CoreDuplicateFinderService duplicateFinderService, IReadOnlyList<DuplicateFile> files, string destinationFolder)
        {
            return Task.CompletedTask;
        }

        public static Task<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>> GetQueuedOperationsAsync(this CoreOperationQueueService operationQueueService)
        {
            return Task.FromResult<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>>(
                operationQueueService.GetOperationsByStatus(Winhance.Core.Features.FileManager.Interfaces.OperationStatus.Queued).ToList());
        }

        public static Task<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>> GetActiveOperationsAsync(this CoreOperationQueueService operationQueueService)
        {
            return Task.FromResult<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>>(
                operationQueueService.GetOperationsByStatus(Winhance.Core.Features.FileManager.Interfaces.OperationStatus.Running).ToList());
        }

        public static Task<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>> GetCompletedOperationsAsync(this CoreOperationQueueService operationQueueService, int count)
        {
            var completed = operationQueueService.GetHistory(count)
                .Where(o => o.Status == Winhance.Core.Features.FileManager.Interfaces.OperationStatus.Completed
                    || o.Status == Winhance.Core.Features.FileManager.Interfaces.OperationStatus.Failed
                    || o.Status == Winhance.Core.Features.FileManager.Interfaces.OperationStatus.Cancelled)
                .ToList();

            return Task.FromResult<IReadOnlyList<Winhance.Core.Features.FileManager.Interfaces.FileOperation>>(completed);
        }

        public static Task PauseQueueAsync(this CoreOperationQueueService operationQueueService)
        {
            operationQueueService.PauseAll();
            return Task.CompletedTask;
        }

        public static Task ResumeQueueAsync(this CoreOperationQueueService operationQueueService)
        {
            operationQueueService.ResumeAll();
            return Task.CompletedTask;
        }

        public static Task CancelOperationAsync(this CoreOperationQueueService operationQueueService, string operationId)
        {
            var operation = operationQueueService.Operations.FirstOrDefault(o => o.Id == operationId);
            if (operation != null)
            {
                operationQueueService.Cancel(operation);
            }

            return Task.CompletedTask;
        }

        public static Task RetryOperationAsync(this CoreOperationQueueService operationQueueService, string operationId)
        {
            var operation = operationQueueService.Operations.FirstOrDefault(o => o.Id == operationId);
            if (operation != null)
            {
                operationQueueService.Retry(operation);
            }

            return Task.CompletedTask;
        }

        public static Task ClearCompletedOperationsAsync(this CoreOperationQueueService operationQueueService)
        {
            operationQueueService.ClearHistory();
            return Task.CompletedTask;
        }

        public static Task ClearQueueAsync(this CoreOperationQueueService operationQueueService)
        {
            foreach (var operation in operationQueueService.Operations
                .Where(o => o.Status == Winhance.Core.Features.FileManager.Interfaces.OperationStatus.Queued)
                .ToList())
            {
                operationQueueService.Cancel(operation);
            }

            return Task.CompletedTask;
        }

        private static CoreFileItem CreateFileItemFromPath(string path)
        {
            if (Directory.Exists(path))
            {
                var directory = new DirectoryInfo(path);
                return new CoreFileItem
                {
                    Name = directory.Name,
                    FullPath = directory.FullName,
                    LastModified = directory.LastWriteTime,
                    Created = directory.CreationTime,
                    AccessedDate = directory.LastAccessTime,
                    Attributes = directory.Attributes,
                    IsDirectory = true,
                    IsHidden = (directory.Attributes & FileAttributes.Hidden) != 0,
                    IsSystem = (directory.Attributes & FileAttributes.System) != 0,
                    IsReadOnly = (directory.Attributes & FileAttributes.ReadOnly) != 0
                };
            }

            if (File.Exists(path))
            {
                var file = new FileInfo(path);
                return new CoreFileItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    Size = file.Length,
                    LastModified = file.LastWriteTime,
                    Created = file.CreationTime,
                    AccessedDate = file.LastAccessTime,
                    Extension = file.Extension,
                    Attributes = file.Attributes,
                    IsDirectory = false,
                    IsHidden = (file.Attributes & FileAttributes.Hidden) != 0,
                    IsSystem = (file.Attributes & FileAttributes.System) != 0,
                    IsReadOnly = (file.Attributes & FileAttributes.ReadOnly) != 0
                };
            }

            return new CoreFileItem
            {
                Name = Path.GetFileName(path),
                FullPath = path
            };
        }

        private static BitmapImage LoadBitmapFromFile(string path)
        {
            using var stream = File.OpenRead(path);
            return LoadBitmapFromStream(stream);
        }

        private static BitmapImage LoadBitmapFromStream(Stream stream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static ColumnConfigurationItem ToColumnConfigurationItem(CoreColumnDefinition column)
        {
            return new ColumnConfigurationItem
            {
                Name = column.Id,
                DisplayName = column.DisplayName,
                Width = column.Width,
                IsVisible = column.IsVisible,
                Order = column.DisplayIndex,
                Alignment = column.Alignment.ToString()
            };
        }

        private static T? GetObjectProperty<T>(object item, string name)
        {
            var property = item.GetType().GetProperty(name);
            if (property?.GetValue(item) is T typed)
            {
                return typed;
            }

            return default;
        }

        private static CoreViewMode ToCoreViewMode(string mode)
        {
            return mode.Trim().Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase).ToLowerInvariant() switch
            {
                "icons" or "mediumicons" => CoreViewMode.MediumIcons,
                "smallicons" => CoreViewMode.SmallIcons,
                "largeicons" => CoreViewMode.LargeIcons,
                "extralargeicons" => CoreViewMode.ExtraLargeIcons,
                "thumbnail" or "thumbnails" => CoreViewMode.Thumbnails,
                "tiles" => CoreViewMode.Tiles,
                "list" => CoreViewMode.List,
                "content" => CoreViewMode.Content,
                "columns" => CoreViewMode.Columns,
                _ => CoreViewMode.Details
            };
        }

        private static string ToUiViewMode(CoreViewMode mode)
        {
            return mode switch
            {
                CoreViewMode.SmallIcons or CoreViewMode.MediumIcons or CoreViewMode.LargeIcons or CoreViewMode.ExtraLargeIcons => "Icons",
                CoreViewMode.Thumbnails => "Thumbnail",
                CoreViewMode.Tiles => "Tiles",
                CoreViewMode.List => "List",
                CoreViewMode.Content => "Content",
                CoreViewMode.Columns => "Columns",
                _ => "Details"
            };
        }

        private static async Task<bool> FilesHaveSameContent(string leftFilePath, string rightFilePath)
        {
            var leftInfo = new FileInfo(leftFilePath);
            var rightInfo = new FileInfo(rightFilePath);
            if (leftInfo.Length != rightInfo.Length)
            {
                return false;
            }

            var leftBuffer = new byte[1024 * 1024];
            var rightBuffer = new byte[1024 * 1024];

            await using var leftStream = File.OpenRead(leftFilePath);
            await using var rightStream = File.OpenRead(rightFilePath);

            while (true)
            {
                var leftRead = await leftStream.ReadAsync(leftBuffer);
                var rightRead = await rightStream.ReadAsync(rightBuffer);
                if (leftRead != rightRead)
                {
                    return false;
                }

                if (leftRead == 0)
                {
                    return true;
                }

                for (var i = 0; i < leftRead; i++)
                {
                    if (leftBuffer[i] != rightBuffer[i])
                    {
                        return false;
                    }
                }
            }
        }
    }

    public class ViewModeSettings
    {
        public string ViewMode { get; set; } = "Details";
        public int IconSize { get; set; } = 48;
        public bool ShowFileExtensions { get; set; } = true;
        public bool ShowHiddenFiles { get; set; }
        public bool ShowSystemFiles { get; set; }
        public bool ShowThumbnails { get; set; } = true;
        public int ThumbnailSize { get; set; } = 96;
        public bool GroupItems { get; set; }
        public string GroupBy { get; set; } = "None";
    }

    public class ColumnConfigurationItem
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public double Width { get; set; }
        public bool IsVisible { get; set; }
        public int Order { get; set; }
        public string Alignment { get; set; } = "Left";
    }

    public class FileAttributeInfo
    {
        public bool IsArchive { get; set; }
        public bool IsHidden { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsSystem { get; set; }
        public bool IsCompressed { get; set; }
        public bool IsEncrypted { get; set; }
    }

    public class FileTimestampInfo
    {
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Accessed { get; set; }
    }

    public class PatternOptions
    {
        public string Pattern { get; set; } = "*";
        public bool UseRegex { get; set; }
        public bool CaseSensitive { get; set; }
    }

    public class OperationQueuedEventArgs : EventArgs
    {
        public Winhance.Core.Features.FileManager.Interfaces.FileOperation Operation { get; set; } = null!;
    }

    public class OperationStartedEventArgs : EventArgs
    {
        public string OperationId { get; set; } = string.Empty;
    }

    public class OperationCompletedEventArgs : EventArgs
    {
        public string OperationId { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public class OperationProgressEventArgs : EventArgs
    {
        public string OperationId { get; set; } = string.Empty;
        public int ProgressPercentage { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public double BytesPerSecond { get; set; }
    }

    public class OperationFailedEventArgs : EventArgs
    {
        public string OperationId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class FolderAnalysisResult
    {
        public List<FolderSizeItem> Folders { get; set; } = new();
        public List<FileSizeItem> Files { get; set; } = new();
    }

    public class ScanProgress
    {
        public double ProgressPercentage { get; set; }
        public string CurrentFolder { get; set; } = string.Empty;
        public int FilesScanned { get; set; }
        public int FoldersScanned { get; set; }
    }

    public class ScanResult
    {
        public long TotalSize { get; set; }
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        public List<ScanFolderResult> Folders { get; set; } = new();
        public List<ScanFileResult> Files { get; set; } = new();
        public List<ScanFileTypeResult> FileTypes { get; set; } = new();
    }

    public class ScanFolderResult
    {
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public int FileCount { get; set; }
    }

    public class ScanFileResult
    {
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime ModifiedDate { get; set; }
    }

    public class ScanFileTypeResult
    {
        public string Extension { get; set; } = string.Empty;
        public int Count { get; set; }
        public long TotalSize { get; set; }
    }

}
