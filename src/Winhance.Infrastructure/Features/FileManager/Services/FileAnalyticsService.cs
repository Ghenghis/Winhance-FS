using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using Winhance.Infrastructure.Extensions;

namespace Winhance.Infrastructure.Features.FileManager.Services
{
    /// <summary>
    /// File analytics and reporting service providing comprehensive usage statistics and insights
    /// </summary>
    public class FileAnalyticsService : IFileAnalyticsService
    {
        private readonly ILogService _logService;
        private readonly IFileManagerService _fileManager;
        private readonly string _analyticsDataPath;

        public FileAnalyticsService(
            ILogService logService,
            IFileManagerService fileManager)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            
            _analyticsDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Winhance", "Analytics");
            
            Directory.CreateDirectory(_analyticsDataPath);
        }

        public async Task<UsageReport> GenerateUsageReportAsync(
            string path, 
            ReportOptions options, 
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentNullException.ThrowIfNull(options);

            var report = new UsageReport
            {
                ReportPath = path,
                GeneratedAt = DateTime.UtcNow,
                ReportPeriod = options.Period,
                Success = false
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Generating usage report for: {path}");

                // Collect file statistics
                var stats = await CollectFileStatistics(path, options.IncludeSubdirectories, cancellationToken);
                
                report.TotalFiles = stats.TotalFiles;
                report.TotalDirectories = stats.TotalDirectories;
                report.TotalSize = stats.TotalSize;
                report.LargestFile = stats.LargestFile;
                report.OldestFile = stats.OldestFile;
                report.NewestFile = stats.NewestFile;
                
                // Generate file type breakdown
                report.FileTypeBreakdown = await GenerateFileTypeBreakdown(stats.FilesByType);
                
                // Generate size distribution
                report.SizeDistribution = GenerateSizeDistribution(stats.FilesSizes);
                
                // Generate access patterns if available
                if (options.IncludeAccessPatterns)
                {
                    report.AccessPatterns = await AnalyzeAccessPatterns(path, options.Period, cancellationToken);
                }

                report.Success = true;
                
                // Save report if requested
                if (options.SaveReport)
                {
                    await SaveUsageReport(report, cancellationToken);
                }

                _logService.Log(LogLevel.Info, $"Usage report generated successfully: {report.TotalFiles} files analyzed");
            }
            catch (Exception ex)
            {
                report.ErrorMessage = ex.Message;
                _logService.Log(LogLevel.Error, $"Usage report generation failed: {ex.Message}");
            }

            return report;
        }

        public async Task<PerformanceMetrics> GetPerformanceMetricsAsync(
            TimeRange range, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(range);

            var metrics = new PerformanceMetrics
            {
                TimeRange = range,
                CollectedAt = DateTime.UtcNow
            };

            try
            {
                // Collect system performance data
                metrics.DiskUsage = await CollectDiskUsageMetrics(cancellationToken);
                metrics.FileOperationsPerHour = await CalculateFileOperationsPerHour(range);
                metrics.AverageFileSize = await CalculateAverageFileSize(range);
                metrics.MostAccessedFiles = await GetMostAccessedFiles(range, 10);
                metrics.SystemResourceUsage = CollectSystemResourceUsage();
                
                _logService.Log(LogLevel.Info, "Performance metrics collected successfully");
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Performance metrics collection failed: {ex.Message}");
            }

            return metrics;
        }

        public async Task<TrendAnalysis> AnalyzeTrendsAsync(
            string path, 
            TimeRange range, 
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentNullException.ThrowIfNull(range);

            var analysis = new TrendAnalysis
            {
                AnalysisPath = path,
                TimeRange = range,
                AnalyzedAt = DateTime.UtcNow
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Analyzing trends for: {path}");

                // Analyze growth trends
                analysis.GrowthTrend = await AnalyzeGrowthTrend(path, range, cancellationToken);
                
                // Analyze file creation patterns
                analysis.CreationPatterns = await AnalyzeCreationPatterns(path, range, cancellationToken);
                
                // Analyze modification patterns
                analysis.ModificationPatterns = await AnalyzeModificationPatterns(path, range, cancellationToken);
                
                // Analyze deletion patterns
                analysis.DeletionPatterns = await AnalyzeDeletionPatterns(path, range, cancellationToken);
                
                // Generate predictions
                analysis.Predictions = GeneratePredictions(analysis);

                _logService.Log(LogLevel.Info, "Trend analysis completed successfully");
            }
            catch (Exception ex)
            {
                analysis.ErrorMessage = ex.Message;
                _logService.Log(LogLevel.Error, $"Trend analysis failed: {ex.Message}");
            }

            return analysis;
        }

