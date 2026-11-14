using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.Common.Models;

namespace Winhance.Infrastructure.Features.Common.Services
{
    /// <summary>
    /// Comprehensive logging service with file, console, and structured logging support
    /// </summary>
    public class LogService : ILogService, IDisposable
    {
        private readonly LogConfiguration _config;
        private readonly ConcurrentQueue<LogEntry> _logQueue;
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _writeSemaphore;
        private readonly string _logFilePath;
        private readonly object _lockObject = new();
        private bool _disposed;

        // ILogService event
        public event EventHandler<LogMessageEventArgs>? LogMessageGenerated;

        public LogService(LogConfiguration? config = null)
        {
            _config = config ?? new LogConfiguration();
            _logQueue = new ConcurrentQueue<LogEntry>();
            _writeSemaphore = new SemaphoreSlim(1, 1);
            
            // Create log directory
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Winhance", "Logs");
            Directory.CreateDirectory(logDirectory);
            
            // Generate log file path with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logFilePath = Path.Combine(logDirectory, $"winhance_{timestamp}.log");
            
            // Start flush timer
            _flushTimer = new Timer(FlushLogs, null, 
                TimeSpan.FromSeconds(_config.FlushIntervalSeconds), 
                TimeSpan.FromSeconds(_config.FlushIntervalSeconds));

            // Log service initialization
            Log(LogLevel.Info, "LogService initialized", "LogService");
        }

        // Primary Log method
        public void Log(LogLevel level, string message, string? source = null, Exception? exception = null)
        {
            if (_disposed || !ShouldLog(level))
                return;

            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Source = source ?? GetCallerInfo(),
                Exception = exception,
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                ProcessId = Environment.ProcessId
            };

            _logQueue.Enqueue(logEntry);

            // Fire event
            LogMessageGenerated?.Invoke(this, new LogMessageEventArgs(level, message, exception));

            // Console output for immediate visibility
            if (_config.EnableConsoleOutput && level >= _config.ConsoleLogLevel)
            {
                WriteToConsole(logEntry);
            }

            // Force immediate flush for critical errors
            if (level == LogLevel.Critical)
            {
                _ = Task.Run(() => FlushLogs(null));
            }
        }

        // ILogService interface implementations
        public void StartLog()
        {
            // Already initialized in constructor
        }

        public void StopLog()
        {
            Dispose();
        }

        public void LogInformation(string message)
        {
            Log(LogLevel.Info, message, "ILogService");
        }

        public void LogWarning(string message)
        {
            Log(LogLevel.Warning, message, "ILogService");
        }

        public void LogError(string message, Exception? exception = null)
        {
            Log(LogLevel.Error, message, "ILogService", exception);
        }

        public void LogSuccess(string message)
        {
            Log(LogLevel.Info, $"SUCCESS: {message}", "ILogService");
        }

        public string GetLogPath()
        {
            return _logFilePath;
        }

        public async Task LogPerformanceAsync(string operation, TimeSpan duration, object? additionalData = null)
        {
            if (!_config.EnablePerformanceLogging)
                return;

            var message = $"Performance: {operation} completed in {duration.TotalMilliseconds:F2}ms";
            if (additionalData != null)
            {
                message += $" - Data: {additionalData}";
            }
            
            await Task.Run(() => Log(LogLevel.Debug, message, "Performance"));
        }

        public void Log(LogLevel level, string message, Exception? exception)
        {
            Log(level, message, "ILogService", exception);
        }

        // Convenience methods
        public void LogDebug(string message, string? source = null)
        {
            Log(LogLevel.Debug, message, source);
        }

        public void LogInfo(string message, string? source = null)
        {
            Log(LogLevel.Info, message, source);
        }

        public void LogCritical(string message, Exception? exception = null, string? source = null)
        {
            Log(LogLevel.Critical, message, source, exception);
        }

        public Task LogPerformanceAsync(string operation, TimeSpan duration, string? source = null)
        {
            if (!_config.EnablePerformanceLogging)
                return Task.CompletedTask;

            var message = $"Performance: {operation} completed in {duration.TotalMilliseconds:F2}ms";
            Log(LogLevel.Info, message, source ?? "PerformanceTracker");
            return Task.CompletedTask;
        }

