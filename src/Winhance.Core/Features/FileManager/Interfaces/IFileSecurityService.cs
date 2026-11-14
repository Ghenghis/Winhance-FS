using System;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Interface for file security and encryption services
    /// </summary>
    public interface IFileSecurityService
    {
        /// <summary>
        /// Encrypt a file with specified options
        /// </summary>
        /// <param name="filePath">Path to the file to encrypt</param>
        /// <param name="options">Encryption options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Encryption result</returns>
        Task<EncryptionResult> EncryptFileAsync(
            string filePath, 
            EncryptionOptions options, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrypt an encrypted file
        /// </summary>
        /// <param name="encryptedFilePath">Path to the encrypted file</param>
        /// <param name="options">Decryption options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Decryption result</returns>
        Task<DecryptionResult> DecryptFileAsync(
            string encryptedFilePath, 
            DecryptionOptions options, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze file permissions and security settings
        /// </summary>
        /// <param name="path">Path to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Permission analysis result</returns>
        Task<PermissionAnalysis> AnalyzePermissionsAsync(
            string path, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get file access audit trail
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="range">Time range for audit</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of audit trail entries</returns>
        Task<AuditTrail[]> GetFileAccessAuditAsync(
            string filePath, 
            TimeRange range, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Securely delete a file with multiple overwrite passes
        /// </summary>
        /// <param name="filePath">Path to the file to delete</param>
        /// <param name="options">Secure delete options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Secure delete result</returns>
        Task<SecureDeleteResult> SecureDeleteAsync(
            string filePath, 
            SecureDeleteOptions? options = null, 
            CancellationToken cancellationToken = default);
    }
}
