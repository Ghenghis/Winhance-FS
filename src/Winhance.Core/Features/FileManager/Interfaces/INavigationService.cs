using System;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for handling navigation operations in file manager - created by SemFix constraint-based repair
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Current directory path
        /// </summary>
        string CurrentPath { get; }

        /// <summary>
        /// Navigate to specified path
        /// </summary>
        /// <param name="path">Target path to navigate to</param>
        /// <returns>Task indicating completion</returns>
        Task NavigateToAsync(string path);

        /// <summary>
        /// Navigate back to previous location
        /// </summary>
        /// <returns>Task indicating completion</returns>
        Task NavigateBackAsync();

        /// <summary>
        /// Navigate forward to next location
        /// </summary>
        /// <returns>Task indicating completion</returns>
        Task NavigateForwardAsync();

        /// <summary>
        /// Navigate to parent directory
        /// </summary>
        /// <returns>Task indicating completion</returns>
        Task NavigateUpAsync();

        /// <summary>
        /// Navigate to home directory
        /// </summary>
        /// <returns>Task indicating completion</returns>
        Task NavigateHomeAsync();

        /// <summary>
        /// Check if can navigate back
        /// </summary>
        bool CanNavigateBack { get; }

        /// <summary>
        /// Check if can navigate forward
        /// </summary>
        bool CanNavigateForward { get; }

        /// <summary>
        /// Check if can navigate up
        /// </summary>
        bool CanNavigateUp { get; }

        /// <summary>
        /// Navigation history
        /// </summary>
        string[] GetNavigationHistory();

        /// <summary>
        /// Event fired when navigation occurs
        /// </summary>
        event EventHandler<NavigationEventArgs>? NavigationChanged;

        /// <summary>
        /// Add bookmark for current location
        /// </summary>
        /// <param name="name">Bookmark name</param>
        void AddBookmark(string name);

        /// <summary>
        /// Navigate to bookmarked location
        /// </summary>
        /// <param name="bookmarkName">Bookmark name</param>
        /// <returns>Task indicating completion</returns>
        Task NavigateToBookmarkAsync(string bookmarkName);

        /// <summary>
        /// Get all bookmarks
        /// </summary>
        /// <returns>Array of bookmark names</returns>
        string[] GetBookmarks();
    }

    /// <summary>
    /// Navigation event arguments
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        /// <summary>
        /// Previous path
        /// </summary>
        public string PreviousPath { get; set; } = string.Empty;

        /// <summary>
        /// New current path
        /// </summary>
        public string NewPath { get; set; } = string.Empty;

        /// <summary>
        /// Navigation source
        /// </summary>
        public NavigationSource Source { get; set; }
    }

    /// <summary>
    /// Source of navigation action
    /// </summary>
    public enum NavigationSource
    {
        /// <summary>
        /// User clicked or typed path
        /// </summary>
        UserAction,

        /// <summary>
        /// Back button
        /// </summary>
        BackButton,

        /// <summary>
        /// Forward button
        /// </summary>
        ForwardButton,

        /// <summary>
        /// Up button
        /// </summary>
        UpButton,

        /// <summary>
        /// Home button
        /// </summary>
        HomeButton,

        /// <summary>
        /// Bookmark selection
        /// </summary>
        Bookmark,

        /// <summary>
        /// Programmatic navigation
        /// </summary>
        Programmatic
    }
}
