using System;
using System.Runtime.Serialization;

namespace Winhance.Infrastructure.Common.Exceptions
{
    /// <summary>
    /// Base exception for all file manager related errors
    /// </summary>
    [Serializable]
    public class FileManagerException : Exception
    {
        public string? OperationName { get; }
        public string? FilePath { get; }
        public ErrorCode ErrorCode { get; }

        public FileManagerException() : base()
        {
            ErrorCode = ErrorCode.Unknown;
        }

        public FileManagerException(string message) : base(message)
        {
            ErrorCode = ErrorCode.Unknown;
        }

        public FileManagerException(string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = ErrorCode.Unknown;
        }

        public FileManagerException(
            string message, 
            ErrorCode errorCode, 
            string? operationName = null, 
            string? filePath = null) : base(message)
        {
            ErrorCode = errorCode;
            OperationName = operationName;
            FilePath = filePath;
        }

        public FileManagerException(
            string message, 
            Exception innerException, 
            ErrorCode errorCode, 
            string? operationName = null, 
            string? filePath = null) : base(message, innerException)
        {
            ErrorCode = errorCode;
            OperationName = operationName;
            FilePath = filePath;
        }

        protected FileManagerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ErrorCode = (ErrorCode)(info.GetValue(nameof(ErrorCode), typeof(ErrorCode)) ?? ErrorCode.Unknown);
            OperationName = info.GetString(nameof(OperationName));
            FilePath = info.GetString(nameof(FilePath));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(OperationName), OperationName);
            info.AddValue(nameof(FilePath), FilePath);
        }
    }

    /// <summary>
    /// Exception thrown when a file operation fails due to access permissions
    /// </summary>
    [Serializable]
    public class FileAccessException : FileManagerException
    {
        public FileAccessException(string filePath, string operation) 
            : base($"Access denied for {operation} operation on: {filePath}", ErrorCode.AccessDenied, operation, filePath)
        {
        }

        public FileAccessException(string filePath, string operation, Exception innerException) 
            : base($"Access denied for {operation} operation on: {filePath}", innerException, ErrorCode.AccessDenied, operation, filePath)
        {
        }

        protected FileAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a file or directory is not found
    /// </summary>
    [Serializable]
    public class FileNotFoundManagerException : FileManagerException
    {
        public FileNotFoundManagerException(string filePath) 
            : base($"File or directory not found: {filePath}", ErrorCode.FileNotFound, filePath: filePath)
        {
        }

        public FileNotFoundManagerException(string filePath, string operation) 
            : base($"File or directory not found during {operation}: {filePath}", ErrorCode.FileNotFound, operation, filePath)
        {
        }

        protected FileNotFoundManagerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown when there's insufficient disk space
    /// </summary>
    [Serializable]
    public class InsufficientSpaceException : FileManagerException
    {
        public long RequiredSpace { get; }
        public long AvailableSpace { get; }

        public InsufficientSpaceException(long requiredSpace, long availableSpace, string? path = null) 
            : base($"Insufficient disk space. Required: {FormatBytes(requiredSpace)}, Available: {FormatBytes(availableSpace)}", 
                   ErrorCode.InsufficientSpace, filePath: path)
        {
            RequiredSpace = requiredSpace;
            AvailableSpace = availableSpace;
        }

        protected InsufficientSpaceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            RequiredSpace = info.GetInt64(nameof(RequiredSpace));
            AvailableSpace = info.GetInt64(nameof(AvailableSpace));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(RequiredSpace), RequiredSpace);
            info.AddValue(nameof(AvailableSpace), AvailableSpace);
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Exception thrown when a file operation conflicts with an existing file
    /// </summary>
    [Serializable]
    public class FileConflictException : FileManagerException
    {
        public string ExistingFilePath { get; }
        public ConflictType ConflictType { get; }

        public FileConflictException(string filePath, string existingFilePath, ConflictType conflictType) 
            : base($"File conflict ({conflictType}): {filePath} conflicts with {existingFilePath}", 
                   ErrorCode.FileConflict, filePath: filePath)
        {
            ExistingFilePath = existingFilePath;
            ConflictType = conflictType;
        }

        protected FileConflictException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ExistingFilePath = info.GetString(nameof(ExistingFilePath)) ?? string.Empty;
            ConflictType = (ConflictType)(info.GetValue(nameof(ConflictType), typeof(ConflictType)) ?? ConflictType.Unknown);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ExistingFilePath), ExistingFilePath);
            info.AddValue(nameof(ConflictType), ConflictType);
        }
    }

    /// <summary>
    /// Exception thrown when a file operation is cancelled
    /// </summary>
    [Serializable]
    public class OperationCancelledException : FileManagerException
    {
        public OperationCancelledException(string operation) 
            : base($"Operation cancelled: {operation}", ErrorCode.OperationCancelled, operation)
        {
        }

        protected OperationCancelledException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a file is locked by another process
    /// </summary>
    [Serializable]
    public class FileLockedException : FileManagerException
    {
        public string? LockingProcess { get; }

        public FileLockedException(string filePath, string? lockingProcess = null) 
            : base($"File is locked by another process: {filePath}" + 
                   (lockingProcess != null ? $" (Process: {lockingProcess})" : ""), 
                   ErrorCode.FileLocked, filePath: filePath)
        {
            LockingProcess = lockingProcess;
        }

        protected FileLockedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            LockingProcess = info.GetString(nameof(LockingProcess));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(LockingProcess), LockingProcess);
        }
    }

    /// <summary>
    /// Exception thrown when archive operations fail
    /// </summary>
    [Serializable]
    public class ArchiveOperationException : FileManagerException
    {
        public string ArchivePath { get; }
        public ArchiveOperation Operation { get; }

        public ArchiveOperationException(string archivePath, ArchiveOperation operation, string message) 
            : base($"Archive {operation} failed for {archivePath}: {message}", ErrorCode.ArchiveError, operation.ToString(), archivePath)
        {
            ArchivePath = archivePath;
            Operation = operation;
        }

        public ArchiveOperationException(string archivePath, ArchiveOperation operation, string message, Exception innerException) 
            : base($"Archive {operation} failed for {archivePath}: {message}", innerException, ErrorCode.ArchiveError, operation.ToString(), archivePath)
        {
            ArchivePath = archivePath;
            Operation = operation;
        }

        protected ArchiveOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ArchivePath = info.GetString(nameof(ArchivePath)) ?? string.Empty;
            Operation = (ArchiveOperation)(info.GetValue(nameof(Operation), typeof(ArchiveOperation)) ?? ArchiveOperation.Unknown);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ArchivePath), ArchivePath);
            info.AddValue(nameof(Operation), Operation);
        }
    }

    /// <summary>
    /// Error codes for file manager exceptions
    /// </summary>
    public enum ErrorCode
    {
        Unknown = 0,
        AccessDenied = 1001,
        FileNotFound = 1002,
        DirectoryNotFound = 1003,
        InsufficientSpace = 1004,
        FileConflict = 1005,
        OperationCancelled = 1006,
        FileLocked = 1007,
        InvalidPath = 1008,
        NetworkError = 1009,
        ArchiveError = 1010,
        EncryptionError = 1011,
        IndexingError = 1012,
        SearchError = 1013,
        TransactionError = 1014,
        ConfigurationError = 1015
    }

    /// <summary>
    /// Types of file conflicts
    /// </summary>
    public enum ConflictType
    {
        Unknown,
        FileAlreadyExists,
        DirectoryAlreadyExists,
        NameConflict,
        SizeConflict,
        DateConflict
    }

    /// <summary>
    /// Archive operation types
    /// </summary>
    public enum ArchiveOperation
    {
        Unknown,
        Create,
        Extract,
        Add,
        Remove,
        Update,
        Test
    }
}
