using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using Winhance.Infrastructure.Common.Exceptions;

namespace Winhance.Infrastructure.Features.FileManager.Services
{
    /// <summary>
    /// Transaction manager providing rollback capabilities for file operations
    /// Ensures data integrity and provides recovery from failed operations
    /// </summary>
    public class TransactionManager : ITransactionManager
    {
        private readonly ILogService _logService;
        private readonly string _transactionLogPath;
        private readonly string _backupPath;
        private readonly Dictionary<string, FileTransaction> _activeTransactions;
        private readonly object _lockObject = new();

        public TransactionManager(ILogService logService)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Winhance", "Transactions");
            
            _transactionLogPath = Path.Combine(appDataPath, "transactions.log");
            _backupPath = Path.Combine(appDataPath, "backups");
            
            Directory.CreateDirectory(appDataPath);
            Directory.CreateDirectory(_backupPath);
            
            _activeTransactions = new Dictionary<string, FileTransaction>();
        }

        public async Task<string> BeginTransactionAsync(
            string operationName,
            string description,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(operationName);
            ArgumentException.ThrowIfNullOrEmpty(description);

            var transactionId = Guid.NewGuid().ToString();
            var transaction = new FileTransaction
            {
                Id = transactionId,
                OperationName = operationName,
                Description = description,
                Status = TransactionStatus.Active,
                StartTime = DateTime.UtcNow,
                Operations = new List<TransactionOperation>()
            };

            lock (_lockObject)
            {
                _activeTransactions[transactionId] = transaction;
            }

            await LogTransactionEvent(transactionId, "TRANSACTION_STARTED", $"Operation: {operationName}");
            _logService.Log(LogLevel.Info, $"Transaction started: {transactionId} - {operationName}");

            return transactionId;
        }

        public async Task<bool> CommitTransactionAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(transactionId);

            FileTransaction? transaction;
            lock (_lockObject)
            {
                if (!_activeTransactions.TryGetValue(transactionId, out transaction))
                {
                    throw new TransactionException($"Transaction not found: {transactionId}");
                }
            }

