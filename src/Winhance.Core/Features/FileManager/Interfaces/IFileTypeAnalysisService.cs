using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for analyzing file types and properties - created by SemFix constraint-based repair
    /// </summary>
    public interface IFileTypeAnalysisService
    {
        /// <summary>
        /// Analyze file type from path
        /// </summary>
        /// <param name="filePath">File path to analyze</param>
        /// <returns>File type analysis result</returns>
        Task<FileTypeAnalysisResult> AnalyzeFileTypeAsync(string filePath);

        /// <summary>
        /// Get detailed file properties
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Dictionary of file properties</returns>
        Task<Dictionary<string, object>> GetFilePropertiesAsync(string filePath);

        /// <summary>
        /// Check if file is executable
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>True if file is executable</returns>
        bool IsExecutable(string filePath);

        /// <summary>
        /// Check if file is image
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>True if file is image</returns>
        bool IsImage(string filePath);

        /// <summary>
        /// Check if file is video
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>True if file is video</returns>
        bool IsVideo(string filePath);

        /// <summary>
        /// Check if file is audio
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>True if file is audio</returns>
        bool IsAudio(string filePath);

        /// <summary>
        /// Check if file is document
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>True if file is document</returns>
        bool IsDocument(string filePath);

        /// <summary>
        /// Check if file is archive
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>True if file is archive</returns>
        bool IsArchive(string filePath);

        /// <summary>
        /// Get MIME type for file
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>MIME type string</returns>
        string GetMimeType(string filePath);

        /// <summary>
        /// Get file signature/magic bytes
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>File signature bytes</returns>
        Task<byte[]> GetFileSignatureAsync(string filePath);

        /// <summary>
        /// Get supported file extensions for category
        /// </summary>
        /// <param name="category">File category</param>
        /// <returns>Array of supported extensions</returns>
        string[] GetSupportedExtensions(FileAnalysisCategory category);
    }

    /// <summary>
    /// File type analysis result
    /// </summary>
    public class FileTypeAnalysisResult
    {
        /// <summary>
        /// File category
        /// </summary>
        public FileAnalysisCategory Category { get; set; }

        /// <summary>
        /// File type description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// MIME type
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// File extension
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// Whether file is binary
        /// </summary>
        public bool IsBinary { get; set; }

        /// <summary>
        /// Confidence level (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Additional properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// File analysis categories
    /// </summary>
    public enum FileAnalysisCategory
    {
        Unknown,
        Document,
        Image,
        Video,
        Audio,
        Archive,
        Executable,
        Code,
        Data,
        Configuration,
        Font,
        Disk
    }
}
