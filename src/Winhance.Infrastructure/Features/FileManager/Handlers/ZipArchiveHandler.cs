using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;

// Disambiguate types that exist in both Interfaces and Models namespaces.
// IArchiveHandler contract uses the Models namespace types.
using ArchiveProgress = Winhance.Core.Features.FileManager.Models.ArchiveProgress;
using ExtractionOptions = Winhance.Core.Features.FileManager.Models.ExtractionOptions;
using ArchiveInfo = Winhance.Core.Features.FileManager.Models.ArchiveInfo;
using ArchiveResult = Winhance.Core.Features.FileManager.Models.ArchiveResult;
using ExtractionResult = Winhance.Core.Features.FileManager.Models.ExtractionResult;

namespace Winhance.Infrastructure.Features.FileManager.Handlers
{
    /// <summary>
    /// Archive handler for ZIP files
    /// </summary>
    public class ZipArchiveHandler : IArchiveHandler
    {
        public async Task<ArchiveHandlerResult> CreateArchiveAsync(
            IEnumerable<string> sourcePaths,
            string outputPath,
            ArchiveCreationOptions options,
            IProgress<ArchiveProgress>? progress,
            CancellationToken cancellationToken)
        {
            try
            {
                var sourceList = sourcePaths.ToList();
                var totalItems = sourceList.Count;
                var processedItems = 0;

                using var fileStream = new FileStream(outputPath, FileMode.Create);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

                foreach (var sourcePath in sourceList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (File.Exists(sourcePath))
                    {
                        await AddFileToZipAsync(archive, sourcePath, Path.GetFileName(sourcePath), cancellationToken);
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        await AddDirectoryToZipAsync(archive, sourcePath, Path.GetFileName(sourcePath), cancellationToken);
                    }

                    processedItems++;
                    progress?.Report(new ArchiveProgress
                    {
                        TotalItems = totalItems,
                        ProcessedItems = processedItems,
                        CurrentItem = Path.GetFileName(sourcePath),
                        PercentComplete = (double)processedItems / totalItems * 100
                    });
                }

                return new ArchiveHandlerResult
                {
                    Success = true,
                    OutputPath = outputPath,
                    ProcessedItems = processedItems
                };
            }
            catch (Exception ex)
            {
                return new ArchiveHandlerResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<ExtractionHandlerResult> ExtractArchiveAsync(
            string archivePath,
            string extractionPath,
            ExtractionOptions options,
            IProgress<ArchiveProgress>? progress,
            CancellationToken cancellationToken)
        {
            try
            {
                Directory.CreateDirectory(extractionPath);

                using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);

                var totalEntries = archive.Entries.Count;
                var processedEntries = 0;

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var destinationPath = Path.Combine(extractionPath, entry.FullName);
                    
                    // Security check - prevent directory traversal
                    if (!destinationPath.StartsWith(extractionPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (entry.FullName.EndsWith('/'))
                    {
                        // Directory entry
                        Directory.CreateDirectory(destinationPath);
                    }
                    else
                    {
                        // File entry
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        
                        if (!options.Overwrite && File.Exists(destinationPath))
                        {
                            processedEntries++;
                            continue;
                        }

                        using var entryStream = entry.Open();
                        using var outputStream = new FileStream(destinationPath, FileMode.Create);
                        await entryStream.CopyToAsync(outputStream, cancellationToken);
                    }

                    processedEntries++;
                    progress?.Report(new ArchiveProgress
                    {
                        TotalItems = totalEntries,
                        ProcessedItems = processedEntries,
                        CurrentItem = entry.FullName,
                        PercentComplete = (double)processedEntries / totalEntries * 100
                    });
                }

                return new ExtractionHandlerResult
                {
                    Success = true,
                    ExtractedItems = processedEntries,
                    ExtractedSize = GetDirectorySize(extractionPath)
                };
            }
            catch (Exception ex)
            {
                return new ExtractionHandlerResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public Task<ArchiveInfo> GetArchiveInfoAsync(string archivePath, CancellationToken cancellationToken)
        {
            try
            {
                using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);

                var entries = new List<ArchiveEntry>();
                long totalUncompressedSize = 0;
                long totalCompressedSize = 0;

                foreach (var entry in archive.Entries)
                {
                    totalUncompressedSize += entry.Length;
                    totalCompressedSize += entry.CompressedLength;

                    entries.Add(new ArchiveEntry
                    {
                        FullName = entry.FullName,
                        Name = entry.Name,
                        UncompressedSize = entry.Length,
                        CompressedSize = entry.CompressedLength,
                        LastModified = entry.LastWriteTime.DateTime,
                        IsDirectory = entry.FullName.EndsWith('/')
                    });
                }

                var archiveInfo = new ArchiveInfo
                {
                    ArchivePath = archivePath,
                    Type = ArchiveType.Zip,
                    EntryCount = archive.Entries.Count,
                    UncompressedSize = totalUncompressedSize,
                    CompressedSize = totalCompressedSize,
                    CompressionRatio = totalUncompressedSize > 0 ? (double)totalCompressedSize / totalUncompressedSize : 0,
                    Entries = entries.ToArray()
                };
                return Task.FromResult(archiveInfo);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read archive info: {ex.Message}", ex);
            }
        }

        public async Task<IntegrityTestResult> TestIntegrityAsync(string archivePath, CancellationToken cancellationToken)
        {
            try
            {
                using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
                using var archive = new ZipArchive(fileStream, ZipArchiveMode.Read);

                var testedEntries = 0;
                var errors = new List<string>();

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        // Try to open and read the entry
                        using var entryStream = entry.Open();
                        var buffer = new byte[1024];
                        while (await entryStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken) > 0)
                        {
                            // Just read through the stream to verify integrity
                        }
                        testedEntries++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Entry '{entry.FullName}': {ex.Message}");
                    }
                }

                return new IntegrityTestResult
                {
                    Success = errors.Count == 0,
                    TestedEntries = testedEntries,
                    ErrorMessages = errors.ToArray()
                };
            }
            catch (Exception ex)
            {
                return new IntegrityTestResult
                {
                    Success = false,
                    ErrorMessages = new[] { ex.Message }
                };
            }
        }

        private async Task AddFileToZipAsync(ZipArchive archive, string filePath, string entryName, CancellationToken cancellationToken)
        {
            var entry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            await fileStream.CopyToAsync(entryStream, cancellationToken);
        }

        private async Task AddDirectoryToZipAsync(ZipArchive archive, string directoryPath, string entryName, CancellationToken cancellationToken)
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            var baseDirectory = Path.GetDirectoryName(directoryPath)!;

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var relativePath = Path.GetRelativePath(baseDirectory, file);
                await AddFileToZipAsync(archive, file, relativePath, cancellationToken);
            }
        }

        private static long GetDirectorySize(string directoryPath)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                long size = 0;
                foreach (var file in files)
                {
                    size += new FileInfo(file).Length;
                }
                return size;
            }
            catch
            {
                return 0;
            }
        }
    }
}