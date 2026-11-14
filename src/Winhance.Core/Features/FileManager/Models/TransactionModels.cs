using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Represents a file transaction with rollback capabilities
    /// </summary>
    public class FileTransaction
    {
        public string Id { get; set; } = string.Empty;
        public string OperationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<TransactionOperation> Operations { get; set; } = new();
        public string[]? RollbackErrors { get; set; }
    }

    /// <summary>
    /// Represents a single operation within a transaction
    /// </summary>
    public class TransactionOperation
    {
        public string Id { get; set; } = string.Empty;
        public TransactionOperationType Type { get; set; }
        public string? SourcePath { get; set; }
        public string? TargetPath { get; set; }
        public string? BackupPath { get; set; }
        public string? Content { get; set; }
        public bool IsDirectory { get; set; }
        public bool Overwrite { get; set; }
        public bool RequiresBackup { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TransactionOperationStatus Status { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Transaction status enumeration
    /// </summary>
    public enum TransactionStatus
    {
        Active,
        Committed,
        RolledBack,
        Failed,
        RollbackFailed
    }

    /// <summary>
    /// Transaction operation type enumeration
    /// </summary>
    public enum TransactionOperationType
    {
        Copy,
        Move,
        Delete,
        Create,
        Rename,
        Modify
    }

    /// <summary>
    /// Transaction operation status enumeration
    /// </summary>
    public enum TransactionOperationStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        RolledBack,
        RollbackFailed
    }

    /// <summary>
    /// Transaction history entry
    /// </summary>
    public class TransactionHistory
    {
        public DateTime Timestamp { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
    }
}
