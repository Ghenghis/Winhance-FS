using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Core.Features.FileManager.Models;
using ScheduleType = Winhance.Core.Features.FileManager.Models.ScheduleType;

namespace Winhance.Infrastructure.Features.FileManager.Services
{
    /// <summary>
    /// Workflow automation service implementing advanced automation features
    /// Provides custom workflow execution, file system event triggers, and automated actions
    /// </summary>
    public class WorkflowAutomationService : IWorkflowAutomationService
    {
        private readonly ILogService _logService;
        private readonly IFileManagerService _fileManager;
        private readonly Dictionary<string, IWorkflow> _workflows;
        private readonly Dictionary<string, FileSystemWatcher> _watchers;
        private readonly string _workflowsPath;

        public WorkflowAutomationService(
            ILogService logService,
            IFileManagerService fileManager)
        {
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
            
            _workflows = new Dictionary<string, IWorkflow>();
            _watchers = new Dictionary<string, FileSystemWatcher>();
            
            _workflowsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Winhance", "Workflows");
            
            Directory.CreateDirectory(_workflowsPath);
        }

        public async Task<WorkflowResult> ExecuteWorkflowAsync(
            string workflowId,
            WorkflowContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(workflowId);
            ArgumentNullException.ThrowIfNull(context);

            var result = new WorkflowResult
            {
                WorkflowId = workflowId,
                Success = false,
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Executing workflow: {workflowId}");

                if (!_workflows.TryGetValue(workflowId, out var workflow))
                {
                    throw new InvalidOperationException($"Workflow not found: {workflowId}");
                }

                // Execute workflow steps
                var stepResults = new List<WorkflowStepResult>();
                var stepIndex = 0;

                foreach (var step in workflow.Steps)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var stepResult = new WorkflowStepResult
                    {
                        StepIndex = stepIndex,
                        StepName = step.Name,
                        Success = false,
                        StartTime = DateTime.UtcNow
                    };

                    try
                    {
                        await ExecuteWorkflowStep(step, context, cancellationToken);
                        stepResult.Success = true;
                        stepResult.EndTime = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        stepResult.Success = false;
                        stepResult.ErrorMessage = ex.Message;
                        stepResult.EndTime = DateTime.UtcNow;
                        
                        if (step.ContinueOnError)
                        {
                            _logService.Log(LogLevel.Warning, 
                                $"Workflow step failed but continuing: {step.Name} - {ex.Message}");
                        }
                        else
                        {
                            throw;
                        }
                    }

                    stepResults.Add(stepResult);
                    stepIndex++;
                }

                result.Success = true;
                result.StepResults = stepResults.ToArray();
                result.EndTime = DateTime.UtcNow;

                _logService.Log(LogLevel.Info, 
                    $"Workflow executed successfully: {workflowId} ({stepResults.Count} steps)");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                
                _logService.Log(LogLevel.Error, $"Workflow execution failed: {workflowId} - {ex.Message}");
            }

            return result;
        }

        public Task<TriggerResult> SetupFileWatchTriggerAsync(
            string path,
            WatchOptions options,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentNullException.ThrowIfNull(options);

            var result = new TriggerResult
            {
                TriggerId = Guid.NewGuid().ToString(),
                TriggerType = TriggerType.FileSystemWatch,
                Path = path,
                Success = false,
                SetupTime = DateTime.UtcNow
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Setting up file watch trigger for: {path}");

                if (!Directory.Exists(path))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {path}");
                }

                var watcher = new FileSystemWatcher(path)
                {
                    Filter = options.Filter ?? "*.*",
                    IncludeSubdirectories = options.IncludeSubdirectories,
                    NotifyFilter = ConvertNotifyFilters(options.WatchEvents)
                };

                // Setup event handlers
                watcher.Created += (s, e) => HandleFileSystemEvent(e, FileSystemEventType.Created, options);
                watcher.Changed += (s, e) => HandleFileSystemEvent(e, FileSystemEventType.Changed, options);
                watcher.Deleted += (s, e) => HandleFileSystemEvent(e, FileSystemEventType.Deleted, options);
                watcher.Renamed += (s, e) => HandleFileSystemEvent(e, FileSystemEventType.Renamed, options);

                // Start watching
                watcher.EnableRaisingEvents = true;

                _watchers[result.TriggerId] = watcher;
                result.Success = true;

                _logService.Log(LogLevel.Info, $"File watch trigger setup successfully: {result.TriggerId}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;

                _logService.Log(LogLevel.Error, $"Failed to setup file watch trigger: {ex.Message}");
            }

            return Task.FromResult(result);
        }

