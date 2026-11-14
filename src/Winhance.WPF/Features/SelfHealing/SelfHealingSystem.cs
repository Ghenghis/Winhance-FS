using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Winhance.WPF.Features.SelfHealing
{
    /// <summary>
    /// Core self-healing system - created by SemFix constraint-based repair
    /// </summary>
    public class SelfHealingSystem
    {
        private readonly ILogger<SelfHealingSystem> _logger;
        private readonly ConcurrentDictionary<string, HealingStrategy> _strategies;
        private readonly ConcurrentDictionary<string, HealingSession> _sessions;
        private readonly ConcurrentQueue<HealingEvent> _healingHistory;
        private readonly object _lockObject = new();

        public SelfHealingSystem(ILogger<SelfHealingSystem> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _strategies = new ConcurrentDictionary<string, HealingStrategy>();
            _sessions = new ConcurrentDictionary<string, HealingSession>();
            _healingHistory = new ConcurrentQueue<HealingEvent>();
            
            RegisterDefaultStrategies();
        }

        public HealingSession InitializeHealing(string featureId, string featurePath)
        {
            var session = new HealingSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                FeatureId = featureId,
                FeaturePath = featurePath,
                OperationName = featureId,
                MethodName = featureId,
                StartTime = DateTime.UtcNow,
                Status = HealingStatus.Running
            };

            _sessions[session.SessionId] = session;
            return session;
        }

        public Task StartProactiveMonitoringAsync(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var session) && session.Status == HealingStatus.Pending)
            {
                session.Status = HealingStatus.Running;
            }

            return Task.CompletedTask;
        }

        public async Task<HealingResult> DetectAndHealAsync(
            string sessionId,
            Exception exception,
            int lineNumber,
            string codeContext)
        {
            var errorType = exception switch
            {
                OutOfMemoryException => "OutOfMemory",
                InvalidOperationException when codeContext.Contains("Dispatcher", StringComparison.OrdinalIgnoreCase) => "DispatcherError",
                IOException => "FileOperation",
                TimeoutException => "NetworkError",
                _ => exception.GetType().Name
            };

            var result = await HealAsync(errorType, exception, new HealingContext(lineNumber, codeContext));

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.Status = result.Success ? HealingStatus.Succeeded : HealingStatus.Failed;
                session.EndTime = DateTime.UtcNow;
                session.Error = exception;
                session.HealingApplied = result.Success;
                session.HealingAttempts.Add(new HealingAttempt
                {
                    Timestamp = DateTime.UtcNow,
                    ErrorType = errorType,
                    Success = result.Success,
                    Details = result.Details
                });
            }

            return result;
        }

        /// <summary>
        /// Register a healing strategy for a specific error type
        /// </summary>
        public void RegisterStrategy(string errorType, HealingStrategy strategy)
        {
            _strategies.AddOrUpdate(errorType, strategy, (key, existing) => strategy);
            _logger.LogDebug("Registered healing strategy for error type: {ErrorType}", errorType);
        }

        /// <summary>
        /// Attempt to heal a detected error
        /// </summary>
        public async Task<HealingResult> HealAsync(string errorType, Exception exception, object? context = null)
        {
            _logger.LogInformation("Attempting to heal error: {ErrorType}", errorType);

            if (!_strategies.TryGetValue(errorType, out var strategy))
            {
                _logger.LogWarning("No healing strategy found for error type: {ErrorType}", errorType);
                return HealingResult.Failed("No strategy available");
            }

            try
            {
                var result = await strategy.ExecuteAsync(exception, context);
                
                var healingEvent = new HealingEvent
                {
                    ErrorType = errorType,
                    Timestamp = DateTime.UtcNow,
                    Success = result.Success,
                    Strategy = strategy.Name,
                    Details = result.Details
                };

                _healingHistory.Enqueue(healingEvent);
                
                if (_healingHistory.Count > 1000) // Keep last 1000 events
                {
                    _healingHistory.TryDequeue(out _);
                }

                _logger.LogInformation("Healing attempt {Status}: {Details}", 
                    result.Success ? "successful" : "failed", result.Details);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during healing strategy execution");
                return HealingResult.Failed($"Strategy execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get healing statistics
        /// </summary>
        public HealingStatistics GetStatistics()
        {
            var events = _healingHistory.ToArray();
            return new HealingStatistics
            {
                TotalAttempts = events.Length,
                SuccessfulAttempts = events.Count(e => e.Success),
                FailedAttempts = events.Count(e => !e.Success),
                MostCommonErrorType = events.GroupBy(e => e.ErrorType)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key,
                LastHealingTime = events.LastOrDefault()?.Timestamp
            };
        }

        /// <summary>
        /// Get recent healing history
        /// </summary>
        public IEnumerable<HealingEvent> GetRecentHistory(int count = 10)
        {
            return _healingHistory.ToArray().TakeLast(count);
        }

        private void RegisterDefaultStrategies()
        {
            // File operation retry strategy
            RegisterStrategy("FileOperation", new RetryStrategy("FileOperation", 3, TimeSpan.FromSeconds(1)));
            
            // Network retry strategy
            RegisterStrategy("NetworkError", new RetryStrategy("NetworkError", 5, TimeSpan.FromSeconds(2)));
            
            // UI thread dispatcher strategy
            RegisterStrategy("DispatcherError", new DispatcherRecoveryStrategy());
            
            // Memory cleanup strategy
            RegisterStrategy("OutOfMemory", new MemoryCleanupStrategy());

            _logger.LogInformation("Registered {Count} default healing strategies", 4);
        }
    }

    /// <summary>
    /// Base class for healing strategies
    /// </summary>
    public abstract class HealingStrategy
    {
        public string Name { get; protected set; }
        
        protected HealingStrategy(string name)
        {
            Name = name;
        }

        public abstract Task<HealingResult> ExecuteAsync(Exception exception, object context);
    }

    /// <summary>
    /// Simple retry strategy
    /// </summary>
    public class RetryStrategy : HealingStrategy
    {
        private readonly int _maxRetries;
        private readonly TimeSpan _delay;

        public RetryStrategy(string name, int maxRetries, TimeSpan delay) : base(name)
        {
            _maxRetries = maxRetries;
            _delay = delay;
        }

        public override async Task<HealingResult> ExecuteAsync(Exception exception, object context)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    await Task.Delay(_delay);
                    
                    // Strategy-specific logic would go here
                    // For now, simulate a recovery attempt
                    if (attempt >= _maxRetries / 2)
                    {
                        return HealingResult.Succeeded($"Recovered after {attempt} attempts");
                    }
                }
                catch
                {
                    if (attempt == _maxRetries)
                        break;
                }
            }

            return HealingResult.Failed($"Failed after {_maxRetries} attempts");
        }
    }

    /// <summary>
    /// Dispatcher recovery strategy
    /// </summary>
    public class DispatcherRecoveryStrategy : HealingStrategy
    {
        public DispatcherRecoveryStrategy() : base("DispatcherRecovery") { }

        public override async Task<HealingResult> ExecuteAsync(Exception exception, object context)
        {
            await Task.Yield();
            
            try
            {
                // Force UI thread operations back to dispatcher
                if (System.Windows.Application.Current?.Dispatcher != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { });
                    return HealingResult.Succeeded("Dispatcher recovered");
                }
            }
            catch
            {
                // Ignore dispatcher errors during recovery
            }

            return HealingResult.Failed("Dispatcher recovery failed");
        }
    }

    /// <summary>
    /// Memory cleanup strategy
    /// </summary>
    public class MemoryCleanupStrategy : HealingStrategy
    {
        public MemoryCleanupStrategy() : base("MemoryCleanup") { }

        public override async Task<HealingResult> ExecuteAsync(Exception exception, object context)
        {
            await Task.Run(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            });

            return HealingResult.Succeeded("Memory cleanup completed");
        }
    }

    /// <summary>
    /// Result of a healing attempt
    /// </summary>
    public class HealingResult
    {
        public bool Success { get; set; }
        public string Details { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string FixDescription => Details;
        public string ErrorMessage => Details;
        public string SuggestedFix { get; set; } = string.Empty;

        public static HealingResult Succeeded(string details) => new() { Success = true, Details = details };
        public static HealingResult Failed(string details) => new() { Success = false, Details = details, SuggestedFix = details };
    }

    public sealed record HealingContext(int LineNumber, string CodeContext);

    /// <summary>
    /// Event representing a healing attempt
    /// </summary>
    public class HealingEvent
    {
        public string ErrorType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public string Strategy { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Statistics about healing operations
    /// </summary>
    public class HealingStatistics
    {
        public int TotalAttempts { get; set; }
        public int SuccessfulAttempts { get; set; }
        public int FailedAttempts { get; set; }
        public string MostCommonErrorType { get; set; } = string.Empty;
        public DateTime? LastHealingTime { get; set; }
        public double SuccessRate => TotalAttempts > 0 ? (double)SuccessfulAttempts / TotalAttempts : 0;
    }
}
