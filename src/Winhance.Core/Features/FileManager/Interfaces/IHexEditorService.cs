using System;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for hex editor functionality - created by SemFix constraint-based repair
    /// </summary>
    public interface IHexEditorService
    {
        /// <summary>
        /// Open file in hex editor
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <returns>Task indicating completion</returns>
        Task OpenFileAsync(string filePath);

        /// <summary>
        /// Read hex data from file at offset
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="length">Number of bytes to read</param>
        /// <returns>Hex data as byte array</returns>
        Task<byte[]> ReadHexDataAsync(string filePath, long offset, int length);

        /// <summary>
        /// Write hex data to file at offset
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="offset">Byte offset</param>
        /// <param name="data">Data to write</param>
        /// <returns>Task indicating completion</returns>
        Task WriteHexDataAsync(string filePath, long offset, byte[] data);

        /// <summary>
        /// Search for hex pattern in file
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="pattern">Hex pattern to search for</param>
        /// <returns>Array of byte offsets where pattern was found</returns>
        Task<long[]> SearchHexPatternAsync(string filePath, byte[] pattern);

        /// <summary>
        /// Get file size
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>File size in bytes</returns>
        Task<long> GetFileSizeAsync(string filePath);

        /// <summary>
        /// Check if file can be edited in hex editor
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>True if file can be hex edited</returns>
        bool CanEditFile(string filePath);

        /// <summary>
        /// Event fired when hex data changes
        /// </summary>
        event EventHandler<HexDataChangedEventArgs>? HexDataChanged;
    }

    /// <summary>
    /// Hex data change event arguments
    /// </summary>
    public class HexDataChangedEventArgs : EventArgs
    {
        /// <summary>
        /// File path that changed
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Byte offset of change
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// Length of change
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// New data
        /// </summary>
        public byte[] NewData { get; set; } = Array.Empty<byte>();
    }
}