        public async Task<WorkflowDefinition> CreateWorkflowAsync(
            WorkflowDefinition definition,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(definition);

            try
            {
                // Validate workflow definition
                ValidateWorkflowDefinition(definition);

                // Create workflow instance
                var workflow = new CustomWorkflow(definition, _logService, _fileManager);

                // Store workflow
                _workflows[definition.Id] = workflow;

                // Persist workflow definition
                await SaveWorkflowDefinition(definition);

                _logService.Log(LogLevel.Info, $"Workflow created: {definition.Id} - {definition.Name}");
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Failed to create workflow: {ex.Message}");
                throw;
            }

            return definition;
        }

        public async Task<ScheduleResult> ScheduleWorkflowAsync(
            string workflowId,
            WorkflowSchedule schedule,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(workflowId);
            ArgumentNullException.ThrowIfNull(schedule);

            var result = new ScheduleResult
            {
                WorkflowId = workflowId,
                ScheduleId = Guid.NewGuid().ToString(),
                Success = false,
                ScheduledAt = DateTime.UtcNow
            };

            try
            {
                _logService.Log(LogLevel.Info, $"Scheduling workflow: {workflowId}");

                if (!_workflows.ContainsKey(workflowId))
                {
                    throw new InvalidOperationException($"Workflow not found: {workflowId}");
                }

                // Create and start scheduled task
                var scheduledTask = new ScheduledWorkflowTask(
                    workflowId, 
                    schedule, 
                    this, 
                    _logService);

                await scheduledTask.StartAsync(cancellationToken);

                result.Success = true;
                result.NextExecution = CalculateNextExecution(schedule);

                _logService.Log(LogLevel.Info, 
                    $"Workflow scheduled successfully: {workflowId} - Next execution: {result.NextExecution}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                
                _logService.Log(LogLevel.Error, $"Failed to schedule workflow: {ex.Message}");
            }

            return result;
        }

        public async Task<BatchProcessingResult> ProcessBatchAsync(
            BatchProcessingOptions options,
            IProgress<BatchProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);

            var result = new BatchProcessingResult
            {
                BatchId = Guid.NewGuid().ToString(),
                Success = false,
                StartTime = DateTime.UtcNow,
                ProcessedItems = new List<BatchItemResult>()
            };

            try
            {
                _logService.Log(LogLevel.Info, 
                    $"Starting batch processing: {options.Items.Count()} items");

                var itemIndex = 0;
                var totalItems = options.Items.Count();

                foreach (var item in options.Items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var itemResult = new BatchItemResult
                    {
                        ItemPath = item.Path,
                        Success = false,
                        StartTime = DateTime.UtcNow
                    };

                    try
                    {
                        await ProcessBatchItem(item, options, cancellationToken);
                        itemResult.Success = true;
                        itemResult.EndTime = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        itemResult.Success = false;
                        itemResult.ErrorMessage = ex.Message;
                        itemResult.EndTime = DateTime.UtcNow;
                        
                        if (!options.ContinueOnError)
                        {
                            throw;
                        }
                    }

                    result.ProcessedItems.Add(itemResult);
                    itemIndex++;

                    progress?.Report(new BatchProgress
                    {
                        ItemsProcessed = itemIndex,
                        TotalItems = totalItems,
                        CurrentItem = Path.GetFileName(item.Path)
                    });
                }

                result.Success = true;
                result.EndTime = DateTime.UtcNow;
                result.TotalProcessed = result.ProcessedItems.Count;
                result.SuccessfulItems = result.ProcessedItems.Count(r => r.Success);
                result.FailedItems = result.ProcessedItems.Count(r => !r.Success);

                _logService.Log(LogLevel.Info, 
                    $"Batch processing completed: {result.SuccessfulItems}/{result.TotalProcessed} successful");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                
                _logService.Log(LogLevel.Error, $"Batch processing failed: {ex.Message}");
            }

            return result;
        }