        public Task<IDisposable> StartPerformanceTrackingAsync(string operation, string? source = null)
        {
            return Task.FromResult<IDisposable>(new PerformanceTracker(this, operation, source));
        }

        public async Task<string[]> GetRecentLogsAsync(int count = 100, LogLevel minLevel = LogLevel.Debug)
        {
            var logs = new List<string>();
            
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var lines = await File.ReadAllLinesAsync(_logFilePath);
                    var filteredLines = new List<string>();
                    
                    foreach (var line in lines)
                    {
                        if (TryParseLogLevel(line, out var lineLevel) && lineLevel >= minLevel)
                        {
                            filteredLines.Add(line);
                        }
                    }
                    
                    // Get the most recent entries
                    var startIndex = Math.Max(0, filteredLines.Count - count);
                    logs.AddRange(filteredLines.GetRange(startIndex, filteredLines.Count - startIndex));
                }
            }
            catch (Exception ex)
            {
                Log(LogLevel.Warning, $"Failed to read recent logs: {ex.Message}", "LogService");
            }
            
            return logs.ToArray();
        }

        public async Task ClearLogsAsync()
        {
            try
            {
                await _writeSemaphore.WaitAsync();
                
                if (File.Exists(_logFilePath))
                {
                    File.Delete(_logFilePath);
                }
                
                Log(LogLevel.Info, "Log file cleared", "LogService");
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, $"Failed to clear logs: {ex.Message}", "LogService", ex);
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        private bool ShouldLog(LogLevel level)
        {
            return level >= _config.MinimumLogLevel;
        }

        private string GetCallerInfo()
        {
            try
            {
                var stackTrace = new StackTrace(2, true);
                var frame = stackTrace.GetFrame(0);
                
                if (frame != null)
                {
                    var method = frame.GetMethod();
                    var className = method?.DeclaringType?.Name ?? "Unknown";
                    var methodName = method?.Name ?? "Unknown";
                    return $"{className}.{methodName}";
                }
            }
            catch
            {
                // Ignore errors in caller detection
            }
            
            return "Unknown";
        }

        private void WriteToConsole(LogEntry entry)
        {
            try
            {
                var color = GetConsoleColor(entry.Level);
                var originalColor = Console.ForegroundColor;
                
                Console.ForegroundColor = color;
                Console.WriteLine(FormatLogEntry(entry, _config.ConsoleFormat));
                Console.ForegroundColor = originalColor;
            }
            catch
            {
                // Ignore console write errors
            }
        }

        private ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Magenta,
                _ => ConsoleColor.White
            };
        }

        private void FlushLogs(object? state)
        {
            if (_disposed || _logQueue.IsEmpty)
                return;

            Task.Run(async () =>
            {
                try
                {
                    await _writeSemaphore.WaitAsync();
                    
                    var entries = new List<LogEntry>();
                    while (_logQueue.TryDequeue(out var entry) && entries.Count < _config.MaxBatchSize)
                    {
                        entries.Add(entry);
                    }
                    
                    if (entries.Count > 0)
                    {
                        await WriteLogEntries(entries);
                    }
                }
                catch (Exception ex)
                {
                    // Try to write error to console as fallback
                    Console.WriteLine($"[ERROR] LogService flush failed: {ex.Message}");
                }
                finally
                {
                    _writeSemaphore.Release();
                }
            });
        }

        private async Task WriteLogEntries(List<LogEntry> entries)
        {
            var sb = new StringBuilder();
            
            foreach (var entry in entries)
            {
                sb.AppendLine(FormatLogEntry(entry, _config.FileFormat));
            }
            
            await File.AppendAllTextAsync(_logFilePath, sb.ToString());
            
            // Rotate log file if it gets too large
            if (new FileInfo(_logFilePath).Length > _config.MaxFileSizeBytes)
            {
                await RotateLogFile();
            }
        }

        private string FormatLogEntry(LogEntry entry, LogFormat format)
        {
            return format switch
            {
                LogFormat.Simple => $"[{entry.Level}] {entry.Message}",
                LogFormat.Detailed => $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{entry.Level}] ({entry.Source}) {entry.Message}" +
                                    (entry.Exception != null ? $" | Exception: {entry.Exception}" : ""),
                LogFormat.Json => System.Text.Json.JsonSerializer.Serialize(new
                {
                    timestamp = entry.Timestamp,
                    level = entry.Level.ToString(),
                    message = entry.Message,
                    source = entry.Source,
                    threadId = entry.ThreadId,
                    processId = entry.ProcessId,
                    exception = entry.Exception?.ToString()
                }),
                _ => $"{entry.Timestamp:HH:mm:ss} [{entry.Level}] {entry.Message}"
            };
        }

        private async Task RotateLogFile()
        {
            try
            {
                var directory = Path.GetDirectoryName(_logFilePath)!;
                var fileName = Path.GetFileNameWithoutExtension(_logFilePath);
                var extension = Path.GetExtension(_logFilePath);
                
                var archivedPath = Path.Combine(directory, $"{fileName}_archived_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}");
                File.Move(_logFilePath, archivedPath);
                
                // Clean up old archived files
                await CleanupOldLogFiles(directory);
                
                Log(LogLevel.Info, $"Log file rotated to: {archivedPath}", "LogService");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Log rotation failed: {ex.Message}");
            }
        }

        private Task CleanupOldLogFiles(string directory)
        {
            try
            {
                var logFiles = Directory.GetFiles(directory, "winhance_*.log");
                Array.Sort(logFiles, (x, y) => File.GetCreationTime(y).CompareTo(File.GetCreationTime(x)));

                // Keep only the most recent files
                for (int i = _config.MaxArchivedFiles; i < logFiles.Length; i++)
                {
                    File.Delete(logFiles[i]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Log cleanup failed: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        private bool TryParseLogLevel(string line, out LogLevel level)
        {
            level = LogLevel.Debug;
            
            if (string.IsNullOrEmpty(line))
                return false;
            
            if (line.Contains("[DEBUG]") || line.Contains("[Debug]"))
                level = LogLevel.Debug;
            else if (line.Contains("[INFO]") || line.Contains("[Info]"))
                level = LogLevel.Info;
            else if (line.Contains("[WARNING]") || line.Contains("[Warning]"))
                level = LogLevel.Warning;
            else if (line.Contains("[ERROR]") || line.Contains("[Error]"))
                level = LogLevel.Error;
            else if (line.Contains("[CRITICAL]") || line.Contains("[Critical]"))
                level = LogLevel.Critical;
            else
                return false;
            
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            
            _disposed = true;
            
            // Flush remaining logs
            FlushLogs(null);
            Thread.Sleep(500); // Give flush time to complete
            
            _flushTimer?.Dispose();
            _writeSemaphore?.Dispose();
            
            Log(LogLevel.Info, "LogService disposed", "LogService");
        }
    }

    /// <summary>
    /// Log entry model
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Source { get; set; }
        public Exception? Exception { get; set; }
        public int ThreadId { get; set; }
        public int ProcessId { get; set; }
    }

    /// <summary>
    /// Log configuration options
    /// </summary>
    public class LogConfiguration
    {
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Info;
        public LogLevel ConsoleLogLevel { get; set; } = LogLevel.Warning;
        public bool EnableConsoleOutput { get; set; } = true;
        public bool EnablePerformanceLogging { get; set; } = true;
        public LogFormat FileFormat { get; set; } = LogFormat.Detailed;
        public LogFormat ConsoleFormat { get; set; } = LogFormat.Simple;
        public int FlushIntervalSeconds { get; set; } = 5;
        public int MaxBatchSize { get; set; } = 100;
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
        public int MaxArchivedFiles { get; set; } = 10;
    }

    /// <summary>
    /// Log format options
    /// </summary>
    public enum LogFormat
    {
        Simple,
        Detailed,
        Json
    }

    /// <summary>
    /// Performance tracking helper
    /// </summary>
    internal class PerformanceTracker : IDisposable
    {
        private readonly ILogService _logService;
        private readonly string _operation;
        private readonly string? _source;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public PerformanceTracker(ILogService logService, string operation, string? source)
        {
            _logService = logService;
            _operation = operation;
            _source = source;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _stopwatch.Stop();
            
            _ = _logService.LogPerformanceAsync(_operation, _stopwatch.Elapsed, _source);
        }
    }
}