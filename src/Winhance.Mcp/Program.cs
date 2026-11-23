using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Winhance.Mcp;

var Version = (System.Reflection.Assembly.GetExecutingAssembly()
    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
    .FirstOrDefault() as System.Reflection.AssemblyInformationalVersionAttribute)?.InformationalVersion.Split('+')[0] ?? "1.5.0";

if (args.Any(a => a is "--version" or "-v"))
{
    Console.WriteLine($"Winhance-FS MCP Server {Version}");
    return 0;
}

if (args.Any(a => a is "--help" or "-h" or "/?" or "--config") || !Console.IsInputRedirected)
{
    // Started by hand (double-click or a terminal): explain how to connect instead of waiting on stdin.
    var exe = Environment.ProcessPath ?? "Winhance-FS-MCP.exe";
    var config = JsonSerializer.Serialize(
        new { mcpServers = new Dictionary<string, object> { ["winhance-fs"] = new { command = exe, args = Array.Empty<string>() } } },
        new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine($"""
        Winhance-FS MCP Server {Version}

        A read-only Model Context Protocol server that lets AI assistants (Claude Desktop, Claude Code, VS Code,
        Windsurf, Cursor...) search your drives instantly and inspect files with Winhance-FS's tools. It never
        changes, moves or deletes your files.

        Tools: search_files, largest_files, search_index_status, build_search_index, find_duplicates,
               hidden_streams, check_file_contents, previous_versions, recycle_bin

        Add it to your client's MCP configuration (for Claude Desktop: Settings > Developer > Edit Config):

        {config}

        Claude Code:  claude mcp add winhance-fs -- "{exe}"

        Searching uses the index built in Winhance-FS (File Manager > Search > Build Index), saved in
        {NexusIndexerService.DefaultIndexPath()}
        """);
    return 0;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

builder.Services.AddSingleton<ILogService, StderrLog>();
builder.Services.AddSingleton<INexusIndexerService>(sp => new NexusIndexerService(sp.GetService<ILogService>()));
builder.Services.AddSingleton<IDuplicateFinderService>(sp => new DuplicateFinderService(sp.GetRequiredService<ILogService>()));
builder.Services.AddSingleton<IForensicsService>(sp => new ForensicsService(sp.GetService<ILogService>()));
builder.Services.AddSingleton<SearchIndexHolder>();

builder.Services
    .AddMcpServer(o =>
    {
        o.ServerInfo = new Implementation { Name = "winhance-fs", Title = "Winhance-FS", Version = Version };
        o.ServerInstructions =
            "Winhance-FS tools for the user's Windows PC. All tools are read-only: they never change, move or delete files. " +
            "Use search_files to find files or folders by name on whole drives in milliseconds (it uses the index built by the " +
            "Winhance-FS app). Use largest_files to see what uses disk space. find_duplicates compares content (SHA-256). " +
            "hidden_streams, check_file_contents, previous_versions and recycle_bin are forensic checks: hidden NTFS data and " +
            "download markers, disguised or encrypted (ransomware-like) files, Windows snapshot copies, and deleted items. " +
            "Paths are Windows paths such as C:\\Users\\Name\\Documents.";
    })
    .WithStdioServerTransport()
    .WithTools<SearchTools>()
    .WithTools<InspectionTools>();

await builder.Build().RunAsync();
return 0;
