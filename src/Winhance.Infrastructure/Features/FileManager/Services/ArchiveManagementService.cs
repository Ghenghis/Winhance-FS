using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;

// Disambiguate types that exist in both Interfaces and Models namespaces.
// IArchiveManagementService contract uses the Models namespace types.
using ArchiveProgress = Winhance.Core.Features.FileManager.Models.ArchiveProgress;
using ArchiveResult = Winhance.Core.Features.FileManager.Models.ArchiveResult;
using ArchiveInfo = Winhance.Core.Features.FileManager.Models.ArchiveInfo;
using ArchiveFormat = Winhance.Core.Features.FileManager.Models.ArchiveFormat;
using ExtractionOptions = Winhance.Core.Features.FileManager.Models.ExtractionOptions;
using ExtractionResult = Winhance.Core.Features.FileManager.Models.ExtractionResult;

namespace Winhance.Infrastructure.Features.FileManager.Services
{
    /// <summary>
    /// Archive management service implementing features AM-001 to AM-036
    /// Provides comprehensive archive creation, extraction, and management capabilities
    /// </summary>
    public class ArchiveManagementService : IArchiveManagementService
    {
        private readonly ILogService _logService;
        private readonly Dictionary<string, ArchiveType> _supportedFormats;
        private readonly Dictionary<ArchiveType, IArchiveHandler> _handlers;

        public ArchiveManagementService(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            _supportedFormats = new Dictionary<string, ArchiveType>(StringComparer.OrdinalIgnoreCase)
            {
                { ".zip", ArchiveType.Zip },
                { ".7z", ArchiveType.SevenZip },
                { ".rar", ArchiveType.Rar },
                { ".tar", ArchiveType.Tar },
                { ".gz", ArchiveType.GZip },
                { ".bz2", ArchiveType.BZip2 }
            };

            _handlers = new Dictionary<ArchiveType, IArchiveHandler>
            {
                { ArchiveType.Zip, new ZipArchiveHandler(_logService) },
                { ArchiveType.SevenZip, new SevenZipArchiveHandler(_logService) },
                { ArchiveType.Tar, new TarArchiveHandler(_logService) }
            };
        }

