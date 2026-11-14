using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Service for managing file and folder security permissions.
    /// </summary>
    public interface ISecurityService
    {
        /// <summary>
        /// Gets the security information for a file or folder.
        /// </summary>
        Task<SecurityInfo> GetSecurityInfoAsync(string path);

        /// <summary>
        /// Sets the security information for a file or folder.
        /// </summary>
        Task<bool> SetSecurityInfoAsync(string path, SecurityInfo securityInfo);

        /// <summary>
        /// Checks if the current user has the specified permissions.
        /// </summary>
        Task<bool> CheckPermissionsAsync(string path, SecurityPermissionFlags permissions);

        /// <summary>
        /// Gets the owner of a file or folder.
        /// </summary>
        Task<string> GetOwnerAsync(string path);

        /// <summary>
        /// Sets the owner of a file or folder.
        /// </summary>
        Task<bool> SetOwnerAsync(string path, string owner);

        /// <summary>
        /// Takes ownership of a file or folder.
        /// </summary>
        Task<bool> TakeOwnershipAsync(string path);

        /// <summary>
        /// Gets the effective permissions for the current user.
        /// </summary>
        Task<SecurityPermissionFlags> GetEffectivePermissionsAsync(string path);

        /// <summary>
        /// Copies permissions from one path to another.
        /// </summary>
        Task<bool> CopyPermissionsAsync(string sourcePath, string destinationPath);

        /// <summary>
        /// Resets permissions to inherited defaults.
        /// </summary>
        Task<bool> ResetPermissionsAsync(string path);
    }

    /// <summary>
    /// Represents security information for a file or folder.
    /// </summary>
    public class SecurityInfo
    {
        /// <summary>
        /// The path this security info applies to.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// The owner of the file or folder.
        /// </summary>
        public string Owner { get; set; } = string.Empty;

        /// <summary>
        /// The group owner of the file or folder.
        /// </summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>
        /// Whether the item is a directory.
        /// </summary>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Whether permissions are inherited from parent.
        /// </summary>
        public bool InheritedFromParent { get; set; }

        /// <summary>
        /// The access control entries.
        /// </summary>
        public List<AccessControlEntry> AccessEntries { get; set; } = new();

        /// <summary>
        /// The audit entries.
        /// </summary>
        public List<AuditEntry> AuditEntries { get; set; } = new();
    }

    /// <summary>
    /// Represents an access control entry.
    /// </summary>
    public class AccessControlEntry
    {
        /// <summary>
        /// The identity (user or group) this entry applies to.
        /// </summary>
        public string Identity { get; set; } = string.Empty;

        /// <summary>
        /// The permissions granted or denied.
        /// </summary>
        public SecurityPermissionFlags Permissions { get; set; }

        /// <summary>
        /// Whether this is an allow or deny entry.
        /// </summary>
        public SecurityAccessType AccessType { get; set; }

        /// <summary>
        /// Whether this entry is inherited.
        /// </summary>
        public bool IsInherited { get; set; }

        /// <summary>
        /// Inheritance flags.
        /// </summary>
        public InheritanceType Inheritance { get; set; }
    }

    /// <summary>
    /// Represents an audit entry.
    /// </summary>
    public class AuditEntry
    {
        /// <summary>
        /// The identity this audit applies to.
        /// </summary>
        public string Identity { get; set; } = string.Empty;

        /// <summary>
        /// The permissions being audited.
        /// </summary>
        public SecurityPermissionFlags AuditedPermissions { get; set; }

        /// <summary>
        /// Whether to audit success.
        /// </summary>
        public bool AuditSuccess { get; set; }

        /// <summary>
        /// Whether to audit failure.
        /// </summary>
        public bool AuditFailure { get; set; }
    }

    /// <summary>
    /// File permission flags for security operations.
    /// </summary>
    [Flags]
    public enum SecurityPermissionFlags
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        Delete = 8,
        ReadPermissions = 16,
        ChangePermissions = 32,
        TakeOwnership = 64,
        ListDirectory = 128,
        CreateFiles = 256,
        CreateDirectories = 512,
        DeleteSubdirectories = 1024,
        Traverse = 2048,
        ReadAttributes = 4096,
        WriteAttributes = 8192,
        ReadExtendedAttributes = 16384,
        WriteExtendedAttributes = 32768,
        FullControl = Read | Write | Execute | Delete | ReadPermissions | ChangePermissions | TakeOwnership
    }

    /// <summary>
    /// Security access control type.
    /// </summary>
    public enum SecurityAccessType
    {
        Allow,
        Deny
    }

    /// <summary>
    /// Inheritance type for permissions.
    /// </summary>
    [Flags]
    public enum InheritanceType
    {
        None = 0,
        ContainerInherit = 1,
        ObjectInherit = 2,
        InheritOnly = 4,
        NoPropagateInherit = 8
    }
}
