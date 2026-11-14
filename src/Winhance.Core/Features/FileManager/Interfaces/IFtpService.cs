using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for FTP operations - created by SemFix constraint-based repair
    /// </summary>
    public interface IFtpService
    {
        /// <summary>
        /// Connect to FTP server
        /// </summary>
        /// <param name="server">FTP server address</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="port">Port (default 21)</param>
        /// <returns>Task indicating connection success</returns>
        Task<bool> ConnectAsync(string server, string username, string password, int port = 21);

        /// <summary>
        /// Disconnect from FTP server
        /// </summary>
        /// <returns>Task indicating completion</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Upload file to FTP server
        /// </summary>
        /// <param name="localPath">Local file path</param>
        /// <param name="remotePath">Remote file path</param>
        /// <returns>Task indicating upload success</returns>
        Task<bool> UploadFileAsync(string localPath, string remotePath);

        /// <summary>
        /// Download file from FTP server
        /// </summary>
        /// <param name="remotePath">Remote file path</param>
        /// <param name="localPath">Local file path</param>
        /// <returns>Task indicating download success</returns>
        Task<bool> DownloadFileAsync(string remotePath, string localPath);

        /// <summary>
        /// List files and directories in remote path
        /// </summary>
        /// <param name="remotePath">Remote directory path</param>
        /// <returns>List of FTP items</returns>
        Task<IEnumerable<FtpItem>> ListDirectoryAsync(string remotePath);

        /// <summary>
        /// Delete file on FTP server
        /// </summary>
        /// <param name="remotePath">Remote file path</param>
        /// <returns>Task indicating deletion success</returns>
        Task<bool> DeleteFileAsync(string remotePath);

        /// <summary>
        /// Create directory on FTP server
        /// </summary>
        /// <param name="remotePath">Remote directory path</param>
        /// <returns>Task indicating creation success</returns>
        Task<bool> CreateDirectoryAsync(string remotePath);

        /// <summary>
        /// Delete directory on FTP server
        /// </summary>
        /// <param name="remotePath">Remote directory path</param>
        /// <returns>Task indicating deletion success</returns>
        Task<bool> DeleteDirectoryAsync(string remotePath);

        /// <summary>
        /// Check if connected to FTP server
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Current remote directory
        /// </summary>
        string CurrentDirectory { get; }

        /// <summary>
        /// Event fired when transfer progress updates
        /// </summary>
        event EventHandler<FtpProgressEventArgs>? ProgressChanged;

        /// <summary>
        /// Event fired when connection status changes
        /// </summary>
        event EventHandler<FtpConnectionEventArgs>? ConnectionChanged;
    }

    /// <summary>
    /// FTP item (file or directory)
    /// </summary>
    public class FtpItem
    {
        /// <summary>
        /// Item name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Full path
        /// </summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>
        /// Whether item is directory
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// File size (0 for directories)
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Last modified date
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// File permissions
        /// </summary>
        public string Permissions { get; set; } = string.Empty;
    }

    /// <summary>
    /// FTP transfer progress event args
    /// </summary>
    public class FtpProgressEventArgs : EventArgs
    {
        /// <summary>
        /// File being transferred
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Bytes transferred
        /// </summary>
        public long BytesTransferred { get; set; }

        /// <summary>
        /// Total bytes to transfer
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage => TotalBytes > 0 ? (int)(BytesTransferred * 100 / TotalBytes) : 0;

        /// <summary>
        /// Transfer speed in bytes per second
        /// </summary>
        public double SpeedBytesPerSecond { get; set; }

        /// <summary>
        /// Transfer direction
        /// </summary>
        public FtpTransferDirection Direction { get; set; }
    }

    /// <summary>
    /// FTP connection event args
    /// </summary>
    public class FtpConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Server address
        /// </summary>
        public string Server { get; set; } = string.Empty;

        /// <summary>
        /// Whether connected
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Error message if connection failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// FTP transfer direction
    /// </summary>
    public enum FtpTransferDirection
    {
        Upload,
        Download
    }
}