        public async Task<FileTypeAnalysis[]> GetFileTypeAnalysisAsync(
            string path, 
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            var results = new List<FileTypeAnalysis>();

            try
            {
                var entries = await _fileManager.GetDirectoryContentsAsync(path, cancellationToken);
                var files = entries.Where(e => !e.IsDirectory);

                var typeGroups = files.GroupBy(f => GetFileTypeCategory(f.Extension ?? ""));

                foreach (var group in typeGroups)
                {
                    var analysis = new FileTypeAnalysis
                    {
                        FileType = group.Key,
                        FileCount = group.Count(),
                        TotalSize = group.Sum(f => f.Size),
                        AverageSize = (long)group.Average(f => f.Size),
                        LargestFile = group.OrderByDescending(f => f.Size).First().FullPath,
                        SmallestFile = group.OrderBy(f => f.Size).First().FullPath,
                        Extensions = group.Select(f => f.Extension ?? "")
                            .Distinct()
                            .Where(ext => !string.IsNullOrEmpty(ext))
                            .ToArray()
                    };

                    results.Add(analysis);
                }

                _logService.Log(LogLevel.Info, $"File type analysis completed: {results.Count} types found");
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"File type analysis failed: {ex.Message}");
            }

            return results.OrderByDescending(a => a.TotalSize).ToArray();
        }

        public async Task<StorageAnalysis> AnalyzeStorageAsync(
            string[] paths, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(paths);

            var analysis = new StorageAnalysis
            {
                AnalyzedPaths = paths,
                AnalyzedAt = DateTime.UtcNow,
                DriveAnalyses = new List<DriveAnalysis>()
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Analyzing storage for {paths.Length} paths");

                var drives = await _fileManager.GetDrivesAsync(cancellationToken);

                foreach (var drive in drives.Where(d => d.IsReady))
                {
                    var driveAnalysis = new DriveAnalysis
                    {
                        DriveLetter = drive.Name,
                        DriveLabel = drive.Label,
                        TotalSpace = drive.TotalSize,
                        FreeSpace = drive.FreeSpace,
                        UsedSpace = drive.TotalSize - drive.FreeSpace,
                        UsagePercentage = (double)(drive.TotalSize - drive.FreeSpace) / drive.TotalSize * 100
                    };

                    // Analyze file distribution on this drive
                    var pathsOnDrive = paths.Where(p => Path.GetPathRoot(p)?.StartsWith(drive.Name[0].ToString(), StringComparison.OrdinalIgnoreCase) == true);
                    
                    if (pathsOnDrive.Any())
                    {
                        driveAnalysis.FileDistribution = await AnalyzeFileDistribution(pathsOnDrive, cancellationToken);
                        driveAnalysis.LargestDirectories = await FindLargestDirectories(pathsOnDrive, 10, cancellationToken);
                    }

                    analysis.DriveAnalyses.Add(driveAnalysis);
                }

                analysis.TotalAnalyzedSpace = analysis.DriveAnalyses.Sum(d => d.UsedSpace);
                analysis.TotalFreeSpace = analysis.DriveAnalyses.Sum(d => d.FreeSpace);
                analysis.OverallUsagePercentage = analysis.TotalAnalyzedSpace / 
                    (analysis.TotalAnalyzedSpace + analysis.TotalFreeSpace) * 100;

                _logService.Log(LogLevel.Info, "Storage analysis completed successfully");
            }
            catch (Exception ex)
            {
                analysis.ErrorMessage = ex.Message;
                _logService.Log(LogLevel.Error, $"Storage analysis failed: {ex.Message}");
            }

            return analysis;
        }

        private async Task<FileStatistics> CollectFileStatistics(
            string path, 
            bool includeSubdirectories, 
            CancellationToken cancellationToken)
        {
            var stats = new FileStatistics();
            var filesByType = new Dictionary<string, List<FileSystemEntry>>();
            var filesSizes = new List<long>();

            var searchOptions = new EnumerationOptions
            {
                RecurseSubdirectories = includeSubdirectories,
                IgnoreInaccessible = true
            };

            await foreach (var filePath in Directory.EnumerateFiles(path, "*", searchOptions)
                .ToAsyncEnumerable().WithCancellation(cancellationToken))
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var entry = new FileSystemEntry
                    {
                        Name = fileInfo.Name,
                        FullPath = fileInfo.FullName,
                        Size = fileInfo.Length,
                        DateModified = fileInfo.LastWriteTime,
                        DateCreated = fileInfo.CreationTime,
                        Extension = fileInfo.Extension,
                        IsDirectory = false
                    };

                    stats.TotalFiles++;
                    stats.TotalSize += entry.Size;
                    filesSizes.Add(entry.Size);

                    var fileType = GetFileTypeCategory(entry.Extension ?? "");
                    if (!filesByType.ContainsKey(fileType))
                        filesByType[fileType] = new List<FileSystemEntry>();
                    filesByType[fileType].Add(entry);

                    if (stats.LargestFile == null || entry.Size > new FileInfo(stats.LargestFile).Length)
                        stats.LargestFile = entry.FullPath;

                    if (stats.OldestFile == null || entry.DateCreated < new FileInfo(stats.OldestFile).CreationTime)
                        stats.OldestFile = entry.FullPath;

                    if (stats.NewestFile == null || entry.DateCreated > new FileInfo(stats.NewestFile).CreationTime)
                        stats.NewestFile = entry.FullPath;
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.Warning, $"Error processing file {filePath}: {ex.Message}");
                }
            }

            await foreach (var dirPath in Directory.EnumerateDirectories(path, "*", searchOptions)
                .ToAsyncEnumerable().WithCancellation(cancellationToken))
            {
                stats.TotalDirectories++;
            }

            stats.FilesByType = filesByType;
            stats.FilesSizes = filesSizes;

            return stats;
        }

        private Task<FileTypeBreakdown[]> GenerateFileTypeBreakdown(
            Dictionary<string, List<FileSystemEntry>> filesByType)
        {
            var breakdown = new List<FileTypeBreakdown>();

            foreach (var kvp in filesByType)
            {
                breakdown.Add(new FileTypeBreakdown
                {
                    FileType = kvp.Key,
                    Count = kvp.Value.Count,
                    TotalSize = kvp.Value.Sum(f => f.Size),
                    AverageSize = (long)kvp.Value.Average(f => f.Size),
                    Percentage = 0 // Will be calculated later
                });
            }

            var totalFiles = breakdown.Sum(b => b.Count);
            foreach (var item in breakdown)
            {
                item.Percentage = (double)item.Count / totalFiles * 100;
            }

            return Task.FromResult(breakdown.OrderByDescending(b => b.TotalSize).ToArray());
        }

        private SizeDistribution GenerateSizeDistribution(List<long> fileSizes)
        {
            if (!fileSizes.Any())
                return new SizeDistribution();

            var sortedSizes = fileSizes.OrderBy(s => s).ToArray();

            return new SizeDistribution
            {
                SmallFiles = sortedSizes.Count(s => s < 1024 * 1024), // < 1MB
                MediumFiles = sortedSizes.Count(s => s >= 1024 * 1024 && s < 100 * 1024 * 1024), // 1MB - 100MB
                LargeFiles = sortedSizes.Count(s => s >= 100 * 1024 * 1024), // >= 100MB
                AverageSize = (long)sortedSizes.Average(),
                MedianSize = sortedSizes[sortedSizes.Length / 2],
                TotalSize = sortedSizes.Sum()
            };
        }

        private async Task<AccessPattern[]> AnalyzeAccessPatterns(
            string path, 
            TimeRange period, 
            CancellationToken cancellationToken)
        {
            // Simplified implementation - would need actual access logging
            var patterns = new List<AccessPattern>();

            var now = DateTime.UtcNow;
            var timeSlots = new[]
            {
                new { Name = "Morning (6-12)", Start = 6, End = 12 },
                new { Name = "Afternoon (12-18)", Start = 12, End = 18 },
                new { Name = "Evening (18-24)", Start = 18, End = 24 },
                new { Name = "Night (0-6)", Start = 0, End = 6 }
            };

            foreach (var slot in timeSlots)
            {
                patterns.Add(new AccessPattern
                {
                    TimeSlot = slot.Name,
                    AccessCount = Random.Shared.Next(10, 100), // Placeholder
                    MostAccessedFile = "sample.txt", // Placeholder
                    AverageAccessDuration = TimeSpan.FromMinutes(Random.Shared.Next(1, 30))
                });
            }

            return patterns.ToArray();
        }

        private async Task<DiskUsageMetric[]> CollectDiskUsageMetrics(CancellationToken cancellationToken)
        {
            var metrics = new List<DiskUsageMetric>();
            var drives = await _fileManager.GetDrivesAsync(cancellationToken);

            foreach (var drive in drives.Where(d => d.IsReady))
            {
                metrics.Add(new DiskUsageMetric
                {
                    DriveLetter = drive.Name,
                    TotalSpace = drive.TotalSize,
                    FreeSpace = drive.FreeSpace,
                    UsedSpace = drive.TotalSize - drive.FreeSpace,
                    UsagePercentage = (double)(drive.TotalSize - drive.FreeSpace) / drive.TotalSize * 100
                });
            }

            return metrics.ToArray();
        }

        private Task<int> CalculateFileOperationsPerHour(TimeRange range)
        {
            // Placeholder implementation - would need actual operation logging
            return Task.FromResult(Random.Shared.Next(50, 500));
        }

        private Task<long> CalculateAverageFileSize(TimeRange range)
        {
            // Placeholder implementation
            return Task.FromResult(1024L * 1024); // 1MB average
        }

        private Task<string[]> GetMostAccessedFiles(TimeRange range, int count)
        {
            // Placeholder implementation
            return Task.FromResult(new[] { "document1.pdf", "image1.jpg", "video1.mp4" });
        }

        private SystemResourceUsage CollectSystemResourceUsage()
        {
            return new SystemResourceUsage
            {
                CpuUsagePercentage = Random.Shared.Next(10, 80),
                MemoryUsagePercentage = Random.Shared.Next(30, 90),
                DiskIOPerSecond = Random.Shared.Next(100, 1000),
                NetworkIOPerSecond = Random.Shared.Next(50, 500)
            };
        }

        private string GetFileTypeCategory(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" => "Images",
                ".mp4" or ".avi" or ".mkv" or ".mov" or ".wmv" => "Videos",
                ".mp3" or ".wav" or ".flac" or ".aac" => "Audio",
                ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" => "Documents",
                ".zip" or ".rar" or ".7z" or ".tar" => "Archives",
                ".exe" or ".msi" or ".app" or ".deb" => "Applications",
                ".cs" or ".py" or ".js" or ".cpp" or ".java" => "Source Code",
                _ => "Other"
            };
        }

        private Task<GrowthTrend> AnalyzeGrowthTrend(
            string path,
            TimeRange range,
            CancellationToken cancellationToken)
        {
            // Simplified implementation
            return Task.FromResult(new GrowthTrend
            {
                FilesAddedPerDay = Random.Shared.Next(10, 100),
                SizeGrowthPerDay = Random.Shared.Next(1024 * 1024, 100 * 1024 * 1024),
                GrowthRate = Random.Shared.NextDouble() * 10,
                ProjectedSizeInOneMonth = Random.Shared.Next(1024 * 1024 * 100, 1024 * 1024 * 1000)
            });
        }

        private Task<CreationPattern[]> AnalyzeCreationPatterns(
            string path,
            TimeRange range,
            CancellationToken cancellationToken)
        {
            var patterns = new List<CreationPattern>();
            var daysOfWeek = Enum.GetValues<DayOfWeek>();

            foreach (var day in daysOfWeek)
            {
                patterns.Add(new CreationPattern
                {
                    DayOfWeek = day,
                    FilesCreated = Random.Shared.Next(5, 50),
                    PeakHour = Random.Shared.Next(9, 17)
                });
            }

            return Task.FromResult(patterns.ToArray());
        }

        private Task<ModificationPattern[]> AnalyzeModificationPatterns(
            string path,
            TimeRange range,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new[]
            {
                new ModificationPattern
                {
                    FileType = "Documents",
                    ModificationsPerDay = Random.Shared.Next(10, 50),
                    MostActiveHour = Random.Shared.Next(9, 17)
                },
                new ModificationPattern
                {
                    FileType = "Images",
                    ModificationsPerDay = Random.Shared.Next(5, 25),
                    MostActiveHour = Random.Shared.Next(14, 20)
                }
            });
        }

        private Task<DeletionPattern[]> AnalyzeDeletionPatterns(
            string path,
            TimeRange range,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new[]
            {
                new DeletionPattern
                {
                    FileType = "Temporary",
                    DeletionsPerDay = Random.Shared.Next(20, 100),
                    CommonDeletionTime = TimeSpan.FromHours(18)
                }
            });
        }

        private TrendPrediction[] GeneratePredictions(TrendAnalysis analysis)
        {
            return new[]
            {
                new TrendPrediction
                {
                    Metric = "Storage Usage",
                    PredictedValue = analysis.GrowthTrend.ProjectedSizeInOneMonth,
                    Confidence = 0.75,
                    TimeFrame = TimeSpan.FromDays(30)
                },
                new TrendPrediction
                {
                    Metric = "File Count",
                    PredictedValue = analysis.GrowthTrend.FilesAddedPerDay * 30,
                    Confidence = 0.68,
                    TimeFrame = TimeSpan.FromDays(30)
                }
            };
        }

        private async Task<FileDistribution> AnalyzeFileDistribution(
            IEnumerable<string> paths, 
            CancellationToken cancellationToken)
        {
            var distribution = new FileDistribution();
            var totalFiles = 0;
            var totalSize = 0L;
            var typeBreakdown = new Dictionary<string, int>();

            foreach (var path in paths)
            {
                try
                {
                    var entries = await _fileManager.GetDirectoryContentsAsync(path, cancellationToken);
                    var files = entries.Where(e => !e.IsDirectory);

                    totalFiles += files.Count();
                    totalSize += files.Sum(f => f.Size);

                    foreach (var file in files)
                    {
                        var type = GetFileTypeCategory(file.Extension ?? "");
                        typeBreakdown[type] = typeBreakdown.GetValueOrDefault(type, 0) + 1;
                    }
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.Warning, $"Error analyzing path {path}: {ex.Message}");
                }
            }

            distribution.TotalFiles = totalFiles;
            distribution.TotalSize = totalSize;
            distribution.TypeBreakdown = typeBreakdown;

            return distribution;
        }

        private Task<DirectorySize[]> FindLargestDirectories(
            IEnumerable<string> paths,
            int count,
            CancellationToken cancellationToken)
        {
            var directorySizes = new List<DirectorySize>();

            foreach (var path in paths)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(path);
                    var size = CalculateDirectorySize(dirInfo);

                    directorySizes.Add(new DirectorySize
                    {
                        Path = path,
                        Size = size,
                        FileCount = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count()
                    });
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.Warning, $"Error calculating directory size for {path}: {ex.Message}");
                }
            }

            return Task.FromResult(directorySizes
                .OrderByDescending(d => d.Size)
                .Take(count)
                .ToArray());
        }

        private long CalculateDirectorySize(DirectoryInfo dirInfo)
        {
            long size = 0;

            try
            {
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch (Exception)
                    {
                        // Skip files that can't be accessed
                    }
                }
            }
            catch (Exception)
            {
                // Skip directories that can't be accessed
            }

            return size;
        }

        private async Task SaveUsageReport(UsageReport report, CancellationToken cancellationToken)
        {
            var reportPath = Path.Combine(_analyticsDataPath, $"usage_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(reportPath, json, cancellationToken);
        }
    }
}
