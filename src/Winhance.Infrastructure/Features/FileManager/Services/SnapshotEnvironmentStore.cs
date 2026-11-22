using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services;

/// <summary>
/// Read-only environment loaded from a JSON snapshot (the same format as Environment Doctor backups).
/// Used to analyze another PC's environment (copy its backup file here) and for demos/documentation
/// screenshots without exposing this PC's variables. <see cref="CanWrite"/> is always false, so repairs are
/// previewed but never applied; nothing ever touches the registry.
/// </summary>
public sealed class SnapshotEnvironmentStore : IEnvironmentStore
{
    /// <summary>Environment variable that points Winhance at a snapshot instead of this PC's registry.</summary>
    public const string SnapshotVariable = "WINHANCE_ENV_SNAPSHOT";

    private readonly List<EnvVariable> _variables;
    private readonly string _path;

    public SnapshotEnvironmentStore(string path)
    {
        _path = path;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var list = root.ValueKind == JsonValueKind.Array ? root : root.GetProperty("Variables");
        _variables = list.EnumerateArray().Select(e => new EnvVariable(
            Enum.Parse<EnvScope>(Read(e, "Scope"), ignoreCase: true),
            Read(e, "Name"),
            Read(e, "RawValue"),
            Enum.Parse<EnvValueKind>(Read(e, "Kind"), ignoreCase: true))).ToList();
    }

    public string Description => $"Snapshot (read-only): {Path.GetFileName(_path)}";

    public IReadOnlyList<EnvVariable> Read(EnvScope scope) => _variables.Where(v => v.Scope == scope).ToList();

    public void Write(EnvScope scope, string name, string value, EnvValueKind kind) =>
        throw new UnauthorizedAccessException("A snapshot is read-only.");

    public void Delete(EnvScope scope, string name) =>
        throw new UnauthorizedAccessException("A snapshot is read-only.");

    public void BroadcastChange()
    {
    }

    public bool CanWrite(EnvScope scope) => false;

    /// <summary>The store to use: a snapshot if <see cref="SnapshotVariable"/> names an existing file, else the registry.</summary>
    public static IEnvironmentStore CreateDefault()
    {
        var path = Environment.GetEnvironmentVariable(SnapshotVariable);
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path)
            ? new SnapshotEnvironmentStore(path)
            : new RegistryEnvironmentStore();
    }

    private static string Read(JsonElement e, string name)
    {
        foreach (var p in e.EnumerateObject())
        {
            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return p.Value.ValueKind == JsonValueKind.Number ? p.Value.GetRawText() : p.Value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
