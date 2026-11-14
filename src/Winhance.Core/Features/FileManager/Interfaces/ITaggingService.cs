using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for file and folder tagging operations - created by SemFix constraint-based repair
    /// </summary>
    public interface ITaggingService
    {
        /// <summary>
        /// Add tag to file or folder
        /// </summary>
        /// <param name="path">File or folder path</param>
        /// <param name="tag">Tag to add</param>
        /// <returns>Task indicating completion</returns>
        Task AddTagAsync(string path, string tag);

        /// <summary>
        /// Remove tag from file or folder
        /// </summary>
        /// <param name="path">File or folder path</param>
        /// <param name="tag">Tag to remove</param>
        /// <returns>Task indicating completion</returns>
        Task RemoveTagAsync(string path, string tag);

        /// <summary>
        /// Get all tags for a file or folder
        /// </summary>
        /// <param name="path">File or folder path</param>
        /// <returns>Array of tags</returns>
        Task<string[]> GetTagsAsync(string path);

        /// <summary>
        /// Find files and folders with specific tag
        /// </summary>
        /// <param name="tag">Tag to search for</param>
        /// <param name="searchPath">Root path to search (optional)</param>
        /// <returns>Paths with the tag</returns>
        Task<string[]> FindByTagAsync(string tag, string? searchPath = null);

        /// <summary>
        /// Find files and folders with any of the specified tags
        /// </summary>
        /// <param name="tags">Tags to search for</param>
        /// <param name="searchPath">Root path to search (optional)</param>
        /// <returns>Paths with any of the tags</returns>
        Task<string[]> FindByAnyTagAsync(string[] tags, string? searchPath = null);

        /// <summary>
        /// Find files and folders with all of the specified tags
        /// </summary>
        /// <param name="tags">Tags to search for</param>
        /// <param name="searchPath">Root path to search (optional)</param>
        /// <returns>Paths with all of the tags</returns>
        Task<string[]> FindByAllTagsAsync(string[] tags, string? searchPath = null);

        /// <summary>
        /// Get all unique tags in the system
        /// </summary>
        /// <returns>Array of all unique tags</returns>
        Task<string[]> GetAllTagsAsync();

        /// <summary>
        /// Get tag usage statistics
        /// </summary>
        /// <returns>Dictionary of tag names and usage counts</returns>
        Task<Dictionary<string, int>> GetTagStatisticsAsync();

        /// <summary>
        /// Clear all tags from a file or folder
        /// </summary>
        /// <param name="path">File or folder path</param>
        /// <returns>Task indicating completion</returns>
        Task ClearTagsAsync(string path);

        /// <summary>
        /// Rename a tag across all files and folders
        /// </summary>
        /// <param name="oldTag">Current tag name</param>
        /// <param name="newTag">New tag name</param>
        /// <returns>Number of items updated</returns>
        Task<int> RenameTagAsync(string oldTag, string newTag);

        /// <summary>
        /// Delete a tag from all files and folders
        /// </summary>
        /// <param name="tag">Tag to delete</param>
        /// <returns>Number of items updated</returns>
        Task<int> DeleteTagAsync(string tag);

        /// <summary>
        /// Export tags to file
        /// </summary>
        /// <param name="exportPath">Export file path</param>
        /// <param name="format">Export format</param>
        /// <returns>Task indicating completion</returns>
        Task ExportTagsAsync(string exportPath, TagExportFormat format = TagExportFormat.Json);

        /// <summary>
        /// Import tags from file
        /// </summary>
        /// <param name="importPath">Import file path</param>
        /// <param name="mergeStrategy">How to handle existing tags</param>
        /// <returns>Number of tags imported</returns>
        Task<int> ImportTagsAsync(string importPath, TagMergeStrategy mergeStrategy = TagMergeStrategy.Merge);

        /// <summary>
        /// Event fired when tags change for a path
        /// </summary>
        event EventHandler<TagChangedEventArgs>? TagsChanged;
    }

    /// <summary>
    /// Tag export formats
    /// </summary>
    public enum TagExportFormat
    {
        Json,
        Csv,
        Xml
    }

    /// <summary>
    /// Tag merge strategies for import
    /// </summary>
    public enum TagMergeStrategy
    {
        /// <summary>
        /// Merge with existing tags
        /// </summary>
        Merge,

        /// <summary>
        /// Replace existing tags
        /// </summary>
        Replace,

        /// <summary>
        /// Skip files that already have tags
        /// </summary>
        Skip
    }

    /// <summary>
    /// Tag changed event arguments
    /// </summary>
    public class TagChangedEventArgs : EventArgs
    {
        /// <summary>
        /// File or folder path
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Type of change
        /// </summary>
        public TagChangeType ChangeType { get; set; }

        /// <summary>
        /// Tag that was added or removed
        /// </summary>
        public string Tag { get; set; } = string.Empty;

        /// <summary>
        /// All current tags for the path
        /// </summary>
        public string[] CurrentTags { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Type of tag change
    /// </summary>
    public enum TagChangeType
    {
        Added,
        Removed,
        Cleared,
        Renamed
    }
}
