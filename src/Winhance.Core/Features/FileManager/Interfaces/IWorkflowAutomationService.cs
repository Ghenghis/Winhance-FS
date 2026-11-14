using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.FileManager.Models;

namespace Winhance.Core.Features.FileManager.Interfaces
{
    /// <summary>
    /// Interface for workflow automation services
    /// </summary>
    public interface IWorkflowAutomationService : IDisposable
    {
        /// <summary>
        /// Execute a workflow with the given context
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <param name="context">Workflow execution context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Workflow execution result</returns>
        Task<WorkflowResult> ExecuteWorkflowAsync(
            string workflowId, 
            WorkflowContext context, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Setup a file system watch trigger
        /// </summary>
        /// <param name="path">Path to watch</param>
        /// <param name="options">Watch options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Trigger setup result</returns>
        Task<TriggerResult> SetupFileWatchTriggerAsync(
            string path, 
            WatchOptions options, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a new workflow definition
        /// </summary>
        /// <param name="definition">Workflow definition</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created workflow definition</returns>
        Task<WorkflowDefinition> CreateWorkflowAsync(
            WorkflowDefinition definition, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Schedule a workflow for execution
        /// </summary>
        /// <param name="workflowId">Workflow identifier</param>
        /// <param name="schedule">Schedule configuration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Schedule result</returns>
        Task<ScheduleResult> ScheduleWorkflowAsync(
            string workflowId, 
            WorkflowSchedule schedule, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Process multiple items in batch
        /// </summary>
        /// <param name="options">Batch processing options</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Batch processing result</returns>
        Task<BatchProcessingResult> ProcessBatchAsync(
            BatchProcessingOptions options, 
            IProgress<BatchProgress>? progress = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute a conditional action based on conditions
        /// </summary>
        /// <param name="action">Conditional action to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Conditional action result</returns>
        Task<ConditionalActionResult> ExecuteConditionalActionAsync(
            ConditionalAction action, 
            CancellationToken cancellationToken = default);
    }
}