            try
            {
                // Verify all operations completed successfully
                foreach (var operation in transaction.Operations)
                {
                    if (!operation.IsSuccessful)
                    {
                        throw new TransactionException($"Cannot commit transaction {transactionId}: Operation {operation.Id} failed");
                    }
                }

                // Mark transaction as committed
                transaction.Status = TransactionStatus.Committed;
                transaction.EndTime = DateTime.UtcNow;

                // Clean up backups for committed operations
                await CleanupTransactionBackups(transaction);

                await LogTransactionEvent(transactionId, "TRANSACTION_COMMITTED", 
                    $"Operations: {transaction.Operations.Count}");

                lock (_lockObject)
                {
                    _activeTransactions.Remove(transactionId);
                }

                _logService.Log(LogLevel.Info, $"Transaction committed: {transactionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Failed to commit transaction {transactionId}: {ex.Message}");
                await LogTransactionEvent(transactionId, "COMMIT_FAILED", ex.Message);
                throw;
            }
        }

        public async Task<bool> RollbackTransactionAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(transactionId);

            FileTransaction? transaction;
            lock (_lockObject)
            {
                if (!_activeTransactions.TryGetValue(transactionId, out transaction))
                {
                    throw new TransactionException($"Transaction not found: {transactionId}");
                }
            }

            try
            {
                _logService.Log(LogLevel.Info, $"Rolling back transaction: {transactionId}");
                await LogTransactionEvent(transactionId, "ROLLBACK_STARTED", 
                    $"Operations to rollback: {transaction.Operations.Count}");

                var rollbackSuccessful = true;
                var rollbackErrors = new List<string>();

                // Rollback operations in reverse order
                for (int i = transaction.Operations.Count - 1; i >= 0; i--)
                {
                    var operation = transaction.Operations[i];
                    
                    try
                    {
                        await RollbackOperation(operation, cancellationToken);
                        operation.Status = TransactionOperationStatus.RolledBack;
                    }
                    catch (Exception ex)
                    {
                        rollbackSuccessful = false;
                        rollbackErrors.Add($"Operation {operation.Id}: {ex.Message}");
                        operation.Status = TransactionOperationStatus.RollbackFailed;
                        _logService.Log(LogLevel.Error, $"Failed to rollback operation {operation.Id}: {ex.Message}");
                    }
                }

                transaction.Status = rollbackSuccessful ? TransactionStatus.RolledBack : TransactionStatus.RollbackFailed;
                transaction.EndTime = DateTime.UtcNow;
                transaction.RollbackErrors = rollbackErrors.ToArray();

                await LogTransactionEvent(transactionId, 
                    rollbackSuccessful ? "ROLLBACK_COMPLETED" : "ROLLBACK_FAILED",
                    rollbackSuccessful ? "All operations rolled back" : $"Errors: {string.Join("; ", rollbackErrors)}");

                lock (_lockObject)
                {
                    _activeTransactions.Remove(transactionId);
                }

                _logService.Log(rollbackSuccessful ? LogLevel.Info : LogLevel.Error, 
                    $"Transaction rollback {(rollbackSuccessful ? "completed" : "failed")}: {transactionId}");

                return rollbackSuccessful;
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Critical error during transaction rollback {transactionId}: {ex.Message}");
                await LogTransactionEvent(transactionId, "ROLLBACK_CRITICAL_ERROR", ex.Message);
                throw;
            }
        }

        public async Task AddOperationAsync(
            string transactionId,
            TransactionOperation operation,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(transactionId);
            ArgumentNullException.ThrowIfNull(operation);

            FileTransaction? transaction;
            lock (_lockObject)
            {
                if (!_activeTransactions.TryGetValue(transactionId, out transaction))
                {
                    throw new TransactionException($"Transaction not found: {transactionId}");
                }
            }

            if (transaction.Status != TransactionStatus.Active)
            {
                throw new TransactionException($"Cannot add operation to inactive transaction: {transactionId}");
            }

            // Create backup if needed
            if (operation.RequiresBackup)
            {
                await CreateOperationBackup(operation);
            }

            operation.Id = Guid.NewGuid().ToString();
            operation.Timestamp = DateTime.UtcNow;
            operation.Status = TransactionOperationStatus.Pending;

            lock (_lockObject)
            {
                transaction.Operations.Add(operation);
            }

            await LogTransactionEvent(transactionId, "OPERATION_ADDED", 
                $"Type: {operation.Type}, Path: {operation.SourcePath}");
        }

        public async Task<bool> ExecuteOperationAsync(
            string transactionId,
            string operationId,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(transactionId);
            ArgumentException.ThrowIfNullOrEmpty(operationId);

            FileTransaction? transaction;
            TransactionOperation? operation = null;

            lock (_lockObject)
            {
                if (!_activeTransactions.TryGetValue(transactionId, out transaction))
                {
                    throw new TransactionException($"Transaction not found: {transactionId}");
                }

                operation = transaction.Operations.Find(op => op.Id == operationId);
                if (operation == null)
                {
                    throw new TransactionException($"Operation not found: {operationId}");
                }
            }

            try
            {
                operation.Status = TransactionOperationStatus.InProgress;
                await ExecuteFileOperation(operation, cancellationToken);
                
                operation.Status = TransactionOperationStatus.Completed;
                operation.IsSuccessful = true;
                operation.CompletedAt = DateTime.UtcNow;

                await LogTransactionEvent(transactionId, "OPERATION_COMPLETED", 
                    $"Operation {operationId} completed successfully");

                return true;
            }
            catch (Exception ex)
            {
                operation.Status = TransactionOperationStatus.Failed;
                operation.IsSuccessful = false;
                operation.ErrorMessage = ex.Message;
                operation.CompletedAt = DateTime.UtcNow;

                await LogTransactionEvent(transactionId, "OPERATION_FAILED", 
                    $"Operation {operationId} failed: {ex.Message}");

                _logService.Log(LogLevel.Error, $"Operation failed {operationId}: {ex.Message}");
                throw;
            }
        }

        public Task<FileTransaction[]> GetActiveTransactionsAsync(CancellationToken cancellationToken = default)
        {
            lock (_lockObject)
            {
                return Task.FromResult(_activeTransactions.Values.ToArray());
            }
        }

        public Task<FileTransaction?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(transactionId);

            lock (_lockObject)
            {
                return Task.FromResult(_activeTransactions.TryGetValue(transactionId, out var transaction) ? transaction : null);
            }
        }

        public async Task<TransactionHistory[]> GetTransactionHistoryAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default)
        {
            var history = new List<TransactionHistory>();

            try
            {
                if (!File.Exists(_transactionLogPath)) return history.ToArray();

                var lines = await File.ReadAllLinesAsync(_transactionLogPath, cancellationToken);
                
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var parts = line.Split('|');
                        if (parts.Length >= 4)
                        {
                            var timestamp = DateTime.Parse(parts[0]);
                            if (timestamp >= fromDate && timestamp <= toDate)
                            {
                                history.Add(new TransactionHistory
                                {
                                    Timestamp = timestamp,
                                    TransactionId = parts[1],
                                    EventType = parts[2],
                                    Details = parts[3],
                                    UserName = parts.Length > 4 ? parts[4] : Environment.UserName
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.Log(LogLevel.Warning, $"Failed to parse transaction log line: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Failed to read transaction history: {ex.Message}");
            }

            return history.OrderByDescending(h => h.Timestamp).ToArray();
        }

        private Task CreateOperationBackup(TransactionOperation operation)
        {
            if (string.IsNullOrEmpty(operation.SourcePath)) return Task.CompletedTask;

            try
            {
                var backupDir = Path.Combine(_backupPath, operation.Id);
                Directory.CreateDirectory(backupDir);

                if (File.Exists(operation.SourcePath))
                {
                    var backupFile = Path.Combine(backupDir, Path.GetFileName(operation.SourcePath));
                    File.Copy(operation.SourcePath, backupFile, true);
                    operation.BackupPath = backupFile;
                }
                else if (Directory.Exists(operation.SourcePath))
                {
                    var backupSubDir = Path.Combine(backupDir, Path.GetFileName(operation.SourcePath));
                    CopyDirectory(operation.SourcePath, backupSubDir);
                    operation.BackupPath = backupSubDir;
                }
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Warning, $"Failed to create backup for operation {operation.Id}: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        private async Task ExecuteFileOperation(TransactionOperation operation, CancellationToken cancellationToken)
        {
            switch (operation.Type)
            {
                case TransactionOperationType.Copy:
                    await ExecuteCopyOperation(operation, cancellationToken);
                    break;
                    
                case TransactionOperationType.Move:
                    await ExecuteMoveOperation(operation, cancellationToken);
                    break;
                    
                case TransactionOperationType.Delete:
                    await ExecuteDeleteOperation(operation, cancellationToken);
                    break;
                    
                case TransactionOperationType.Create:
                    await ExecuteCreateOperation(operation, cancellationToken);
                    break;
                    
                case TransactionOperationType.Rename:
                    await ExecuteRenameOperation(operation, cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"Operation type not supported: {operation.Type}");
            }
        }

        private Task ExecuteCopyOperation(TransactionOperation operation, CancellationToken cancellationToken)
        {
            if (File.Exists(operation.SourcePath!))
            {
                File.Copy(operation.SourcePath!, operation.TargetPath!, operation.Overwrite);
            }
            else if (Directory.Exists(operation.SourcePath!))
            {
                CopyDirectory(operation.SourcePath!, operation.TargetPath!);
            }
            else
            {
                throw new FileNotFoundManagerException(operation.SourcePath!, "Copy");
            }

            return Task.CompletedTask;
        }

        private Task ExecuteMoveOperation(TransactionOperation operation, CancellationToken cancellationToken)
        {
            if (File.Exists(operation.SourcePath!))
            {
                if (operation.Overwrite && File.Exists(operation.TargetPath!))
                {
                    File.Delete(operation.TargetPath!);
                }
                File.Move(operation.SourcePath!, operation.TargetPath!);
            }
            else if (Directory.Exists(operation.SourcePath!))
            {
                Directory.Move(operation.SourcePath!, operation.TargetPath!);
            }
            else
            {
                throw new FileNotFoundManagerException(operation.SourcePath!, "Move");
            }

            return Task.CompletedTask;
        }

        private Task ExecuteDeleteOperation(TransactionOperation operation, CancellationToken cancellationToken)
        {
            if (File.Exists(operation.SourcePath!))
            {
                File.Delete(operation.SourcePath!);
            }
            else if (Directory.Exists(operation.SourcePath!))
            {
                Directory.Delete(operation.SourcePath!, true);
            }

            return Task.CompletedTask;
        }

        private async Task ExecuteCreateOperation(TransactionOperation operation, CancellationToken cancellationToken)
        {
            if (operation.IsDirectory)
            {
                Directory.CreateDirectory(operation.TargetPath!);
            }
            else
            {
                await File.WriteAllTextAsync(operation.TargetPath!, operation.Content ?? "", cancellationToken);
            }
        }

        private Task ExecuteRenameOperation(TransactionOperation operation, CancellationToken cancellationToken)
        {
            if (File.Exists(operation.SourcePath!))
            {
                File.Move(operation.SourcePath!, operation.TargetPath!);
            }
            else if (Directory.Exists(operation.SourcePath!))
            {
                Directory.Move(operation.SourcePath!, operation.TargetPath!);
            }
            else
            {
                throw new FileNotFoundManagerException(operation.SourcePath!, "Rename");
            }

            return Task.CompletedTask;
        }

        private async Task RollbackOperation(TransactionOperation operation, CancellationToken cancellationToken)
        {
            switch (operation.Type)
            {
                case TransactionOperationType.Copy:
                    await RollbackCopyOperation(operation);
                    break;
                    
                case TransactionOperationType.Move:
                    await RollbackMoveOperation(operation);
                    break;
                    
                case TransactionOperationType.Delete:
                    await RollbackDeleteOperation(operation);
                    break;
                    
                case TransactionOperationType.Create:
                    await RollbackCreateOperation(operation);
                    break;
                    
                case TransactionOperationType.Rename:
                    await RollbackRenameOperation(operation);
                    break;
            }
        }

        private Task RollbackCopyOperation(TransactionOperation operation)
        {
            // Delete the copied file/directory
            if (File.Exists(operation.TargetPath!))
            {
                File.Delete(operation.TargetPath!);
            }
            else if (Directory.Exists(operation.TargetPath!))
            {
                Directory.Delete(operation.TargetPath!, true);
            }

            return Task.CompletedTask;
        }

        private Task RollbackMoveOperation(TransactionOperation operation)
        {
            // Move back from target to source
            if (File.Exists(operation.TargetPath!))
            {
                File.Move(operation.TargetPath!, operation.SourcePath!);
            }
            else if (Directory.Exists(operation.TargetPath!))
            {
                Directory.Move(operation.TargetPath!, operation.SourcePath!);
            }

            return Task.CompletedTask;
        }

        private Task RollbackDeleteOperation(TransactionOperation operation)
        {
            // Restore from backup
            if (!string.IsNullOrEmpty(operation.BackupPath) && File.Exists(operation.BackupPath))
            {
                File.Copy(operation.BackupPath, operation.SourcePath!, true);
            }
            else if (!string.IsNullOrEmpty(operation.BackupPath) && Directory.Exists(operation.BackupPath))
            {
                CopyDirectory(operation.BackupPath, operation.SourcePath!);
            }

            return Task.CompletedTask;
        }

        private Task RollbackCreateOperation(TransactionOperation operation)
        {
            // Delete the created file/directory
            if (File.Exists(operation.TargetPath!))
            {
                File.Delete(operation.TargetPath!);
            }
            else if (Directory.Exists(operation.TargetPath!))
            {
                Directory.Delete(operation.TargetPath!, true);
            }

            return Task.CompletedTask;
        }

        private Task RollbackRenameOperation(TransactionOperation operation)
        {
            // Rename back from target to source
            if (File.Exists(operation.TargetPath!))
            {
                File.Move(operation.TargetPath!, operation.SourcePath!);
            }
            else if (Directory.Exists(operation.TargetPath!))
            {
                Directory.Move(operation.TargetPath!, operation.SourcePath!);
            }

            return Task.CompletedTask;
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                CopyDirectory(subDir, destSubDir);
            }
        }

        private Task CleanupTransactionBackups(FileTransaction transaction)
        {
            foreach (var operation in transaction.Operations)
            {
                if (!string.IsNullOrEmpty(operation.BackupPath))
                {
                    try
                    {
                        var backupDir = Path.GetDirectoryName(operation.BackupPath);
                        if (!string.IsNullOrEmpty(backupDir) && Directory.Exists(backupDir))
                        {
                            Directory.Delete(backupDir, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.Log(LogLevel.Warning, $"Failed to cleanup backup for operation {operation.Id}: {ex.Message}");
                    }
                }
            }

            return Task.CompletedTask;
        }

        private async Task LogTransactionEvent(string transactionId, string eventType, string details)
        {
            try
            {
                var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}|{transactionId}|{eventType}|{details}|{Environment.UserName}";
                await File.AppendAllTextAsync(_transactionLogPath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Warning, $"Failed to write transaction log: {ex.Message}");
            }
        }

        public void Dispose()
        {
            // Rollback any remaining active transactions
            var activeTransactions = GetActiveTransactionsAsync().Result;
            
            foreach (var transaction in activeTransactions)
            {
                try
                {
                    RollbackTransactionAsync(transaction.Id).Wait(TimeSpan.FromSeconds(30));
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.Error, $"Failed to rollback transaction during disposal: {transaction.Id} - {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Exception thrown when transaction operations fail
    /// </summary>
    [Serializable]
    public class TransactionException : FileManagerException
    {
        public string? TransactionId { get; }

        public TransactionException(string message) 
            : base(message, ErrorCode.TransactionError)
        {
        }

        public TransactionException(string message, string transactionId) 
            : base(message, ErrorCode.TransactionError)
        {
            TransactionId = transactionId;
        }

        public TransactionException(string message, Exception innerException) 
            : base(message, innerException, ErrorCode.TransactionError)
        {
        }

        public TransactionException(string message, Exception innerException, string transactionId) 
            : base(message, innerException, ErrorCode.TransactionError)
        {
            TransactionId = transactionId;
        }

        protected TransactionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
            TransactionId = info.GetString(nameof(TransactionId));
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(TransactionId), TransactionId);
        }
    }
}
