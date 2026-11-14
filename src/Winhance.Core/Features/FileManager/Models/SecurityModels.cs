using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Encryption options for file security operations
    /// </summary>
    public class EncryptionOptions
    {
        public EncryptionMethod Method { get; set; } = EncryptionMethod.AES256GCM;
        public string Password { get; set; } = string.Empty;
        public string? OutputPath { get; set; }
        public bool DeleteOriginal { get; set; }
        public int KeyDerivationIterations { get; set; } = 100000;
        public long MaxFileSizeBytes { get; set; } = 1024L * 1024 * 1024 * 5; // 5GB
    }

    /// <summary>
    /// Decryption options for file security operations
    /// </summary>
    public class DecryptionOptions
    {
        public string Password { get; set; } = string.Empty;
        public string? OutputPath { get; set; }
    }

    /// <summary>
    /// Result of encryption operation
    /// </summary>
    public class EncryptionResult
    {
        public bool Success { get; set; }
        public string OriginalPath { get; set; } = string.Empty;
        public string? EncryptedPath { get; set; }
        public EncryptionMethod EncryptionMethod { get; set; }
        public long OriginalSize { get; set; }
        public long EncryptedSize { get; set; }
        public bool OriginalDeleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of decryption operation
    /// </summary>
    public class DecryptionResult
    {
        public bool Success { get; set; }
        public string EncryptedPath { get; set; } = string.Empty;
        public string? DecryptedPath { get; set; }
        public string? OriginalFileName { get; set; }
        public EncryptionMethod EncryptionMethod { get; set; }
        public long DecryptedSize { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// File permission analysis result
    /// </summary>
    public class PermissionAnalysis
    {
        public string Path { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; }
        public string Owner { get; set; } = string.Empty;
        public bool IsReadOnly { get; set; }
        public bool IsHidden { get; set; }
        public bool IsSystem { get; set; }
        public List<FilePermission> Permissions { get; set; } = new();
        public List<SecurityIssue> SecurityIssues { get; set; } = new();
    }

    /// <summary>
    /// File permission entry
    /// </summary>
    public class FilePermission
    {
        public string Identity { get; set; } = string.Empty;
        public string Rights { get; set; } = string.Empty;
        public string AccessType { get; set; } = string.Empty;
        public bool IsInherited { get; set; }
        public string InheritanceFlags { get; set; } = string.Empty;
        public string PropagationFlags { get; set; } = string.Empty;
    }

    /// <summary>
    /// Security issue identified during analysis
    /// </summary>
    public class SecurityIssue
    {
        public SecuritySeverity Severity { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Audit trail entry for file access
    /// </summary>
    public class AuditTrail
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? ProcessName { get; set; }
    }

    /// <summary>
    /// Time range for audit queries
    /// </summary>
    public class TimeRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int IntervalDays { get; set; } = 1;
    }

    /// <summary>
    /// Secure delete options
    /// </summary>
    public class SecureDeleteOptions
    {
        public SecureDeleteMethod Method { get; set; } = SecureDeleteMethod.DOD_3_Pass;
    }

    /// <summary>
    /// Result of secure delete operation
    /// </summary>
    public class SecureDeleteResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long OriginalSize { get; set; }
        public int PassesCompleted { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Encryption metadata stored with encrypted files
    /// </summary>
    public class EncryptionMetadata
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public EncryptionMethod EncryptionMethod { get; set; }
        public DateTime EncryptedAt { get; set; }
        public long OriginalSize { get; set; }
        public int KeyDerivationIterations { get; set; }
        public string Salt { get; set; } = string.Empty;
    }

    /// <summary>
    /// Encryption methods supported
    /// </summary>
    public enum EncryptionMethod
    {
        AES256GCM,
        AES256CBC,
        ChaCha20Poly1305
    }

    /// <summary>
    /// Security severity levels
    /// </summary>
    public enum SecuritySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Secure delete methods
    /// </summary>
    public enum SecureDeleteMethod
    {
        SimpleOverwrite,
        DOD_3_Pass,
        Gutmann_35_Pass
    }
}
