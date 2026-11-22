using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services
{
    /// <summary>
    /// Robust implementation of file organization with AI-powered categorization and rollback support.
    /// </summary>
    public class OrganizerService : IOrganizerService
    {
        private readonly ILogService _logService;
        private readonly string _transactionLogPath;
        private readonly string _rulesPath;
        private readonly Dictionary<string, string[]> _fileTypeCategories;

        public OrganizerService(ILogService logService)
        {
            _logService = logService;

            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Winhance", "FileManager");

            _transactionLogPath = Path.Combine(appDataPath, "organize-transactions");
            _rulesPath = Path.Combine(appDataPath, "organize-rules");

            Directory.CreateDirectory(_transactionLogPath);
            Directory.CreateDirectory(_rulesPath);

            _fileTypeCategories = InitializeFileTypeCategories();
        }

        public async Task<OrganizationPlan> AnalyzeAsync(
            string sourcePath,
            OrganizationStrategy strategy,
            CancellationToken cancellationToken = default)
        {
            var plan = new OrganizationPlan
            {
                SourcePath = sourcePath,
                Strategy = strategy,
                AnalysisDate = DateTime.UtcNow,
            };

            var categories = new Dictionary<string, OrganizationCategory>();

            await Task.Run(
                () =>
            {
                if (!Directory.Exists(sourcePath))
                {
                    _logService.Log(LogLevel.Warning, $"Source path not found: {sourcePath}");
                    return;
                }

                var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.TopDirectoryOnly);

                foreach (var filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        var categoryName = GetCategoryName(fileInfo, strategy);
                        var destinationFolder = Path.Combine(sourcePath, categoryName);

                        if (!categories.TryGetValue(categoryName, out var category))
                        {
                            category = new OrganizationCategory
                            {
                                Name = categoryName,
                                DestinationFolder = destinationFolder,
                                Items = new List<OrganizationItem>(),
                            };
                            categories[categoryName] = category;
                        }

                        ((List<OrganizationItem>)category.Items).Add(new OrganizationItem
                        {
                            SourcePath = filePath,
                            DestinationPath = Path.Combine(destinationFolder, fileInfo.Name),
                            FileName = fileInfo.Name,
                            Size = fileInfo.Length,
                            DateModified = fileInfo.LastWriteTime,
                            Extension = fileInfo.Extension,
                        });

                        category.FileCount++;
                        category.TotalSize += fileInfo.Length;
                    }
                    catch (Exception ex)
                    {
                        _logService.Log(LogLevel.Warning, $"Error analyzing file {filePath}: {ex.Message}");
                    }
                }
            }, cancellationToken);

            plan.Categories = categories.Values.OrderByDescending(c => c.FileCount).ToList();
            plan.TotalFiles = plan.Categories.Sum(c => c.FileCount);
            plan.TotalSize = plan.Categories.Sum(c => c.TotalSize);

            return plan;
        }

        public async Task<OrganizationResult> ExecuteAsync(
            OrganizationPlan plan,
            CancellationToken cancellationToken = default)
        {
            var result = new OrganizationResult { Success = true };
            var errors = new List<string>();
            var transactionId = Guid.NewGuid().ToString();
            var transactionLog = new List<OrganizationTransaction>();

            try
            {
                foreach (var category in plan.Categories)
                {
                    // Create destination folder
                    if (!Directory.Exists(category.DestinationFolder))
                    {
                        Directory.CreateDirectory(category.DestinationFolder);
                    }

                    foreach (var item in category.Items)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (File.Exists(item.SourcePath))
                            {
                                var destPath = GetUniqueDestinationPath(item.DestinationPath);

                                await Task.Run(() => File.Move(item.SourcePath, destPath), cancellationToken);

                                transactionLog.Add(new OrganizationTransaction
                                {
                                    SourcePath = item.SourcePath,
                                    DestinationPath = destPath,
                                    Category = category.Name,
                                    Timestamp = DateTime.UtcNow,
                                });

                                result.FilesOrganized++;
                                result.BytesProcessed += item.Size;

                                _logService.Log(LogLevel.Debug, $"Moved: {item.FileName} -> {category.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"{item.FileName}: {ex.Message}");
                            result.FilesFailed++;
                            _logService.Log(LogLevel.Error, $"Failed to move {item.FileName}: {ex.Message}");
                        }
                    }
                }

                // Save transaction log
                await SaveTransactionAsync(transactionId, transactionLog);
                result.TransactionId = transactionId;

                _logService.Log(
                    LogLevel.Info,
                    $"Organization complete: {result.FilesOrganized} files organized, {result.FilesFailed} failed");
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                _logService.Log(LogLevel.Error, $"Organization failed: {ex.Message}");
            }

            result.Errors = errors;
            result.Success = result.FilesFailed == 0;

            return result;
        }

        public async Task<OrganizationResult> UndoAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
        {
            var result = new OrganizationResult { Success = true };
            var errors = new List<string>();

            try
            {
                var transactionPath = Path.Combine(_transactionLogPath, $"{transactionId}.json");

                if (!File.Exists(transactionPath))
                {
                    result.Success = false;
                    errors.Add("Transaction not found");
                    result.Errors = errors;
                    return result;
                }

                var json = await File.ReadAllTextAsync(transactionPath, cancellationToken);
                var transactions = JsonSerializer.Deserialize<List<OrganizationTransaction>>(json)
                    ?? new List<OrganizationTransaction>();

                // Undo in reverse order
                foreach (var transaction in transactions.AsEnumerable().Reverse())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (File.Exists(transaction.DestinationPath))
                        {
                            // Ensure original directory exists
                            var originalDir = Path.GetDirectoryName(transaction.SourcePath);
                            if (!string.IsNullOrEmpty(originalDir) && !Directory.Exists(originalDir))
                            {
                                Directory.CreateDirectory(originalDir);
                            }

                            await Task.Run(
                                () => File.Move(transaction.DestinationPath, transaction.SourcePath),
                                cancellationToken);

                            result.FilesOrganized++;
                            _logService.Log(
                                LogLevel.Debug,
                                $"Restored: {Path.GetFileName(transaction.SourcePath)}");
                        }
                        else
                        {
                            result.FilesSkipped++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{transaction.DestinationPath}: {ex.Message}");
                        result.FilesFailed++;
                    }
                }

                // Clean up empty category folders
                await CleanupEmptyFoldersAsync(transactions, cancellationToken);

                // Delete transaction log on success
                if (result.FilesFailed == 0)
                {
                    File.Delete(transactionPath);
                }

                _logService.Log(
                    LogLevel.Info,
                    $"Undo complete: {result.FilesOrganized} files restored");
            }
            catch (Exception ex)
            {
                result.Success = false;
                _logService.Log(LogLevel.Error, $"Undo failed: {ex.Message}");
            }

            result.Errors = errors;
            result.Success = result.FilesFailed == 0;

            return result;
        }

        public async Task<SpaceRecoveryAnalysis> AnalyzeSpaceRecoveryAsync(
            string driveLetter,
            CancellationToken cancellationToken = default)
        {
            var driveRoot = NormalizeDriveRoot(driveLetter);
            var analysis = new SpaceRecoveryAnalysis
            {
                DriveLetter = driveLetter,
            };

            try
            {
                var drive = new DriveInfo(driveRoot);
                if (drive.IsReady)
                {
                    analysis.TotalSpace = drive.TotalSize;
                    analysis.FreeSpace = drive.AvailableFreeSpace;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine($"Drive info unavailable for {driveLetter}: {ex.Message}");
            }

            var opportunities = new List<RecoveryOpportunity>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // (display name, path, category, description, action, safe to clean automatically, minimum size)
            var candidates = new (string Name, string Path, string Category, string Description, RecoveryAction Action, bool Safe, long MinSize)[]
            {
                ("LM Studio models", Path.Combine(profile, ".lmstudio", "models"), "AI Models", "AI model files that can be relocated to another drive", RecoveryAction.Relocate, false, 1),
                ("Ollama models", Path.Combine(profile, ".ollama", "models"), "AI Models", "AI model files that can be relocated to another drive", RecoveryAction.Relocate, false, 1),
                ("Hugging Face cache", Path.Combine(profile, ".cache", "huggingface", "hub"), "AI Models", "Downloaded model cache that can be relocated to another drive", RecoveryAction.Relocate, false, 1),
                ("NuGet packages", Path.Combine(profile, ".nuget", "packages"), "Development Cache", "Package cache; packages are re-downloaded when needed", RecoveryAction.Review, false, 500L * 1024 * 1024),
                ("Cargo registry", Path.Combine(profile, ".cargo", "registry"), "Development Cache", "Package cache; crates are re-downloaded when needed", RecoveryAction.Review, false, 500L * 1024 * 1024),
                ("npm cache", Path.Combine(profile, ".npm"), "Development Cache", "Package cache; packages are re-downloaded when needed", RecoveryAction.Review, false, 500L * 1024 * 1024),
                ("pip cache", Path.Combine(localAppData, "pip", "cache"), "Development Cache", "Package cache; packages are re-downloaded when needed", RecoveryAction.Review, false, 500L * 1024 * 1024),
                ("Temporary files", Path.GetTempPath(), "System Cache", "Temporary files that can be safely cleaned (files in use are skipped)", RecoveryAction.Clean, true, 100L * 1024 * 1024),
                ("Temporary files", Path.Combine(localAppData, "Temp"), "System Cache", "Temporary files that can be safely cleaned (files in use are skipped)", RecoveryAction.Clean, true, 100L * 1024 * 1024),
            };

            await Task.Run(
                () =>
                {
                    foreach (var c in candidates)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string fullPath;
                        try
                        {
                            fullPath = Path.GetFullPath(c.Path).TrimEnd(Path.DirectorySeparatorChar);
                        }
                        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
                        {
                            continue;
                        }

                        // Only report folders on the drive being analyzed, and each folder once
                        // (GetTempPath() and %LocalAppData%\Temp are normally the same folder).
                        if (!fullPath.StartsWith(driveRoot, StringComparison.OrdinalIgnoreCase) ||
                            !seenPaths.Add(fullPath) ||
                            !Directory.Exists(fullPath))
                        {
                            continue;
                        }

                        var (size, count) = MeasureDirectory(fullPath, cancellationToken);
                        if (size < c.MinSize)
                        {
                            continue;
                        }

                        opportunities.Add(new RecoveryOpportunity
                        {
                            Name = c.Name,
                            Path = fullPath,
                            Size = size,
                            ItemCount = count,
                            Category = c.Category,
                            Description = c.Description,
                            Action = c.Action,
                            RecommendedAction = c.Action,
                            IsSafeToClean = c.Safe,
                            Priority = size > 10L * 1024 * 1024 * 1024 ? "High" : size > 1024L * 1024 * 1024 ? "Medium" : "Low",
                        });
                    }
                },
                cancellationToken);

            analysis.Opportunities = opportunities.OrderByDescending(o => o.Size).ToList();
            analysis.TotalRecoverableSize = opportunities.Sum(o => o.Size);
            analysis.RecoverableSpace = analysis.TotalRecoverableSize;
            return analysis;
        }

        private static string NormalizeDriveRoot(string driveLetter)
        {
            var d = (driveLetter ?? string.Empty).Trim();
            if (d.Length == 0)
            {
                d = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            }

            if (d.Length == 1)
            {
                d += ":";
            }

            if (!d.EndsWith("\\", StringComparison.Ordinal))
            {
                d += "\\";
            }

            return d;
        }

        /// <summary>Total size and file count of a folder tree, skipping anything inaccessible and not following junctions.</summary>
        private static (long Size, int Count) MeasureDirectory(string path, CancellationToken cancellationToken)
        {
            long size = 0;
            var count = 0;
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
            };

            try
            {
                foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", options))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        size += file.Length;
                        count++;
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                System.Diagnostics.Debug.WriteLine($"Skipped inaccessible item: {ex.Message}");
            }

            return (size, count);
        }

        public async Task<OrganizationResult> RelocateModelsAsync(
            string sourcePath,
            string destinationPath,
            bool createSymlinks,
            CancellationToken cancellationToken = default)
        {
            var result = new OrganizationResult { Success = false };
            var errors = new List<string>();
            var transactionId = Guid.NewGuid().ToString();
            var transactionLog = new List<OrganizationTransaction>();

            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || !Directory.Exists(sourcePath))
                {
                    errors.Add("Source folder not found.");
                    result.Errors = errors;
                    return result;
                }

                if (string.IsNullOrWhiteSpace(destinationPath))
                {
                    errors.Add("No destination folder was given.");
                    result.Errors = errors;
                    return result;
                }

                var source = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourcePath));
                var destination = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destinationPath));

                if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase)
                    || destination.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || source.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("The destination cannot be the source folder, inside it, or a parent of it.");
                    result.Errors = errors;
                    return result;
                }

                if (new DirectoryInfo(source).Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    errors.Add("The source folder is already a link (it has probably been relocated before).");
                    result.Errors = errors;
                    return result;
                }

                if (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any())
                {
                    errors.Add("The destination folder must be empty or not exist yet, so nothing is overwritten.");
                    result.Errors = errors;
                    return result;
                }

                Directory.CreateDirectory(destination);
                var sameVolume = string.Equals(
                    Path.GetPathRoot(source), Path.GetPathRoot(destination), StringComparison.OrdinalIgnoreCase);

                // Move entry by entry. Across drives each entry is copied, verified, and only then deleted,
                // so a failure leaves every not-yet-moved entry untouched in the source.
                var entries = Directory.EnumerateFileSystemEntries(source).ToList();
                foreach (var entry in entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var target = Path.Combine(destination, Path.GetFileName(entry));
                    try
                    {
                        var bytes = await Task.Run(() => MoveEntry(entry, target, sameVolume, cancellationToken), cancellationToken);
                        transactionLog.Add(new OrganizationTransaction
                        {
                            SourcePath = entry,
                            DestinationPath = target,
                            Category = "AI Model Relocation",
                            Timestamp = DateTime.UtcNow,
                        });
                        result.FilesOrganized++;
                        result.BytesProcessed += bytes;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{entry}: {ex.Message}");
                        result.FilesFailed++;
                        break; // stop at the first failure; the rest stays where it was
                    }
                }

                if (result.FilesFailed > 0)
                {
                    errors.Add($"Relocation stopped. {result.FilesOrganized} item(s) were moved to {destination}; "
                        + $"the remaining items are still in {source}. No link was created.");
                }
                else if (createSymlinks)
                {
                    if (Directory.EnumerateFileSystemEntries(source).Any())
                    {
                        errors.Add($"New files appeared in {source} during the move; no link was created.");
                        result.FilesFailed++;
                    }
                    else
                    {
                        Directory.Delete(source);
                        var linkError = await CreateSymbolicLinkAsync(source, destination);
                        if (linkError != null)
                        {
                            errors.Add($"Files were moved to {destination}, but the link at {source} could not be "
                                + $"created ({linkError}). Create it manually with: mklink /J \"{source}\" \"{destination}\"");
                            result.FilesFailed++;
                        }
                        else
                        {
                            _logService.Log(LogLevel.Info, $"Created junction: {source} -> {destination}");
                        }
                    }
                }

                await SaveTransactionAsync(transactionId, transactionLog);
                result.TransactionId = transactionId;

                _logService.Log(
                    LogLevel.Info,
                    $"Model relocation: {result.FilesOrganized} item(s), {result.BytesProcessed / (1024 * 1024)} MB moved, {result.FilesFailed} failed");
            }
            catch (OperationCanceledException)
            {
                errors.Add("Relocation was cancelled; items not yet moved are still in the source folder.");
                result.FilesFailed++;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                result.FilesFailed++;
                _logService.Log(LogLevel.Error, $"Relocation failed: {ex.Message}");
            }

            result.Errors = errors;
            result.Success = result.FilesFailed == 0 && errors.Count == 0;
            return result;
        }

        /// <summary>Moves a file or directory; across volumes copies, verifies size, then deletes the source.</summary>
        private static long MoveEntry(string source, string target, bool sameVolume, CancellationToken ct)
        {
            if (File.Exists(source))
            {
                var length = new FileInfo(source).Length;
                if (sameVolume)
                {
                    File.Move(source, target);
                    return length;
                }

                File.Copy(source, target, overwrite: false);
                if (new FileInfo(target).Length != length)
                {
                    File.Delete(target);
                    throw new IOException("Copied file size does not match the original.");
                }

                File.Delete(source);
                return length;
            }

            var sourceBytes = DirectorySize(source);
            if (sameVolume)
            {
                Directory.Move(source, target);
                return sourceBytes;
            }

            CopyDirectory(source, target, ct);
            if (DirectorySize(target) != sourceBytes)
            {
                throw new IOException("Copied folder size does not match the original; the original was kept.");
            }

            Directory.Delete(source, recursive: true);
            return sourceBytes;
        }

        private static void CopyDirectory(string source, string target, CancellationToken ct)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.EnumerateFiles(source))
            {
                ct.ThrowIfCancellationRequested();
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: false);
            }

            foreach (var dir in Directory.EnumerateDirectories(source))
            {
                ct.ThrowIfCancellationRequested();
                if (new DirectoryInfo(dir).Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    throw new IOException($"{dir} is a link; relocate it separately.");
                }

                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)), ct);
            }
        }

        private static long DirectorySize(string path) =>
            new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);

        public async Task<IEnumerable<DuplicateGroup>> FindDuplicatesAsync(
            string searchPath,
            DuplicateSearchMethod method,
            CancellationToken cancellationToken = default)
        {
            var duplicates = new List<DuplicateGroup>();
            var fileGroups = new Dictionary<string, List<DuplicateFile>>();

            await Task.Run(
                () =>
            {
                if (!Directory.Exists(searchPath))
                {
                    return;
                }

                var files = Directory.EnumerateFiles(searchPath, "*", SearchOption.AllDirectories);

                foreach (var filePath in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        string key;

                        switch (method)
                        {
                            case DuplicateSearchMethod.Hash:
                                key = ComputeFileHash(filePath);
                                break;
                            case DuplicateSearchMethod.Name:
                                key = fileInfo.Name.ToLowerInvariant();
                                break;
                            case DuplicateSearchMethod.Size:
                                key = fileInfo.Length.ToString();
                                break;
                            case DuplicateSearchMethod.NameAndSize:
                            default:
                                key = $"{fileInfo.Name.ToLowerInvariant()}_{fileInfo.Length}";
                                break;
                        }

                        if (!fileGroups.TryGetValue(key, out var group))
                        {
                            group = new List<DuplicateFile>();
                            fileGroups[key] = group;
                        }

                        group.Add(new DuplicateFile
                        {
                            Path = filePath,
                            Name = fileInfo.Name,
                            Size = fileInfo.Length,
                            DateModified = fileInfo.LastWriteTime,
                            Hash = method == DuplicateSearchMethod.Hash ? key : null,
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipped inaccessible item: {ex.Message}");
                    }
                }
            }, cancellationToken);

            // Filter to only groups with duplicates
            foreach (var kvp in fileGroups.Where(g => g.Value.Count > 1))
            {
                var group = kvp.Value;
                var totalSize = group.Sum(f => f.Size);
                var wastedSize = totalSize - group.First().Size;

                duplicates.Add(new DuplicateGroup
                {
                    Key = kvp.Key,
                    Files = group,
                    TotalSize = totalSize,
                    WastedSize = wastedSize,
                    FileCount = group.Count,
                });
            }

            return duplicates.OrderByDescending(d => d.WastedSize);
        }

        public async Task<IEnumerable<OrganizationRule>> GetRulesAsync(CancellationToken cancellationToken = default)
        {
            var rules = new List<OrganizationRule>();

            await Task.Run(
                () =>
            {
                try
                {
                    foreach (var file in Directory.GetFiles(_rulesPath, "*.json"))
                    {
                        try
                        {
                            var json = File.ReadAllText(file);
                            var rule = JsonSerializer.Deserialize<OrganizationRule>(json);
                            if (rule != null)
                            {
                                rules.Add(rule);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipped inaccessible item: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipped inaccessible item: {ex.Message}");
                }
            }, cancellationToken);

            return rules.OrderBy(r => r.Priority);
        }

        public async Task SaveRuleAsync(OrganizationRule rule, CancellationToken cancellationToken = default)
        {
            var fileName = $"{rule.Id}.json";
            var filePath = Path.Combine(_rulesPath, fileName);

            var json = JsonSerializer.Serialize(rule, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logService.Log(LogLevel.Info, $"Saved organization rule: {rule.Name}");
        }

        public async Task DeleteRuleAsync(string ruleId, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(_rulesPath, $"{ruleId}.json");

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken);
                _logService.Log(LogLevel.Info, $"Deleted organization rule: {ruleId}");
            }
        }

        private string GetCategoryName(FileInfo fileInfo, OrganizationStrategy strategy)
        {
            return strategy switch
            {
                OrganizationStrategy.ByType => GetTypeCategoryName(fileInfo.Extension),
                OrganizationStrategy.ByDate => fileInfo.LastWriteTime.ToString("yyyy-MM"),
                OrganizationStrategy.BySize => GetSizeCategoryName(fileInfo.Length),
                OrganizationStrategy.ByProject => "Misc",
                OrganizationStrategy.ByAICategory => GetAICategoryName(fileInfo),
                _ => "Other",
            };
        }

        private string GetTypeCategoryName(string extension)
        {
            extension = extension.TrimStart('.').ToLowerInvariant();

            foreach (var category in _fileTypeCategories)
            {
                if (category.Value.Contains(extension))
                {
                    return category.Key;
                }
            }

            return "Other";
        }

        private string GetSizeCategoryName(long size)
        {
            return size switch
            {
                < 1024 => "Tiny (< 1KB)",
                < 1024 * 1024 => "Small (1KB - 1MB)",
                < 10 * 1024 * 1024 => "Medium (1MB - 10MB)",
                < 100 * 1024 * 1024 => "Large (10MB - 100MB)",
                < 1024 * 1024 * 1024 => "Very Large (100MB - 1GB)",
                _ => "Huge (> 1GB)",
            };
        }

        private string GetAICategoryName(FileInfo fileInfo)
        {
            // Simple AI-like categorization based on file patterns
            var name = fileInfo.Name.ToLowerInvariant();
            var ext = fileInfo.Extension.TrimStart('.').ToLowerInvariant();

            if (name.Contains("screenshot", StringComparison.Ordinal) || name.Contains("screen", StringComparison.Ordinal) || name.Contains("capture", StringComparison.Ordinal))
            {
                return "Screenshots";
            }

            if (name.Contains("download", StringComparison.Ordinal) || fileInfo.DirectoryName?.Contains("Downloads", StringComparison.Ordinal) == true)
            {
                return "Downloads";
            }

            if (ext == "pdf" && (name.Contains("invoice", StringComparison.Ordinal) || name.Contains("receipt", StringComparison.Ordinal)))
            {
                return "Financial Documents";
            }

            if (ext == "pdf" && (name.Contains("resume", StringComparison.Ordinal) || name.Contains("cv", StringComparison.Ordinal)))
            {
                return "Career Documents";
            }

            if (new[] { "jpg", "jpeg", "png", "gif" }.Contains(ext) && name.Contains("img_", StringComparison.Ordinal))
            {
                return "Camera Photos";
            }

            return GetTypeCategoryName(fileInfo.Extension);
        }

        private string GetUniqueDestinationPath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }

            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var name = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var counter = 1;

            string newPath;
            do
            {
                newPath = Path.Combine(directory, $"{name} ({counter}){extension}");
                counter++;
            }
            while (File.Exists(newPath));

            return newPath;
        }

        private long CalculateDirectorySize(string path)
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += new FileInfo(file).Length;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Skipped inaccessible item: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skipped inaccessible item: {ex.Message}");
            }

            return size;
        }

        private string ComputeFileHash(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var md5 = MD5.Create();
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
            }
            catch (Exception)
            {
                return Guid.NewGuid().ToString();
            }
        }

        private async Task SaveTransactionAsync(string transactionId, List<OrganizationTransaction> transactions)
        {
            var filePath = Path.Combine(_transactionLogPath, $"{transactionId}.json");
            var json = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        private async Task CleanupEmptyFoldersAsync(
            List<OrganizationTransaction> transactions,
            CancellationToken cancellationToken)
        {
            var folders = transactions
                .Select(t => Path.GetDirectoryName(t.DestinationPath))
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct();

            foreach (var folder in folders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                    {
                        await Task.Run(() => Directory.Delete(folder), cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Skipped inaccessible item: {ex.Message}");
                }
            }
        }

        /// <summary>Creates a directory junction (no admin needed). Returns null on success, else the error.</summary>
        private static async Task<string?> CreateSymbolicLinkAsync(string linkPath, string targetPath)
        {
            return await Task.Run(() =>
            {
                using var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    },
                };
                process.Start();
                var stderr = process.StandardError.ReadToEnd();
                process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return process.ExitCode == 0 && Directory.Exists(linkPath)
                    ? null
                    : (string.IsNullOrWhiteSpace(stderr) ? $"mklink exit code {process.ExitCode}" : stderr.Trim());
            });
        }

        private Dictionary<string, string[]> InitializeFileTypeCategories()
        {
            return new Dictionary<string, string[]>
            {
                ["Documents"] = new[] { "doc", "docx", "pdf", "txt", "rtf", "odt", "xls", "xlsx", "ppt", "pptx", "csv" },
                ["Images"] = new[] { "jpg", "jpeg", "png", "gif", "bmp", "svg", "webp", "ico", "tiff", "heic", "raw" },
                ["Videos"] = new[] { "mp4", "avi", "mkv", "mov", "wmv", "flv", "webm", "m4v", "mpeg", "mpg" },
                ["Audio"] = new[] { "mp3", "wav", "flac", "aac", "ogg", "wma", "m4a", "opus" },
                ["Archives"] = new[] { "zip", "rar", "7z", "tar", "gz", "bz2", "xz", "iso" },
                ["Code"] = new[] { "cs", "py", "js", "ts", "java", "cpp", "c", "h", "rs", "go", "rb", "php", "html", "css", "json", "xml", "yaml", "yml" },
                ["Executables"] = new[] { "exe", "msi", "dll", "bat", "cmd", "ps1", "sh" },
                ["Fonts"] = new[] { "ttf", "otf", "woff", "woff2", "eot" },
                ["3D Models"] = new[] { "obj", "fbx", "stl", "blend", "dae", "3ds", "gltf", "glb" },
                ["AI Models"] = new[] { "gguf", "ggml", "safetensors", "bin", "pt", "pth", "onnx", "h5" },
            };
        }

        private class OrganizationTransaction
        {
            public string SourcePath { get; set; } = string.Empty;

            public string DestinationPath { get; set; } = string.Empty;

            public string Category { get; set; } = string.Empty;

            public DateTime Timestamp { get; set; }
        }
    }
}
