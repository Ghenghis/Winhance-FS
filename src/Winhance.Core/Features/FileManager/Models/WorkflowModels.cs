using System;
using System.Collections.Generic;

namespace Winhance.Core.Features.FileManager.Models
{
    /// <summary>
    /// Workflow definition model
    /// </summary>
    public class WorkflowDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public List<WorkflowStep> Steps { get; set; } = new();
        public Dictionary<string, object> Variables { get; set; } = new();
    }

    /// <summary>
    /// Individual workflow step
    /// </summary>
    public class WorkflowStep
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkflowStepType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool ContinueOnError { get; set; }
        public int Order { get; set; }
        public List<WorkflowCondition> Conditions { get; set; } = new();
    }

    /// <summary>
    /// Workflow execution context
    /// </summary>
    public class WorkflowContext
    {
        public string? TriggerPath { get; set; }
        public FileSystemEventType EventType { get; set; }
        public TriggerType TriggerType { get; set; }
        public DateTime ExecutionTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Variables { get; set; } = new();
        public string? UserId { get; set; }
    }

    /// <summary>
    /// Workflow execution result
    /// </summary>
    public class WorkflowResult
    {
        public string WorkflowId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public WorkflowStepResult[] StepResults { get; set; } = Array.Empty<WorkflowStepResult>();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Individual step execution result
    /// </summary>
    public class WorkflowStepResult
    {
        public int StepIndex { get; set; }
        public string StepName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> OutputData { get; set; } = new();
    }

    /// <summary>
    /// File watch trigger options
    /// </summary>
    public class WatchOptions
    {
        public string Filter { get; set; } = "*.*";
        public bool IncludeSubdirectories { get; set; } = true;
        public WatchEvents WatchEvents { get; set; } = WatchEvents.All;
        public string? WorkflowId { get; set; }
        public TimeSpan? Debounce { get; set; }
    }

    /// <summary>
    /// Trigger setup result
    /// </summary>
    public class TriggerResult
    {
        public string TriggerId { get; set; } = string.Empty;
        public TriggerType TriggerType { get; set; }
        public string Path { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime SetupTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Workflow scheduling configuration
    /// </summary>
    public class WorkflowSchedule
    {
        public ScheduleType Type { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Interval { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int DayOfMonth { get; set; } = 1;
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Schedule operation result
    /// </summary>
    public class ScheduleResult
    {
        public string WorkflowId { get; set; } = string.Empty;
        public string ScheduleId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime? NextExecution { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Batch processing options
    /// </summary>
    public class BatchProcessingOptions
    {
        public IEnumerable<BatchItem> Items { get; set; } = Array.Empty<BatchItem>();
        public BatchOperation Operation { get; set; }
        public string? TargetPath { get; set; }
        public bool ContinueOnError { get; set; } = true;
        public bool PermanentDelete { get; set; }
        public int MaxParallelOperations { get; set; } = 4;
    }

    /// <summary>
    /// Individual batch item
    /// </summary>
    public class BatchItem
    {
        public string Path { get; set; } = string.Empty;
        public string? TargetPath { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Batch processing result
    /// </summary>
    public class BatchProcessingResult
    {
        public string BatchId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalProcessed { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public List<BatchItemResult> ProcessedItems { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Individual batch item result
    /// </summary>
    public class BatchItemResult
    {
        public string ItemPath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Batch progress information
    /// </summary>
    public class BatchProgress
    {
        public int ItemsProcessed { get; set; }
        public int TotalItems { get; set; }
        public string CurrentItem { get; set; } = string.Empty;
        public double PercentageComplete => TotalItems > 0 ? (double)ItemsProcessed / TotalItems * 100 : 0;
    }

    /// <summary>
    /// Conditional action definition
    /// </summary>
    public class ConditionalAction
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<WorkflowCondition> Conditions { get; set; } = new();
        public WorkflowAction Action { get; set; } = new();
    }

    /// <summary>
    /// Workflow condition
    /// </summary>
    public class WorkflowCondition
    {
        public string Id { get; set; } = string.Empty;
        public ConditionType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool Negate { get; set; }
    }

    /// <summary>
    /// Workflow action
    /// </summary>
    public class WorkflowAction
    {
        public string Id { get; set; } = string.Empty;
        public ActionType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Conditional action execution result
    /// </summary>
    public class ConditionalActionResult
    {
        public string ActionId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public bool ConditionsMet { get; set; }
        public DateTime ExecutionTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Workflow interface for custom implementations
    /// </summary>
    public interface IWorkflow
    {
        string Id { get; }
        string Name { get; }
        IEnumerable<WorkflowStep> Steps { get; }
    }

    /// <summary>
    /// Workflow step types
    /// </summary>
    public enum WorkflowStepType
    {
        FileOperation,
        Condition,
        Script,
        Wait,
        Email,
        Notification,
        Custom
    }

    /// <summary>
    /// File system event types for triggers
    /// </summary>
    public enum FileSystemEventType
    {
        Created,
        Changed,
        Deleted,
        Renamed
    }

    /// <summary>
    /// Trigger types
    /// </summary>
    public enum TriggerType
    {
        Manual,
        FileSystemWatch,
        Scheduled,
        SystemEvent
    }

    /// <summary>
    /// Watch events flags
    /// </summary>
    [Flags]
    public enum WatchEvents
    {
        None = 0,
        Create = 1,
        Modify = 2,
        Delete = 4,
        Rename = 8,
        All = Create | Modify | Delete | Rename
    }

    /// <summary>
    /// Schedule types
    /// </summary>
    public enum ScheduleType
    {
        Manual,
        OnChange,
        Interval,
        Once,
        Recurring,
        Daily,
        Weekly,
        Monthly
    }

    /// <summary>
    /// Batch operation types
    /// </summary>
    public enum BatchOperation
    {
        Copy,
        Move,
        Delete,
        Rename,
        Compress,
        Extract
    }

    /// <summary>
    /// Condition types
    /// </summary>
    public enum ConditionType
    {
        FileExists,
        DirectoryExists,
        FileSizeGreater,
        FileSizeLess,
        FileAge,
        TimeRange,
        DayOfWeek,
        Custom
    }

    /// <summary>
    /// Action types
    /// </summary>
    public enum ActionType
    {
        MoveFile,
        CopyFile,
        DeleteFile,
        RenameFile,
        CreateDirectory,
        ExecuteScript,
        SendEmail,
        ShowNotification,
        Custom
    }
}
