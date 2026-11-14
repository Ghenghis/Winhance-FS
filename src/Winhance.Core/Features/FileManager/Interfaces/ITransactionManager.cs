using System;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Interface for transaction management providing rollback capabilities for file operations
    /// </summary>
    public interface ITransactionManager : IDisposable
    {
        /// <summary>
        /// Begin a new transaction for file operations
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="description">Description of the transaction</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transaction ID</returns>
        Task<string> BeginTransactionAsync(
            string operationName, 
            string description, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Commit a transaction, making all operations permanent
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> CommitTransactionAsync(
            string transactionId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback a transaction, undoing all operations
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> RollbackTransactionAsync(
            string transactionId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Add an operation to a transaction
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="operation">Operation to add</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task AddOperationAsync(
            string transactionId, 
            TransactionOperation operation, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a specific operation within a transaction
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="operationId">Operation ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> ExecuteOperationAsync(
            string transactionId, 
            string operationId, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all currently active transactions
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of active transactions</returns>
        Task<FileTransaction[]> GetActiveTransactionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a specific transaction by ID
        /// </summary>
        /// <param name="transactionId">Transaction ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transaction or null if not found</returns>
        Task<FileTransaction?> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get transaction history for a date range
        /// </summary>
        /// <param name="fromDate">Start date</param>
        /// <param name="toDate">End date</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Array of transaction history entries</returns>
        Task<TransactionHistory[]> GetTransactionHistoryAsync(
            DateTime fromDate, 
            DateTime toDate, 
            CancellationToken cancellationToken = default);
    }
}
