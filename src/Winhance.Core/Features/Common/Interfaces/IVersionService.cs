using System.Collections.Generic;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Models;

namespace Winhance.Core.Features.Common.Interfaces
{
    public interface IVersionService
    {
        /// <summary>
        /// Gets the current application version.
        /// </summary>
        /// <returns></returns>
        VersionInfo GetCurrentVersion();

        /// <summary>
        /// Checks if an update is available.
        /// </summary>
        /// <returns>A task that resolves to true if an update is available, false otherwise.</returns>
        Task<VersionInfo> CheckForUpdateAsync();

        /// <summary>
        /// Downloads and launches the installer for the latest version.
        /// </summary>
        /// <returns>A task that completes when the download is initiated.</returns>
        Task DownloadAndInstallUpdateAsync();

        /// <summary>
        /// Gets the full version history from GitHub releases.
        /// </summary>
        /// <returns>A task that resolves to the version history.</returns>
        Task<VersionHistory> GetVersionHistoryAsync(int count = 10);

        /// <summary>
        /// Downloads and installs a specific version (for rollback).
        /// </summary>
        /// <param name="version">The version to install (e.g., "v1.0.0").</param>
        /// <returns>A task that completes when the download is initiated.</returns>
        Task DownloadAndInstallVersionAsync(string version);

        /// <summary>
        /// Gets the previous stable version for rollback.
        /// </summary>
        /// <returns>A task that resolves to the previous version info, or null if none exists.</returns>
        Task<VersionHistoryEntry?> GetPreviousVersionAsync();

        /// <summary>
        /// Checks if rollback to a previous version is possible.
        /// </summary>
        /// <returns>True if a previous version is available for rollback.</returns>
        bool CanRollback();
    }
}
