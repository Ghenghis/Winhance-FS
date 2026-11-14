using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Represents a favorite file or folder.
    /// </summary>
    public class FavoriteItem
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Display name for the favorite.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full path to the favorite location.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Optional group ID this favorite belongs to.
        /// </summary>
        public string? GroupId { get; set; }

        /// <summary>
        /// Icon identifier or path.
        /// </summary>
        public string Icon { get; set; } = "📁";

        /// <summary>
        /// When the favorite was added.
        /// </summary>
        public DateTime DateAdded { get; set; } = DateTime.Now;

        /// <summary>
        /// When the favorite was last accessed.
        /// </summary>
        public DateTime DateAccessed { get; set; } = DateTime.Now;

        /// <summary>
        /// Number of times this favorite has been accessed.
        /// </summary>
        public int AccessCount { get; set; }

        /// <summary>
        /// Sort order within the group.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Whether the favorite is pinned to the top.
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Optional description or notes.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Custom tags for categorization.
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }

    /// <summary>
    /// Represents a group of favorites.
    /// </summary>
    public class FavoriteGroup
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Display name for the group.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Color for the group (hex format).
        /// </summary>
        public string Color { get; set; } = "#FF5722";

        /// <summary>
        /// Icon identifier for the group.
        /// </summary>
        public string Icon { get; set; } = "📂";

        /// <summary>
        /// Whether the group is expanded in the UI.
        /// </summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>
        /// Sort order for the group.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// When the group was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Optional description.
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Configuration for favorites storage and behavior.
    /// </summary>
    public class FavoritesConfig
    {
        /// <summary>
        /// Path to the favorites storage file.
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of recent favorites to track.
        /// </summary>
        public int MaxRecentItems { get; set; } = 20;

        /// <summary>
        /// Whether to show access count.
        /// </summary>
        public bool ShowAccessCount { get; set; } = true;

        /// <summary>
        /// Whether to show last accessed date.
        /// </summary>
        public bool ShowLastAccessed { get; set; } = true;

        /// <summary>
        /// Default group for new favorites.
        /// </summary>
        public string? DefaultGroupId { get; set; }

        /// <summary>
        /// Whether to auto-remove inaccessible favorites.
        /// </summary>
        public bool AutoRemoveInaccessible { get; set; }
    }
}
