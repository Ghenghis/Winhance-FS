using Winhance.Core.Features.Common.Enums;

// The File Manager services compiled into this server only need Log(level, message). This small interface
// stands in for the app's full ILogService so the server does not depend on the WPF app's logging stack.
namespace Winhance.Core.Features.Common.Interfaces
{
    public interface ILogService
    {
        void Log(LogLevel level, string message, Exception? exception = null);
    }
}

namespace Winhance.Mcp
{
    using Winhance.Core.Features.Common.Interfaces;

    /// <summary>Writes warnings and errors to stderr (stdout carries the MCP protocol).</summary>
    public sealed class StderrLog : ILogService
    {
        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            if (level is LogLevel.Warning or LogLevel.Error)
            {
                Console.Error.WriteLine($"[{level}] {message}{(exception != null ? ": " + exception.Message : string.Empty)}");
            }
        }
    }
}