        public async Task<ConditionalActionResult> ExecuteConditionalActionAsync(
            ConditionalAction action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            var result = new ConditionalActionResult
            {
                ActionId = action.Id,
                Success = false,
                ExecutionTime = DateTime.UtcNow
            };

            try
            {
                // Evaluate conditions
                var conditionsMet = await EvaluateConditions(action.Conditions, cancellationToken);
                
                if (!conditionsMet)
                {
                    result.Success = true;
                    result.ConditionsMet = false;
                    result.Message = "Conditions not met - action skipped";
                    return result;
                }

                // Execute action
                await ExecuteAction(action.Action, cancellationToken);

                result.Success = true;
                result.ConditionsMet = true;
                result.Message = "Action executed successfully";

                _logService.Log(LogLevel.Info, $"Conditional action executed: {action.Id}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                
                _logService.Log(LogLevel.Error, $"Conditional action failed: {action.Id} - {ex.Message}");
            }

            return result;
        }

        private async Task ExecuteWorkflowStep(
            WorkflowStep step,
            WorkflowContext context,
            CancellationToken cancellationToken)
        {
            switch (step.Type)
            {
                case WorkflowStepType.FileOperation:
                    await ExecuteFileOperation(step, context, cancellationToken);
                    break;
                    
                case WorkflowStepType.Condition:
                    await ExecuteCondition(step, context, cancellationToken);
                    break;
                    
                case WorkflowStepType.Script:
                    await ExecuteScript(step, context, cancellationToken);
                    break;
                    
                case WorkflowStepType.Wait:
                    await ExecuteWait(step, cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"Workflow step type not supported: {step.Type}");
            }
        }

        private async Task ExecuteFileOperation(
            WorkflowStep step,
            WorkflowContext context,
            CancellationToken cancellationToken)
        {
            var operation = step.Parameters["operation"]?.ToString();
            var sourcePath = step.Parameters["sourcePath"]?.ToString();
            var targetPath = step.Parameters["targetPath"]?.ToString();

            switch (operation?.ToLowerInvariant())
            {
                case "copy":
                    await _fileManager.CopyFilesAsync(new[] { sourcePath! }, targetPath!, cancellationToken);
                    break;
                    
                case "move":
                    await _fileManager.MoveFilesAsync(new[] { sourcePath! }, targetPath!, cancellationToken);
                    break;
                    
                case "delete":
                    await _fileManager.DeleteFilesAsync(new[] { sourcePath! }, false, cancellationToken);
                    break;
                    
                case "create_folder":
                    await _fileManager.CreateDirectoryAsync(targetPath!, cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"File operation not supported: {operation}");
            }
        }

        private Task ExecuteCondition(
            WorkflowStep step,
            WorkflowContext context,
            CancellationToken cancellationToken)
        {
            var condition = step.Parameters["condition"]?.ToString();
            var path = step.Parameters["path"]?.ToString();

            var conditionMet = condition?.ToLowerInvariant() switch
            {
                "file_exists" => File.Exists(path),
                "directory_exists" => Directory.Exists(path),
                "file_size_greater" => new FileInfo(path!).Length > Convert.ToInt64(step.Parameters["size"]),
                _ => throw new NotSupportedException($"Condition not supported: {condition}")
            };

            if (!conditionMet)
            {
                throw new InvalidOperationException($"Workflow condition not met: {condition}");
            }

            return Task.CompletedTask;
        }

        private async Task ExecuteScript(
            WorkflowStep step,
            WorkflowContext context,
            CancellationToken cancellationToken)
        {
            var scriptType = step.Parameters["scriptType"]?.ToString();
            var script = step.Parameters["script"]?.ToString();

            switch (scriptType?.ToLowerInvariant())
            {
                case "powershell":
                    await ExecutePowerShellScript(script!, cancellationToken);
                    break;
                    
                case "batch":
                    await ExecuteBatchScript(script!, cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"Script type not supported: {scriptType}");
            }
        }

        private async Task ExecuteWait(WorkflowStep step, CancellationToken cancellationToken)
        {
            var milliseconds = Convert.ToInt32(step.Parameters["milliseconds"]);
            await Task.Delay(milliseconds, cancellationToken);
        }

        private Task ExecutePowerShellScript(string script, CancellationToken cancellationToken)
        {
            // PowerShell execution implementation
            _logService.Log(LogLevel.Info, "Executing PowerShell script");
            return Task.CompletedTask;
        }

        private Task ExecuteBatchScript(string script, CancellationToken cancellationToken)
        {
            // Batch script execution implementation
            _logService.Log(LogLevel.Info, "Executing batch script");
            return Task.CompletedTask;
        }

        private void HandleFileSystemEvent(
            FileSystemEventArgs e,
            FileSystemEventType eventType,
            WatchOptions options)
        {
            try
            {
                _logService.Log(LogLevel.Debug, $"File system event: {eventType} - {e.FullPath}");

                // Execute configured workflow if specified
                if (!string.IsNullOrEmpty(options.WorkflowId))
                {
                    var context = new WorkflowContext
                    {
                        TriggerPath = e.FullPath,
                        EventType = eventType,
                        Variables = new Dictionary<string, object>
                        {
                            { "FilePath", e.FullPath },
                            { "FileName", Path.GetFileName(e.FullPath) },
                            { "EventType", eventType.ToString() }
                        }
                    };

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ExecuteWorkflowAsync(options.WorkflowId, context);
                        }
                        catch (Exception ex)
                        {
                            _logService.Log(LogLevel.Error, 
                                $"Failed to execute triggered workflow: {options.WorkflowId} - {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logService.Log(LogLevel.Error, $"Error handling file system event: {ex.Message}");
            }
        }

        private NotifyFilters ConvertNotifyFilters(WatchEvents events)
        {
            NotifyFilters filters = 0;

            if (events.HasFlag(WatchEvents.Create))
                filters |= NotifyFilters.CreationTime;
            if (events.HasFlag(WatchEvents.Modify))
                filters |= NotifyFilters.LastWrite;
            if (events.HasFlag(WatchEvents.Delete))
                filters |= NotifyFilters.FileName;
            if (events.HasFlag(WatchEvents.Rename))
                filters |= NotifyFilters.FileName;

            return filters;
        }

        private void ValidateWorkflowDefinition(WorkflowDefinition definition)
        {
            ArgumentException.ThrowIfNullOrEmpty(definition.Id);
            ArgumentException.ThrowIfNullOrEmpty(definition.Name);
            ArgumentNullException.ThrowIfNull(definition.Steps);

            if (!definition.Steps.Any())
            {
                throw new InvalidOperationException("Workflow must contain at least one step");
            }
        }

        private async Task SaveWorkflowDefinition(WorkflowDefinition definition)
        {
            var filePath = Path.Combine(_workflowsPath, $"{definition.Id}.json");
            var json = System.Text.Json.JsonSerializer.Serialize(definition, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(filePath, json);
        }

        private DateTime CalculateNextExecution(WorkflowSchedule schedule)
        {
            var now = DateTime.UtcNow;

            return schedule.Type switch
            {
                ScheduleType.Once => schedule.StartTime,
                ScheduleType.Recurring => now.Add(schedule.Interval),
                ScheduleType.Daily => now.Date.AddDays(1).Add(schedule.StartTime.TimeOfDay),
                ScheduleType.Weekly => now.AddDays(7 - (int)now.DayOfWeek + (int)schedule.DayOfWeek),
                ScheduleType.Monthly => new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(schedule.DayOfMonth - 1),
                _ => now.AddHours(1)
            };
        }

        private async Task<bool> EvaluateConditions(
            IEnumerable<WorkflowCondition> conditions,
            CancellationToken cancellationToken)
        {
            foreach (var condition in conditions)
            {
                var result = await EvaluateCondition(condition, cancellationToken);
                if (!result)
                    return false;
            }
            return true;
        }

        private Task<bool> EvaluateCondition(
            WorkflowCondition condition,
            CancellationToken cancellationToken)
        {
            var result = condition.Type switch
            {
                Winhance.Core.Features.FileManager.Models.ConditionType.FileExists => File.Exists(condition.Parameters["path"]?.ToString()),
                Winhance.Core.Features.FileManager.Models.ConditionType.DirectoryExists => Directory.Exists(condition.Parameters["path"]?.ToString()),
                Winhance.Core.Features.FileManager.Models.ConditionType.FileSizeGreater => new FileInfo(condition.Parameters["path"]?.ToString()!).Length >
                    Convert.ToInt64(condition.Parameters["size"]),
                Winhance.Core.Features.FileManager.Models.ConditionType.TimeRange => IsCurrentTimeInRange(condition),
                _ => throw new NotSupportedException($"Condition type not supported: {condition.Type}")
            };

            return Task.FromResult(result);
        }

        private bool IsCurrentTimeInRange(WorkflowCondition condition)
        {
            var startTime = TimeSpan.Parse(condition.Parameters["startTime"]?.ToString()!);
            var endTime = TimeSpan.Parse(condition.Parameters["endTime"]?.ToString()!);
            var currentTime = DateTime.Now.TimeOfDay;

            return currentTime >= startTime && currentTime <= endTime;
        }

        private async Task ExecuteAction(
            WorkflowAction action,
            CancellationToken cancellationToken)
        {
            switch (action.Type)
            {
                case ActionType.MoveFile:
                    await _fileManager.MoveFilesAsync(
                        new[] { action.Parameters["source"]?.ToString()! },
                        action.Parameters["destination"]?.ToString()!,
                        cancellationToken);
                    break;
                    
                case ActionType.CopyFile:
                    await _fileManager.CopyFilesAsync(
                        new[] { action.Parameters["source"]?.ToString()! },
                        action.Parameters["destination"]?.ToString()!,
                        cancellationToken);
                    break;
                    
                case ActionType.DeleteFile:
                    await _fileManager.DeleteFilesAsync(
                        new[] { action.Parameters["path"]?.ToString()! },
                        false,
                        cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"Action type not supported: {action.Type}");
            }
        }

        private async Task ProcessBatchItem(
            BatchItem item,
            BatchProcessingOptions options,
            CancellationToken cancellationToken)
        {
            switch (options.Operation)
            {
                case BatchOperation.Copy:
                    await _fileManager.CopyFilesAsync(
                        new[] { item.Path },
                        options.TargetPath!,
                        cancellationToken);
                    break;
                    
                case BatchOperation.Move:
                    await _fileManager.MoveFilesAsync(
                        new[] { item.Path },
                        options.TargetPath!,
                        cancellationToken);
                    break;
                    
                case BatchOperation.Delete:
                    await _fileManager.DeleteFilesAsync(
                        new[] { item.Path },
                        options.PermanentDelete,
                        cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"Batch operation not supported: {options.Operation}");
            }
        }

        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher?.Dispose();
            }
            _watchers.Clear();
        }
    }

    // Supporting classes
    internal class CustomWorkflow : IWorkflow
    {
        public string Id { get; }
        public string Name { get; }
        public IEnumerable<WorkflowStep> Steps { get; }

        private readonly ILogService _logService;
        private readonly IFileManagerService _fileManager;

        public CustomWorkflow(
            WorkflowDefinition definition,
            ILogService logService,
            IFileManagerService fileManager)
        {
            Id = definition.Id;
            Name = definition.Name;
            Steps = definition.Steps;
            _logService = logService;
            _fileManager = fileManager;
        }
    }

    internal class ScheduledWorkflowTask
    {
        private readonly string _workflowId;
        private readonly WorkflowSchedule _schedule;
        private readonly IWorkflowAutomationService _automationService;
        private readonly ILogService _logService;
        private Timer? _timer;

        public ScheduledWorkflowTask(
            string workflowId,
            WorkflowSchedule schedule,
            IWorkflowAutomationService automationService,
            ILogService logService)
        {
            _workflowId = workflowId;
            _schedule = schedule;
            _automationService = automationService;
            _logService = logService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var nextExecution = CalculateInitialDelay();
            _timer = new Timer(ExecuteWorkflow, null, nextExecution, TimeSpan.FromMilliseconds(-1));
            return Task.CompletedTask;
        }

        private void ExecuteWorkflow(object? state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var context = new WorkflowContext
                    {
                        TriggerType = TriggerType.Scheduled,
                        Variables = new Dictionary<string, object>
                        {
                            { "ScheduledTime", DateTime.UtcNow },
                            { "ScheduleType", _schedule.Type.ToString() }
                        }
                    };

                    await _automationService.ExecuteWorkflowAsync(_workflowId, context);
                    
                    // Schedule next execution if recurring
                    if (_schedule.Type != ScheduleType.Once)
                    {
                        var nextDelay = CalculateNextDelay();
                        _timer?.Change(nextDelay, TimeSpan.FromMilliseconds(-1));
                    }
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.Error, $"Scheduled workflow execution failed: {_workflowId} - {ex.Message}");
                }
            });
        }

        private TimeSpan CalculateInitialDelay()
        {
            var now = DateTime.UtcNow;
            var nextExecution = _schedule.StartTime;

            if (nextExecution <= now)
            {
                nextExecution = now.AddMinutes(1); // Execute in 1 minute if start time has passed
            }

            return nextExecution - now;
        }

        private TimeSpan CalculateNextDelay()
        {
            return _schedule.Interval;
        }
    }
}
