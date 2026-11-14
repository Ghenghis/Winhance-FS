using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Infrastructure.Features.FileManager.Services
{
    public class TreeMapVisualizationService : ITreeMapVisualizationService
    {
        private readonly ILogService _logService;
        private readonly IFileManagerService _fileManager;

        public TreeMapVisualizationService(ILogService logService, IFileManagerService fileManager)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        }

        public async Task<TreeMapData> GenerateTreeMapAsync(string directoryPath, TreeMapOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(directoryPath);
            ArgumentNullException.ThrowIfNull(options);

            var analyzer = new DirectoryAnalyzer(_logService);
            var analysis = await analyzer.AnalyzeRecursiveAsync(directoryPath, options.MaxDepth, cancellationToken);

            return new TreeMapData
            {
                RootPath = directoryPath,
                Nodes = BuildTreeMapNodes(analysis, options),
                TotalSize = analysis.TotalSize,
                FileCount = analysis.FileCount,
                DirectoryCount = analysis.DirectoryCount,
                GeneratedAt = DateTime.UtcNow,
                Options = options,
                Metadata = options.IncludeMetadata ? analysis.Metadata : new Dictionary<string, object>()
            };
        }

        public async Task<List<StorageOptimization>> AnalyzeStorageAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(directoryPath);

            var analyzer = new DirectoryAnalyzer(_logService);
            var analysis = await analyzer.AnalyzeRecursiveAsync(directoryPath, int.MaxValue, cancellationToken);
            var optimizations = new List<StorageOptimization>();

            // Large files analysis
            var largeFiles = analysis.Files.Where(f => f.Size > 100 * 1024 * 1024).OrderByDescending(f => f.Size).ToList();
            if (largeFiles.Any())
            {
                optimizations.Add(new StorageOptimization
                {
                    Type = OptimizationType.LargeFiles,
                    Description = $"Found {largeFiles.Count} files larger than 100MB",
                    PotentialSavings = largeFiles.Sum(f => f.Size),
                    Items = largeFiles.Select(f => f.FullPath).ToList(),
                    Confidence = 0.9,
                    Priority = largeFiles.Sum(f => f.Size) > 1024L * 1024 * 1024 ? 1 : 2
                });
            }

            // Duplicate files analysis
            var duplicateGroups = await FindDuplicateFilesAsync(analysis.Files, cancellationToken);
            if (duplicateGroups.Any())
            {
                var duplicateSize = duplicateGroups.SelectMany(g => g.Skip(1)).Sum(f => f.Size);
                optimizations.Add(new StorageOptimization
                {
                    Type = OptimizationType.Duplicates,
                    Description = $"Found {duplicateGroups.Sum(g => g.Count() - 1)} duplicate files",
                    PotentialSavings = duplicateSize,
                    Items = duplicateGroups.SelectMany(g => g.Skip(1)).Select(f => f.FullPath).ToList(),
                    Confidence = 0.95,
                    Priority = duplicateSize > 500 * 1024 * 1024 ? 1 : 2
                });
            }

            return optimizations.OrderByDescending(o => o.PotentialSavings).ToList();
        }

        public async Task<TreeMapVisualization> CreateVisualizationAsync(TreeMapData data, VisualizationOptions options, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(options);

            var visualization = new TreeMapVisualization
            {
                Id = Guid.NewGuid().ToString(),
                RootNode = data,
                GeneratedAt = DateTime.UtcNow,
                Width = options.Width,
                Height = options.Height,
                InteractiveElements = new List<InteractiveElement>()
            };

            await ApplySquarifiedLayoutAsync(visualization, options, cancellationToken);
            ApplyColorScheme(visualization, options.ColorScheme);
            AddInteractiveElements(visualization, options);

            return visualization;
        }

        public async Task<List<UsageTrend>> GetUsageTrendsAsync(string directoryPath, TimeRange timeRange, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(directoryPath);
            ArgumentNullException.ThrowIfNull(timeRange);

            var trends = new List<UsageTrend>();
            var currentDate = timeRange.StartDate;

            while (currentDate <= timeRange.EndDate)
            {
                var snapshot = await CaptureDirectorySnapshotAsync(directoryPath, currentDate, cancellationToken);
                trends.Add(new UsageTrend
                {
                    Date = currentDate,
                    TotalSize = snapshot.TotalSize,
                    FileCount = snapshot.FileCount,
                    DirectoryCount = snapshot.DirectoryCount,
                    LargestFiles = snapshot.LargestFiles.Take(10).ToArray(),
                    GrowthRate = CalculateGrowthRate(trends.LastOrDefault(), snapshot)
                });

                currentDate = currentDate.AddDays(timeRange.IntervalDays);
            }

            return trends;
        }

        public async Task<DirectoryAnalysisResult> AnalyzeDirectoryAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(directoryPath);

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            var analyzer = new DirectoryAnalyzer(_logService);
            var result = await analyzer.AnalyzeRecursiveAsync(directoryPath, 10, cancellationToken);

            result.AverageFileSize = result.FileCount > 0 ? result.TotalSize / result.FileCount : 0;
            result.LargestFile = result.Files.OrderByDescending(f => f.Size).FirstOrDefault();
            result.OldestFile = result.Files.OrderBy(f => f.DateModified).FirstOrDefault();
            result.NewestFile = result.Files.OrderByDescending(f => f.DateModified).FirstOrDefault();

            result.FileTypeDistribution = result.Files
                .GroupBy(f => Path.GetExtension(f.FullPath)?.ToLowerInvariant() ?? "(no extension)")
                .ToDictionary(g => g.Key, g => new FileTypeInfo
                {
                    Count = g.Count(),
                    TotalSize = g.Sum(f => f.Size),
                    AverageSize = g.Average(f => f.Size)
                });

            return result;
        }

        public async Task<DirectorySnapshot> CreateSnapshotAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(directoryPath);

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

            var analyzer = new DirectoryAnalyzer(_logService);
            var analysis = await analyzer.AnalyzeRecursiveAsync(directoryPath, int.MaxValue, cancellationToken);

            var snapshot = new DirectorySnapshot
            {
                Id = Guid.NewGuid().ToString(),
                Path = directoryPath,
                Timestamp = DateTime.UtcNow,
                TotalSize = analysis.TotalSize,
                FileCount = analysis.FileCount,
                DirectoryCount = analysis.DirectoryCount,
                LargestFiles = analysis.Files.OrderByDescending(f => f.Size).Take(50).ToArray(),
                FileHashes = new Dictionary<string, string>()
            };

            // Create file hash catalog for important files
            var importantFiles = analysis.Files.Where(f => f.Size > 1024).Take(1000).ToList();
            foreach (var file in importantFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var hash = await CalculateFileHashAsync(file.FullPath, cancellationToken);
                    var relativePath = Path.GetRelativePath(directoryPath, file.FullPath);
                    snapshot.FileHashes[relativePath] = hash;
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.Warning, $"Failed to hash file {file.FullPath}: {ex.Message}");
                }
            }

            return snapshot;
        }

        public Task<List<DirectoryChange>> CompareSnapshotsAsync(DirectorySnapshot before, DirectorySnapshot after, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(before);
            ArgumentNullException.ThrowIfNull(after);

            var changes = new List<DirectoryChange>();

            if (after.TotalSize != before.TotalSize)
            {
                changes.Add(new DirectoryChange
                {
                    Path = after.Path,
                    Type = ChangeType.SizeChanged,
                    Timestamp = after.Timestamp,
                    SizeBefore = before.TotalSize,
                    SizeAfter = after.TotalSize,
                    Details = $"Total size changed from {FormatBytes(before.TotalSize)} to {FormatBytes(after.TotalSize)}"
                });
            }

            // Compare file hashes
            var beforeHashes = before.FileHashes ?? new Dictionary<string, string>();
            var afterHashes = after.FileHashes ?? new Dictionary<string, string>();

            foreach (var file in afterHashes.Keys.Except(beforeHashes.Keys))
            {
                changes.Add(new DirectoryChange
                {
                    Path = Path.Combine(after.Path, file),
                    Type = ChangeType.Added,
                    Timestamp = after.Timestamp,
                    Details = $"File added: {file}"
                });
            }

            foreach (var file in beforeHashes.Keys.Except(afterHashes.Keys))
            {
                changes.Add(new DirectoryChange
                {
                    Path = Path.Combine(before.Path, file),
                    Type = ChangeType.Deleted,
                    Timestamp = after.Timestamp,
                    Details = $"File deleted: {file}"
                });
            }

            return Task.FromResult(changes.OrderBy(c => c.Path).ToList());
        }

        public async Task<List<FileAnalysisItem>> AnalyzeFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filePaths);

            var results = new List<FileAnalysisItem>();

            foreach (var filePath in filePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (!File.Exists(filePath)) continue;

                    var fileInfo = new FileInfo(filePath);
                    var analysisItem = new FileAnalysisItem
                    {
                        Name = fileInfo.Name,
                        FullPath = fileInfo.FullName,
                        Size = fileInfo.Length,
                        DateModified = fileInfo.LastWriteTime,
                        DateCreated = fileInfo.CreationTime,
                        DateAccessed = fileInfo.LastAccessTime,
                        Extension = fileInfo.Extension,
                        IsDirectory = false,
                        Category = DetermineFileCategory(fileInfo.Extension),
                        MimeType = DetermineMimeType(filePath),
                        IsCompressible = IsFileCompressible(fileInfo.Extension)
                    };

                    if (fileInfo.Length > 0 && fileInfo.Length < 100 * 1024 * 1024)
                    {
                        analysisItem.Hash = await CalculateFileHashAsync(filePath, cancellationToken);
                    }

                    results.Add(analysisItem);
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.Warning, $"Failed to analyze file {filePath}: {ex.Message}");
                }
            }

            return results;
        }

        // Private helper methods
        private List<TreeMapNode> BuildTreeMapNodes(DirectoryAnalysisResult analysis, TreeMapOptions options)
        {
            var nodes = new List<TreeMapNode>();
            var totalSize = analysis.TotalSize;

            var layout = new SquarifiedTreeMapLayout();
            var items = analysis.Files.Cast<ITreeMapItem>().Concat(analysis.Directories.Cast<ITreeMapItem>());
            var rectangles = layout.Calculate(items, options.Width, options.Height);

            for (int i = 0; i < rectangles.Count; i++)
            {
                var item = rectangles[i].Item;
                var rect = rectangles[i].Rectangle;

                nodes.Add(new TreeMapNode
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = item.Name,
                    FullPath = item.FullPath,
                    Size = item.Size,
                    SizePercentage = (double)item.Size / totalSize * 100,
                    IsDirectory = item.IsDirectory,
                    Rectangle = rect,
                    Color = GetFileTypeColor(item.FullPath, item.IsDirectory),
                    Level = 0,
                    Children = new List<TreeMapNode>()
                });
            }

            return nodes;
        }

        private async Task<IGrouping<string, FileAnalysisItem>[]> FindDuplicateFilesAsync(IEnumerable<FileAnalysisItem> files, CancellationToken cancellationToken)
        {
            var duplicates = new List<IGrouping<string, FileAnalysisItem>>();
            var sizeGroups = files.Where(f => f.Size > 0).GroupBy(f => f.Size).Where(g => g.Count() > 1);

            foreach (var sizeGroup in sizeGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var hashGroups = new Dictionary<string, List<FileAnalysisItem>>();

                foreach (var file in sizeGroup)
                {
                    try
                    {
                        var hash = await CalculateFileHashAsync(file.FullPath, cancellationToken);
                        if (!hashGroups.ContainsKey(hash)) hashGroups[hash] = new List<FileAnalysisItem>();
                        hashGroups[hash].Add(file);
                    }
                    catch { }
                }

                foreach (var hashGroup in hashGroups.Values.Where(g => g.Count > 1))
                {
                    var grouping = hashGroup.GroupBy(f => hashGroup.First().Hash).First();
                    duplicates.Add(grouping);
                }
            }

            return duplicates.ToArray();
        }

        private Task ApplySquarifiedLayoutAsync(TreeMapVisualization visualization, VisualizationOptions options, CancellationToken cancellationToken)
        {
            if (visualization.RootNode?.Nodes == null) return Task.CompletedTask;

            var layout = new SquarifiedTreeMapLayout();
            var items = visualization.RootNode.Nodes.Cast<ITreeMapItem>().ToList();
            var rectangles = layout.Calculate(items, options.Width, options.Height);

            for (int i = 0; i < rectangles.Count && i < visualization.RootNode.Nodes.Count; i++)
            {
                visualization.RootNode.Nodes[i].Rectangle = rectangles[i].Rectangle;
            }

            return Task.CompletedTask;
        }

        private void ApplyColorScheme(TreeMapVisualization visualization, ColorScheme colorScheme)
        {
            if (visualization.RootNode?.Nodes == null) return;
            foreach (var node in visualization.RootNode.Nodes)
            {
                node.Color = GetFileTypeColor(node.FullPath, node.IsDirectory);
            }
        }

        private void AddInteractiveElements(TreeMapVisualization visualization, VisualizationOptions options)
        {
            if (visualization.RootNode?.Nodes == null) return;

            foreach (var node in visualization.RootNode.Nodes)
            {
                visualization.InteractiveElements.Add(new InteractiveElement
                {
                    NodeId = node.Id,
                    BoundingBox = new BoundingBox { X = node.Rectangle.X, Y = node.Rectangle.Y, Width = node.Rectangle.Width, Height = node.Rectangle.Height },
                    Tooltip = $"Name: {node.Name}\\nSize: {FormatBytes((long)node.Size)}\\nType: {(node.IsDirectory ? "Directory" : "File")}",
                    ClickAction = node.IsDirectory ? "navigate" : "preview"
                });
            }
        }

        private string GetFileTypeColor(string filePath, bool isDirectory)
        {
            if (isDirectory) return "#5C7CFA";
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".exe" or ".msi" or ".dll" => "#FF6B6B",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "#4ECDC4",
                ".mp4" or ".avi" or ".mkv" => "#45B7D1",
                ".mp3" or ".wav" or ".flac" => "#96CEB4",
                ".txt" or ".docx" or ".pdf" => "#FECA57",
                ".zip" or ".rar" or ".7z" => "#FF9FF3",
                ".cs" or ".js" or ".py" => "#A8E6CF",
                _ => "#BDC3C7"
            };
        }

        private async Task<DirectorySnapshot> CaptureDirectorySnapshotAsync(string path, DateTime date, CancellationToken cancellationToken)
        {
            var analyzer = new DirectoryAnalyzer(_logService);
            var analysis = await analyzer.AnalyzeRecursiveAsync(path, 3, cancellationToken);

            return new DirectorySnapshot
            {
                Date = date,
                TotalSize = analysis.TotalSize,
                FileCount = analysis.FileCount,
                DirectoryCount = analysis.DirectoryCount,
                LargestFiles = analysis.Files.OrderByDescending(f => f.Size).Take(10).ToArray()
            };
        }

        private double CalculateGrowthRate(UsageTrend? previous, DirectorySnapshot current)
        {
            if (previous == null) return 0;
            var sizeDifference = current.TotalSize - previous.TotalSize;
            return previous.TotalSize == 0 ? 0 : (double)sizeDifference / previous.TotalSize * 100;
        }

        private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hashBytes);
        }

        private FileCategory DetermineFileCategory(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return FileCategory.Other;
            extension = extension.ToLowerInvariant();
            return extension switch
            {
                ".txt" or ".doc" or ".docx" or ".pdf" => FileCategory.Document,
                ".jpg" or ".jpeg" or ".png" or ".gif" => FileCategory.Image,
                ".mp4" or ".avi" or ".mkv" => FileCategory.Video,
                ".mp3" or ".wav" or ".flac" => FileCategory.Audio,
                ".zip" or ".rar" or ".7z" => FileCategory.Archive,
                ".cs" or ".js" or ".py" => FileCategory.Code,
                ".exe" or ".msi" or ".dll" => FileCategory.Executable,
                _ => FileCategory.Other
            };
        }

        private string DetermineMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".json" => "application/json",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"
            };
        }

        private bool IsFileCompressible(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;
            extension = extension.ToLowerInvariant();
            var compressedFormats = new HashSet<string> { ".zip", ".rar", ".7z", ".jpg", ".jpeg", ".png", ".mp3", ".mp4" };
            return !compressedFormats.Contains(extension);
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            var counter = 0;
            var number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }

    // Supporting classes
    public class DirectoryAnalyzer
    {
        private readonly ILogService _logService;

        public DirectoryAnalyzer(ILogService logService)
        {
            _logService = logService;
        }

        public async Task<DirectoryAnalysisResult> AnalyzeRecursiveAsync(string rootPath, int maxDepth, CancellationToken cancellationToken)
        {
            var result = new DirectoryAnalysisResult
            {
                RootPath = rootPath,
                Files = new List<FileAnalysisItem>(),
                Directories = new List<DirectoryAnalysisItem>(),
                Metadata = new Dictionary<string, object>()
            };

            await AnalyzeDirectoryRecursive(rootPath, result, 0, maxDepth, cancellationToken);

            result.TotalSize = result.Files.Sum(f => f.Size);
            result.FileCount = result.Files.Count;
            result.DirectoryCount = result.Directories.Count;

            return result;
        }

        private async Task AnalyzeDirectoryRecursive(string path, DirectoryAnalysisResult result, int currentDepth, int maxDepth, CancellationToken cancellationToken)
        {
            if (currentDepth > maxDepth) return;

            try
            {
                var dirInfo = new DirectoryInfo(path);
                
                foreach (var file in dirInfo.GetFiles())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.Files.Add(new FileAnalysisItem
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        Size = file.Length,
                        DateModified = file.LastWriteTime,
                        Extension = file.Extension,
                        IsDirectory = false
                    });
                }

                foreach (var dir in dirInfo.GetDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.Directories.Add(new DirectoryAnalysisItem
                    {
                        Name = dir.Name,
                        FullPath = dir.FullName,
                        DateModified = dir.LastWriteTime,
                        IsDirectory = true,
                        Level = currentDepth
                    });
                    
                    await AnalyzeDirectoryRecursive(dir.FullName, result, currentDepth + 1, maxDepth, cancellationToken);
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logService.Log(LogLevel.Warning, $"Access denied: {path}");
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Error analyzing {path}: {ex.Message}");
            }
        }
    }

    public class SquarifiedTreeMapLayout
    {
        public List<LayoutResult> Calculate(IEnumerable<ITreeMapItem> items, int width, int height)
        {
            var results = new List<LayoutResult>();
            var sortedItems = items.OrderByDescending(i => i.Size).ToList();
            
            var currentX = 0;
            var currentY = 0;

            foreach (var item in sortedItems)
            {
                var itemWidth = width / 4;
                var itemHeight = height / 4;

                results.Add(new LayoutResult
                {
                    Item = (TreeMapNode)item,
                    Rectangle = new BoundingBox { X = currentX, Y = currentY, Width = itemWidth, Height = itemHeight }
                });

                currentX += itemWidth;
                if (currentX >= width)
                {
                    currentX = 0;
                    currentY += itemHeight;
                }
            }

            return results;
        }
    }
}
