using System;
using System.IO;
using System.Linq;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.WPF.Features.FileManager.ViewModels
{
    /// <summary>
    /// Helpers on top of <see cref="IArchiveService"/>.
    /// </summary>
    public static class ArchiveServiceExtensions
    {
        /// <summary>
        /// Returns true when the archive service reports it can extract the given file.
        /// </summary>
        public static bool CanExtractArchive(this IArchiveService archiveService, string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return !string.IsNullOrEmpty(extension)
                && archiveService.SupportedFormats.Any(f =>
                    f.CanExtract && f.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        }
    }
}