        public async Task<ArchiveResult> CreateArchiveAsync(
            ArchiveCreationOptions options,
            IProgress<ArchiveProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrEmpty(options.OutputPath);
            ArgumentNullException.ThrowIfNull(options.SourcePaths);

            var result = new ArchiveResult
            {
                ArchivePath = options.OutputPath,
                Success = false,
                StartTime = DateTime.UtcNow,
                ArchiveType = options.ArchiveType
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Creating archive: {options.OutputPath}");

                // Validate source paths
                var validSources = new List<string>();
                foreach (var sourcePath in options.SourcePaths)
                {
                    if (File.Exists(sourcePath) || Directory.Exists(sourcePath))
                    {
                        validSources.Add(sourcePath);
                    }
                    else
                    {
                        _logService.Log(LogLevel.Warning, $"Source path not found: {sourcePath}");
                    }
                }

                if (validSources.Count == 0)
                {
                    throw new InvalidOperationException("No valid source paths found");
                }

                // Get appropriate handler
                var handler = GetHandler(options.ArchiveType);
                
                // Create archive
                var handlerResult = await handler.CreateArchiveAsync(
                    validSources, 
                    options.OutputPath, 
                    options, 
                    progress, 
                    cancellationToken);

                result.Success = handlerResult.Success;
                result.FilesProcessed = handlerResult.FilesProcessed;
                result.TotalSize = handlerResult.TotalSize;
                result.CompressedSize = handlerResult.CompressedSize;
                result.CompressionRatio = handlerResult.CompressionRatio;
                result.ErrorMessages = handlerResult.Errors.ToArray();
                result.EndTime = DateTime.UtcNow;

                if (result.Success)
                {
                    _logService.Log(LogLevel.Info, 
                        $"Archive created successfully: {result.FilesProcessed} files, " +
                        $"compression ratio: {result.CompressionRatio:P2}, " +
                        $"compression level: {System.IO.Compression.CompressionLevel.Optimal}");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors = new List<string> { ex.Message };
                result.EndTime = DateTime.UtcNow;
                
                _logService.Log(LogLevel.Error, $"Archive creation failed: {ex.Message}");
                throw;
            }

            return result;
        }

        public async Task<ExtractionResult> ExtractArchiveAsync(
            string archivePath,
            string extractionPath,
            ExtractionOptions? options = null,
            IProgress<ArchiveProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(archivePath);
            ArgumentException.ThrowIfNullOrEmpty(extractionPath);
            options ??= new ExtractionOptions();

            var result = new ExtractionResult
            {
                ArchivePath = archivePath,
                ExtractionPath = extractionPath,
                Success = false,
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Extracting archive: {archivePath} to {extractionPath}");

                if (!File.Exists(archivePath))
                {
                    throw new FileNotFoundException($"Archive not found: {archivePath}");
                }

                // Detect archive type
                var archiveType = DetectArchiveType(archivePath);
                if (archiveType == ArchiveType.Unknown)
                {
                    throw new NotSupportedException($"Unsupported archive format: {Path.GetExtension(archivePath)}");
                }

                // Create extraction directory
                Directory.CreateDirectory(extractionPath);

                // Get appropriate handler
                var handler = GetHandler(archiveType);

                // Extract archive
                var handlerResult = await handler.ExtractArchiveAsync(
                    archivePath,
                    extractionPath,
                    options,
                    progress,
                    cancellationToken);

                result.Success = handlerResult.Success;
                result.FilesExtracted = handlerResult.FilesExtracted;
                result.TotalSize = handlerResult.TotalSize;
                result.ErrorMessages = handlerResult.Errors.ToArray();
                result.ExtractedFiles = handlerResult.ExtractedFiles;
                result.EndTime = DateTime.UtcNow;

                if (result.Success)
                {
                    _logService.Log(LogLevel.Info, 
                        $"Archive extracted successfully: {result.FilesExtracted} files extracted");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessages = new[] { ex.Message };
                result.EndTime = DateTime.UtcNow;
                
                _logService.Log(LogLevel.Error, $"Archive extraction failed: {ex.Message}");
                throw;
            }

            return result;
        }

        public async Task<ArchiveInfo> GetArchiveInfoAsync(
            string archivePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(archivePath);

            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Archive not found: {archivePath}");
            }

            var archiveType = DetectArchiveType(archivePath);
            if (archiveType == ArchiveType.Unknown)
            {
                throw new NotSupportedException($"Unsupported archive format: {Path.GetExtension(archivePath)}");
            }

            var handler = GetHandler(archiveType);
            return await handler.GetArchiveInfoAsync(archivePath, cancellationToken);
        }

        public async Task<TestResult> TestArchiveIntegrityAsync(
            string archivePath,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(archivePath);

            var result = new TestResult
            {
                ArchivePath = archivePath,
                Success = false,
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Testing archive integrity: {archivePath}");

                if (!File.Exists(archivePath))
                {
                    throw new FileNotFoundException($"Archive not found: {archivePath}");
                }

                var archiveType = DetectArchiveType(archivePath);
                if (archiveType == ArchiveType.Unknown)
                {
                    throw new NotSupportedException($"Unsupported archive format");
                }

                var handler = GetHandler(archiveType);
                var handlerResult = await handler.TestIntegrityAsync(archivePath, cancellationToken);

                result.Success = handlerResult.Success;
                result.ErrorMessages = handlerResult.ErrorMessages ?? Array.Empty<string>();
                result.TestedFiles = handlerResult.TestedFiles ?? new List<string>();
                result.CorruptedFiles = handlerResult.CorruptedFiles ?? new List<string>();
                result.EndTime = DateTime.UtcNow;

                _logService.Log(LogLevel.Info, 
                    $"Archive integrity test completed: {(result.Success ? "PASSED" : "FAILED")}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessages = new[] { ex.Message };
                result.EndTime = DateTime.UtcNow;
                
                _logService.Log(LogLevel.Error, $"Archive integrity test failed: {ex.Message}");
            }

            return result;
        }

        public ArchiveType DetectArchiveType(string filePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(filePath);

            var extension = Path.GetExtension(filePath);
            return _supportedFormats.TryGetValue(extension, out var archiveType) 
                ? archiveType 
                : ArchiveType.Unknown;
        }

        public bool IsArchiveSupported(string filePath)
        {
            return DetectArchiveType(filePath) != ArchiveType.Unknown;
        }

        public IEnumerable<ArchiveFormat> GetSupportedFormats()
        {
            return _supportedFormats.Select(kvp => new ArchiveFormat
            {
                Extension = kvp.Key,
                ArchiveType = kvp.Value,
                Description = GetFormatDescription(kvp.Value),
                SupportsCompression = SupportsCompression(kvp.Value),
                SupportsEncryption = SupportsEncryption(kvp.Value)
            });
        }

        private IArchiveHandler GetHandler(ArchiveType archiveType)
        {
            if (_handlers.TryGetValue(archiveType, out var handler))
            {
                return handler;
            }

            throw new NotSupportedException($"Archive type not supported: {archiveType}");
        }

        private string GetFormatDescription(ArchiveType archiveType)
        {
            return archiveType switch
            {
                ArchiveType.Zip => "ZIP Archive",
                ArchiveType.SevenZip => "7-Zip Archive",
                ArchiveType.Rar => "RAR Archive", 
                ArchiveType.Tar => "Tape Archive",
                ArchiveType.GZip => "GZip Compressed File",
                ArchiveType.BZip2 => "BZip2 Compressed File",
                _ => "Unknown Archive Format"
            };
        }

        private bool SupportsCompression(ArchiveType archiveType)
        {
            return archiveType switch
            {
                ArchiveType.Zip => true,
                ArchiveType.SevenZip => true,
                ArchiveType.Rar => true,
                ArchiveType.GZip => true,
                ArchiveType.BZip2 => true,
                ArchiveType.Tar => false,
                _ => false
            };
        }

        private bool SupportsEncryption(ArchiveType archiveType)
        {
            return archiveType switch
            {
                ArchiveType.Zip => true,
                ArchiveType.SevenZip => true,
                ArchiveType.Rar => true,
                _ => false
            };
        }


    }

    // Archive handler implementations
    internal class ZipArchiveHandler : IArchiveHandler
    {
        private readonly ILogService _logService;

        public ZipArchiveHandler(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public async Task<ArchiveHandlerResult> CreateArchiveAsync(
            IEnumerable<string> sourcePaths,
            string outputPath,
            ArchiveCreationOptions options,
            IProgress<ArchiveProgress>? progress,
            CancellationToken cancellationToken)
        {
            var result = new ArchiveHandlerResult();
            var errors = new List<string>();

            try
            {
                using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
                var filesProcessed = 0;
                var totalSize = 0L;

                foreach (var sourcePath in sourcePaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (File.Exists(sourcePath))
                    {
                        await AddFileToZip(archive, sourcePath, "", System.IO.Compression.CompressionLevel.Optimal);
                        filesProcessed++;
                        totalSize += new FileInfo(sourcePath).Length;
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        var (dirFiles, dirSize) = await AddDirectoryToZip(
                            archive, sourcePath, "", System.IO.Compression.CompressionLevel.Optimal, cancellationToken);
                        filesProcessed += dirFiles;
                        totalSize += dirSize;
                    }

                    progress?.Report(new ArchiveProgress
                    {
                        FilesProcessed = filesProcessed,
                        CurrentFile = Path.GetFileName(sourcePath)
                    });
                }

                var compressedSize = new FileInfo(outputPath).Length;
                
                result.Success = true;
                result.FilesProcessed = filesProcessed;
                result.TotalSize = totalSize;
                result.CompressedSize = compressedSize;
                result.CompressionRatio = totalSize > 0 ? (double)compressedSize / totalSize : 0;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                _logService.Log(LogLevel.Error, $"ZIP creation failed: {ex.Message}");
            }

            result.Errors = errors;
            return result;
        }

        public async Task<ExtractionHandlerResult> ExtractArchiveAsync(
            string archivePath,
            string extractionPath,
            ExtractionOptions options,
            IProgress<ArchiveProgress>? progress,
            CancellationToken cancellationToken)
        {
            var result = new ExtractionHandlerResult();
            var errors = new List<string>();
            var extractedFiles = new List<string>();

            try
            {
                using var archive = ZipFile.OpenRead(archivePath);
                var totalFiles = archive.Entries.Count;
                var filesExtracted = 0;
                var totalSize = 0L;

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var destPath = Path.Combine(extractionPath, entry.FullName);
                        
                        // Security check for path traversal
                        if (!destPath.StartsWith(extractionPath, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add($"Potential path traversal detected: {entry.FullName}");
                            continue;
                        }

                        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                        
                        if (!entry.FullName.EndsWith("/", StringComparison.Ordinal)) // Not a directory
                        {
                            entry.ExtractToFile(destPath, options.OverwriteExisting);
                            extractedFiles.Add(destPath);
                            totalSize += entry.Length;
                        }

                        filesExtracted++;
                        progress?.Report(new ArchiveProgress
                        {
                            FilesProcessed = filesExtracted,
                            TotalFiles = totalFiles,
                            CurrentFile = entry.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to extract {entry.FullName}: {ex.Message}");
                    }
                }

                result.Success = errors.Count == 0;
                result.FilesExtracted = filesExtracted;
                result.TotalSize = totalSize;
                result.ExtractedFiles = extractedFiles;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                _logService.Log(LogLevel.Error, $"ZIP extraction failed: {ex.Message}");
            }

            result.Errors = errors;
            return result;
        }

        public async Task<ArchiveInfo> GetArchiveInfoAsync(string archivePath, CancellationToken cancellationToken)
        {
            using var archive = ZipFile.OpenRead(archivePath);
            var entries = new List<ArchiveEntry>();
            var totalCompressedSize = 0L;
            var totalUncompressedSize = 0L;

            foreach (var entry in archive.Entries)
            {
                entries.Add(new ArchiveEntry
                {
                    Name = entry.Name,
                    FullName = entry.FullName,
                    Size = entry.Length,
                    CompressedSize = entry.CompressedLength,
                    LastModified = entry.LastWriteTime.DateTime,
                    IsDirectory = entry.FullName.EndsWith("/", StringComparison.Ordinal),
                    CompressionRatio = entry.Length > 0 ? (double)entry.CompressedLength / entry.Length : 0
                });

                totalCompressedSize += entry.CompressedLength;
                totalUncompressedSize += entry.Length;
            }

            return new ArchiveInfo
            {
                ArchivePath = archivePath,
                ArchiveType = ArchiveType.Zip,
                TotalEntries = entries.Count,
                TotalSize = totalUncompressedSize,
                CompressedSize = totalCompressedSize,
                CompressionRatio = totalUncompressedSize > 0 ? (double)totalCompressedSize / totalUncompressedSize : 0,
                Entries = entries.ToArray(),
                CreatedAt = File.GetCreationTime(archivePath),
                ModifiedAt = File.GetLastWriteTime(archivePath)
            };
        }

        public async Task<IntegrityTestResult> TestIntegrityAsync(string archivePath, CancellationToken cancellationToken)
        {
            var result = new IntegrityTestResult();
            var errors = new List<string>();
            var testedFilesList = new List<string>();
            var corruptedFiles = new List<string>();

            try
            {
                using var archive = ZipFile.OpenRead(archivePath);

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        // Test by opening and reading the entry
                        using var stream = entry.Open();
                        using var reader = new MemoryStream();
                        await stream.CopyToAsync(reader, cancellationToken);
                        
                        testedFilesList.Add(entry.FullName);
                    }
                    catch (Exception ex)
                    {
                        corruptedFiles.Add(entry.FullName);
                        errors.Add($"Corrupted entry {entry.FullName}: {ex.Message}");
                    }
                }

                result.Success = corruptedFiles.Count == 0;
                result.TestedFiles = testedFilesList;
                result.CorruptedFiles = corruptedFiles;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                result.Success = false;
            }

            result.Errors = errors;
            return result;
        }

        private async Task AddFileToZip(ZipArchive archive, string filePath, string basePath, System.IO.Compression.CompressionLevel compressionLevel)
        {
            var entryName = string.IsNullOrEmpty(basePath) 
                ? Path.GetFileName(filePath) 
                : Path.Combine(basePath, Path.GetFileName(filePath)).Replace('\\', '/');

            var entry = archive.CreateEntry(entryName, compressionLevel);
            entry.LastWriteTime = File.GetLastWriteTime(filePath);

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var entryStream = entry.Open();
            await fileStream.CopyToAsync(entryStream);
        }

        private async Task<(int files, long size)> AddDirectoryToZip(
            ZipArchive archive, 
            string dirPath, 
            string basePath, 
            System.IO.Compression.CompressionLevel compressionLevel,
            CancellationToken cancellationToken)
        {
            var filesAdded = 0;
            var totalSize = 0L;

            var dirName = Path.GetFileName(dirPath);
            var currentBasePath = string.IsNullOrEmpty(basePath) ? dirName : Path.Combine(basePath, dirName);

            // Add files in current directory
            foreach (var file in Directory.GetFiles(dirPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                await AddFileToZip(archive, file, currentBasePath, compressionLevel);
                filesAdded++;
                totalSize += new FileInfo(file).Length;
            }

            // Recursively add subdirectories
            foreach (var subDir in Directory.GetDirectories(dirPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var (subFiles, subSize) = await AddDirectoryToZip(archive, subDir, currentBasePath, compressionLevel, cancellationToken);
                filesAdded += subFiles;
                totalSize += subSize;
            }

            return (filesAdded, totalSize);
        }
    }

    // Placeholder implementations for other archive types
    internal class SevenZipArchiveHandler : IArchiveHandler
    {
        private readonly ILogService _logService;

        public SevenZipArchiveHandler(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public Task<ArchiveHandlerResult> CreateArchiveAsync(IEnumerable<string> sourcePaths, string outputPath, ArchiveCreationOptions options, IProgress<ArchiveProgress>? progress, CancellationToken cancellationToken)
        {
            _logService.Log(LogLevel.Info, "7-Zip archive creation requires an external 7-Zip backend");
            return Task.FromResult(new ArchiveHandlerResult
            {
                Success = false,
                Errors = new List<string> { "7-Zip archive creation requires an external 7-Zip backend" }
            });
        }

        public Task<ExtractionHandlerResult> ExtractArchiveAsync(string archivePath, string extractionPath, ExtractionOptions options, IProgress<ArchiveProgress>? progress, CancellationToken cancellationToken)
        {
            _logService.Log(LogLevel.Info, "7-Zip extraction requires an external 7-Zip backend");
            return Task.FromResult(new ExtractionHandlerResult
            {
                Success = false,
                Errors = new List<string> { "7-Zip extraction requires an external 7-Zip backend" }
            });
        }

        public Task<ArchiveInfo> GetArchiveInfoAsync(string archivePath, CancellationToken cancellationToken)
        {
            _logService.Log(LogLevel.Info, "7-Zip archive info requires an external 7-Zip backend");
            return Task.FromResult(new ArchiveInfo
            {
                Path = archivePath,
                Format = "7-Zip",
                Size = File.Exists(archivePath) ? new FileInfo(archivePath).Length : 0,
                EntryCount = 0
            });
        }

        public Task<IntegrityTestResult> TestIntegrityAsync(string archivePath, CancellationToken cancellationToken)
        {
            _logService.Log(LogLevel.Info, "7-Zip integrity testing requires an external 7-Zip backend");
            return Task.FromResult(new IntegrityTestResult
            {
                Success = false,
                ErrorMessage = "7-Zip integrity testing requires an external 7-Zip backend"
            });
        }
    }

    internal class TarArchiveHandler : IArchiveHandler
    {
        private readonly ILogService _logService;

        public TarArchiveHandler(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        public Task<ArchiveHandlerResult> CreateArchiveAsync(IEnumerable<string> sourcePaths, string outputPath, ArchiveCreationOptions options, IProgress<ArchiveProgress>? progress, CancellationToken cancellationToken)
        {
            _logService.Log(LogLevel.Info, "TAR archive creation not yet supported");
            return Task.FromResult(new ArchiveHandlerResult { Success = false, ErrorMessage = "TAR archive creation not yet supported" });
        }

        public Task<ExtractionHandlerResult> ExtractArchiveAsync(string archivePath, string extractionPath, ExtractionOptions options, IProgress<ArchiveProgress>? progress, CancellationToken cancellationToken)
        {
            _logService.Log(LogLevel.Info, "TAR extraction not yet supported");
            return Task.FromResult(new ExtractionHandlerResult { Success = false, ErrorMessage = "TAR extraction not yet supported" });
        }

        public Task<ArchiveInfo> GetArchiveInfoAsync(string archivePath, CancellationToken cancellationToken)
        {
            _logService.Log(LogLevel.Info, "TAR info retrieval not yet supported");
            return Task.FromResult(new ArchiveInfo { Path = archivePath, Format = "TAR", Size = 0, EntryCount = 0 });
        }

        public Task<IntegrityTestResult> TestIntegrityAsync(string archivePath, CancellationToken cancellationToken)
        {
            _logService.Log(LogLevel.Info, "TAR integrity testing not yet supported");
            return Task.FromResult(new IntegrityTestResult { Success = false, ErrorMessage = "TAR integrity testing not yet supported" });
        }
    }
}

