using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// Recycle Bin support on top of <see cref="IFileManagerService"/>.
    /// </summary>
    public static class FileManagerServiceExtensions
    {
        /// <summary>
        /// Moves files and folders to the Recycle Bin. Files go through <c>DeleteFilesAsync(permanent: false)</c>;
        /// the Core recycle path only handles files, so folders are recycled here.
        /// </summary>
        public static async Task<FileOperationResult> RecycleAsync(
            this IFileManagerService fileManagerService,
            IReadOnlyCollection<string> paths)
        {
            var folders = paths.Where(Directory.Exists).ToList();
            var files = paths.Where(p => !Directory.Exists(p)).ToList();
            var errors = new List<string>();
            var processed = 0;

            if (files.Count > 0)
            {
                var result = await fileManagerService.DeleteFilesAsync(files, permanent: false);
                processed += result.ItemsProcessed;
                errors.AddRange(result.Errors);
            }

            foreach (var folder in folders)
            {
                try
                {
                    await Task.Run(() => Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                        folder,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin));
                    processed++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
                {
                    errors.Add($"{folder}: {ex.Message}");
                }
            }

            return new FileOperationResult
            {
                Success = errors.Count == 0,
                ItemsProcessed = processed,
                ItemsFailed = errors.Count,
                Errors = errors,
                Message = errors.Count == 0
                    ? $"Moved {processed} item(s) to the Recycle Bin"
                    : string.Join("; ", errors),
            };
        }
    }
}
