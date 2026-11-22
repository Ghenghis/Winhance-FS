using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services;

/// <summary>
/// Diagnoses and repairs Windows environment variables. Detection never changes anything; repairs go through
/// <see cref="BuildPlan"/> (merge + safety checks) and <see cref="ApplyAsync"/> (backup, drift check, write,
/// verify by re-reading, automatic rollback, broadcast).
/// </summary>
public sealed partial class EnvironmentDoctorService : IEnvironmentDoctorService
{
    private const int MaxVariableLength = 32767;
    private const int LegacyPathLength = 2047;
    private const int MaxBackups = 50;

    private static readonly string[] DirectoryListVariables = { "Path", "PSModulePath" };

    private static readonly string[] ProtectedFromDelete = { "Path", "PATHEXT", "ComSpec", "windir", "PSModulePath", "OS" };

    private static readonly string[] StandardPathExt = { ".COM", ".EXE", ".BAT", ".CMD" };

    private const string DefaultPathExt = ".COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC";

    /// <summary>Essential Machine PATH entries, in their conventional order, with the severity when missing.</summary>
    private static readonly (string Entry, EnvSeverity Severity)[] EssentialMachinePath =
    {
        (@"%SystemRoot%\system32", EnvSeverity.Error),
        (@"%SystemRoot%", EnvSeverity.Error),
        (@"%SystemRoot%\System32\Wbem", EnvSeverity.Error),
        (@"%SYSTEMROOT%\System32\WindowsPowerShell\v1.0\", EnvSeverity.Warning),
        (@"%SYSTEMROOT%\System32\OpenSSH\", EnvSeverity.Info),
    };

    /// <summary>System32 commands that other toolchains (Git, MSYS, Cygwin, GnuWin) commonly shadow.</summary>
    private static readonly string[] SystemCommands =
    {
        "find", "sort", "where", "timeout", "tar", "curl", "ssh", "more", "fc", "expand", "hostname", "ping",
        "ipconfig", "cmd", "powershell", "bash", "wsl", "choice", "comp", "xcopy", "robocopy", "whoami",
        "certutil", "net", "sc", "tree", "format", "label", "attrib", "findstr",
    };

    private static readonly string[] Watched =
    {
        "python", "python3", "py", "pip", "node", "npm", "npx", "java", "javac", "git", "dotnet", "go", "cargo",
        "rustc", "pwsh", "code", "ruby", "perl", "php", "gcc", "cmake", "adb", "mvn", "gradle", "conda", "uv",
        "bun", "deno", "curl", "tar", "ssh",
    };

    /// <summary>Commands where two different versions on PATH are a real conflict.</summary>
    private static readonly HashSet<string> RuntimeCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "python", "python3", "node", "java", "javac", "dotnet", "go", "ruby", "perl", "php", "gcc", "pwsh",
    };

    /// <summary>Home/root variables: required file (relative), command used to find a replacement, folder levels up.</summary>
    private static readonly Dictionary<string, (string Marker, string? Command, int LevelsUp)> HomeVariables =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["JAVA_HOME"] = (@"bin\java.exe", "java", 2),
            ["JDK_HOME"] = (@"bin\javac.exe", "javac", 2),
            ["JRE_HOME"] = (@"bin\java.exe", "java", 2),
            ["GOROOT"] = (@"bin\go.exe", "go", 2),
            ["GOPATH"] = (string.Empty, null, 0),
            ["CARGO_HOME"] = (@"bin", "cargo", 2),
            ["RUSTUP_HOME"] = (string.Empty, null, 0),
            ["ANDROID_HOME"] = ("platform-tools", "adb", 2),
            ["ANDROID_SDK_ROOT"] = ("platform-tools", "adb", 2),
            ["NVM_HOME"] = ("nvm.exe", null, 0),
            ["NVM_SYMLINK"] = ("node.exe", "node", 1),
            ["MAVEN_HOME"] = (@"bin\mvn.cmd", "mvn", 2),
            ["M2_HOME"] = (@"bin\mvn.cmd", "mvn", 2),
            ["GRADLE_HOME"] = (@"bin\gradle.bat", "gradle", 2),
            ["DOTNET_ROOT"] = ("dotnet.exe", "dotnet", 1),
            ["PYTHONHOME"] = ("python.exe", "python", 1),
            ["VCPKG_ROOT"] = ("vcpkg.exe", null, 0),
            ["FLUTTER_ROOT"] = (@"bin\flutter.bat", null, 0),
            ["CUDA_PATH"] = (@"bin\nvcc.exe", null, 0),
            ["ChocolateyInstall"] = ("choco.exe", null, 0),
            ["PNPM_HOME"] = (string.Empty, null, 0),
            ["VULKAN_SDK"] = (string.Empty, null, 0),
            ["CONDA_HOME"] = (string.Empty, null, 0),
        };

    private readonly IEnvironmentStore _store;
    private readonly ILogService _log;
    private readonly string _backupRoot;

    public EnvironmentDoctorService(ILogService logService)
        : this(new RegistryEnvironmentStore(), logService, null)
    {
    }

    public EnvironmentDoctorService(IEnvironmentStore store, ILogService logService, string? backupRoot)
    {
        _store = store;
        _log = logService;
        _backupRoot = backupRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Winhance", "EnvironmentBackups");
    }

    public IReadOnlyList<string> WatchedCommands => Watched;

    public EnvSnapshot TakeSnapshot() =>
        new(DateTime.UtcNow, _store.Read(EnvScope.Machine).Concat(_store.Read(EnvScope.User)).ToList());

    // ------------------------------------------------------------------------------------------------------
    // Expansion and normalization
    // ------------------------------------------------------------------------------------------------------

    public string Expand(EnvSnapshot snapshot, string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('%'))
        {
            return value ?? string.Empty;
        }

        var lookup = BuildLookup(snapshot);
        var result = value;
        for (var pass = 0; pass < 5 && result.Contains('%'); pass++)
        {
            var next = VariableReference().Replace(result, m =>
                lookup.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
            if (next == result)
            {
                break;
            }

            result = next;
        }

        return result;
    }

    private static Dictionary<string, string> BuildLookup(EnvSnapshot snapshot)
    {
        // Process variables supply profile/volatile ones (USERPROFILE, SystemRoot, LOCALAPPDATA, ...) that are not
        // stored in either registry key; Machine values then User values override, as Windows does at logon.
        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
        {
            if (e.Key is string k && e.Value is string v)
            {
                lookup[k] = v;
            }
        }

        foreach (var v in snapshot.Variables.Where(v => v.Scope == EnvScope.Machine))
        {
            lookup[v.Name] = v.RawValue;
        }

        foreach (var v in snapshot.Variables.Where(v => v.Scope == EnvScope.User && !v.Name.Equals("Path", StringComparison.OrdinalIgnoreCase)))
        {
            lookup[v.Name] = v.RawValue;
        }

        return lookup;
    }

    private string Normalize(EnvSnapshot snapshot, string raw)
    {
        var s = Unquote(raw.Trim());
        s = Expand(snapshot, s).Replace('/', '\\');
        try
        {
            if (Path.IsPathFullyQualified(s))
            {
                s = Path.GetFullPath(s);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
        }

        if (s.Length > 3)
        {
            s = s.TrimEnd('\\');
        }

        return s.ToUpperInvariant();
    }

    private static string Unquote(string s) =>
        s.Length >= 2 && s[0] == '"' && s[^1] == '"' ? s[1..^1].Trim() : s.Replace("\"", string.Empty);

    private static List<string> SplitList(string? value) =>
        string.IsNullOrEmpty(value) ? new List<string>() : value.Split(';').ToList();

    [GeneratedRegex("%([^%;]+)%")]
    private static partial Regex VariableReference();

    // ------------------------------------------------------------------------------------------------------
    // Diagnosis
    // ------------------------------------------------------------------------------------------------------

    public Task<EnvDiagnosis> DiagnoseAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => Diagnose(TakeSnapshot(), cancellationToken), cancellationToken);

    /// <summary>Runs every check against a snapshot. Public for tests.</summary>
    public EnvDiagnosis Diagnose(EnvSnapshot snap, CancellationToken ct = default)
    {
        var ctx = new ScanContext(snap);
        var pathEntries = new List<EnvPathEntry>();

        foreach (var name in DirectoryListVariables)
        {
            foreach (var scope in new[] { EnvScope.Machine, EnvScope.User })
            {
                ct.ThrowIfCancellationRequested();
                CheckDirectoryList(ctx, scope, name, name.Equals("Path", StringComparison.OrdinalIgnoreCase) ? pathEntries : null);
            }
        }

        CheckEssentialMachinePath(ctx);
        ct.ThrowIfCancellationRequested();
        CheckPathExt(ctx);
        CheckCoreVariables(ctx);
        CheckTemp(ctx);
        CheckGenericVariables(ctx);
        ct.ThrowIfCancellationRequested();

        var commands = ResolveCommands(snap, Watched.Concat(SystemCommands).Distinct(StringComparer.OrdinalIgnoreCase));
        CheckCommands(ctx, commands);
        CheckHomeVariables(ctx, commands);
        CheckSecurityAndPitfalls(ctx, commands);

        var effective = EffectivePathEntries(snap).Select(e => e.Expanded);
        var effectiveLength = string.Join(';', effective).Length;
        if (effectiveLength > LegacyPathLength)
        {
            ctx.Add(EnvIssueCode.PathTooLongForLegacyTools, EnvSeverity.Info, null, "Path",
                $"Combined PATH is {effectiveLength:N0} characters",
                "Some older installers and the classic Environment Variables dialog truncate PATH above 2,047 characters, " +
                "and 'setx' truncates at 1,024. Removing missing and duplicate entries is the safest way to shorten it. " +
                "Never use 'setx PATH' to edit PATH.");
        }

        var watchedResults = commands.Where(c => Watched.Contains(c.Command, StringComparer.OrdinalIgnoreCase) && c.AllMatches.Count > 0).ToList();
        return new EnvDiagnosis(snap, ctx.Issues, pathEntries, watchedResults, effectiveLength);
    }

    private sealed class ScanContext
    {
        private int _next;

        public ScanContext(EnvSnapshot snapshot) => Snapshot = snapshot;

        public EnvSnapshot Snapshot { get; }

        public List<EnvIssue> Issues { get; } = new();

        public void Add(EnvIssueCode code, EnvSeverity severity, EnvScope? scope, string variable, string title, string detail,
            string? entry = null, EnvFix? fix = null)
        {
            Issues.Add(new EnvIssue($"E{++_next:D4}", code, severity, scope, variable, title, detail, entry, fix));
        }
    }

    private void CheckDirectoryList(ScanContext ctx, EnvScope scope, string name, List<EnvPathEntry>? display)
    {
        var snap = ctx.Snapshot;
        var variable = snap.Get(scope, name);
        if (variable == null)
        {
            return;
        }

        var entries = SplitList(variable.RawValue);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        var otherScope = scope == EnvScope.User ? EnvScope.Machine : EnvScope.User;
        var otherSet = new HashSet<string>(
            SplitList(snap.Get(EnvScope.Machine, name)?.RawValue).Where(e => e.Trim().Length > 0).Select(e => Normalize(snap, e)),
            StringComparer.Ordinal);
        var pathExt = EffectivePathExt(snap);
        var isPath = name.Equals("Path", StringComparison.OrdinalIgnoreCase);
        var userProfile = Normalize(snap, "%USERPROFILE%");
        var emptyCount = 0;

        for (var i = 0; i < entries.Count; i++)
        {
            var raw = entries[i];
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                // A trailing ';' is harmless; empty entries in the middle (';;') can make some tools search the
                // current directory, so they are worth cleaning.
                if (i != entries.Count - 1)
                {
                    emptyCount++;
                }

                continue;
            }

            var expanded = Expand(snap, Unquote(trimmed));
            var normalized = Normalize(snap, raw);
            var exists = false;
            var executables = 0;
            var status = "OK";
            var isDuplicate = false;

            if (raw != trimmed)
            {
                ctx.Add(EnvIssueCode.PathEntryWhitespace, EnvSeverity.Warning, scope, name,
                    $"{name} entry has leading or trailing spaces: {trimmed}", $"'{raw}' — Windows treats the spaces as part of the folder name.", raw,
                    new EnvFix(EnvFixAction.ReplaceListEntry, scope, name, raw, Unquote(trimmed), Description: "Trim the spaces", Index: i));
            }
            else if (trimmed.Contains('"'))
            {
                ctx.Add(EnvIssueCode.PathEntryQuoted, EnvSeverity.Warning, scope, name,
                    $"{name} entry is wrapped in quotes: {trimmed}", $"'{raw}' — quotes are not needed in PATH (even with spaces) and break many tools.", raw,
                    new EnvFix(EnvFixAction.ReplaceListEntry, scope, name, raw, Unquote(trimmed), Description: "Remove the quotes", Index: i));
            }

            var undefined = VariableReference().Matches(expanded).Select(m => m.Value).Distinct().ToList();
            if (undefined.Count > 0)
            {
                status = "Undefined variable";
                ctx.Add(EnvIssueCode.PathEntryUndefinedVariable, EnvSeverity.Warning, scope, name,
                    $"{name} entry uses an undefined variable: {trimmed}", $"'{raw}' refers to {string.Join(", ", undefined)}, which is not defined, so this entry never matches a folder.", raw,
                    new EnvFix(EnvFixAction.RemoveListEntry, scope, name, raw, Description: "Remove the entry", Index: i));
            }
            else if (expanded.StartsWith(@"\\", StringComparison.Ordinal))
            {
                status = "Network path (not checked)";
                exists = true;
            }
            else if (!Path.IsPathFullyQualified(expanded))
            {
                status = "Relative path";
                ctx.Add(EnvIssueCode.PathEntryRelative, EnvSeverity.Warning, scope, name,
                    $"{name} entry is a relative path: {trimmed}", $"'{raw}' depends on the current folder, so programs can run from wherever a command is started — a security risk.", raw,
                    new EnvFix(EnvFixAction.RemoveListEntry, scope, name, raw, Description: "Remove the entry", Index: i));
            }
            else if (File.Exists(expanded))
            {
                status = "Is a file";
                ctx.Add(EnvIssueCode.PathEntryIsFile, EnvSeverity.Warning, scope, name,
                    $"{name} entry is a file, not a folder: {trimmed}", $"'{raw}' — PATH entries must be folders; Windows ignores this one.", raw,
                    new EnvFix(EnvFixAction.RemoveListEntry, scope, name, raw, Description: "Remove the entry", Index: i));
            }
            else if (!Directory.Exists(expanded))
            {
                status = "Missing";
                ctx.Add(EnvIssueCode.PathEntryMissing, EnvSeverity.Warning, scope, name,
                    $"{name} folder does not exist: {trimmed}", $"'{raw}' → {expanded}. Usually left behind by an uninstalled program; it slows every command lookup.", raw,
                    new EnvFix(EnvFixAction.RemoveListEntry, scope, name, raw, Description: "Remove the missing folder", Index: i));
            }
            else
            {
                exists = true;
                if (isPath)
                {
                    executables = CountExecutables(expanded, pathExt);
                    if (executables == 0)
                    {
                        status = "No programs";
                        ctx.Add(EnvIssueCode.PathEntryNoExecutables, EnvSeverity.Info, scope, name,
                            $"PATH folder contains no programs: {trimmed}", $"'{raw}' has no files with a PATHEXT extension. It may still be needed for DLLs — remove only if you know it is unused.", raw,
                            new EnvFix(EnvFixAction.RemoveListEntry, scope, name, raw, Description: "Remove the entry", Index: i));
                    }
                }

                if (isPath && scope == EnvScope.Machine && userProfile.Length > 3 && normalized.StartsWith(userProfile + "\\", StringComparison.Ordinal))
                {
                    ctx.Add(EnvIssueCode.PathEntryUserWritableInMachinePath, EnvSeverity.Warning, scope, name,
                        "System PATH contains a folder inside a user profile",
                        $"'{raw}' is writable by that user but used by every account and by services, which allows privilege escalation. Prefer the User PATH for it.", raw);
                }
            }

            if (seen.TryGetValue(normalized, out var firstIndex))
            {
                isDuplicate = true;
                status = "Duplicate";
                ctx.Add(EnvIssueCode.PathEntryDuplicate, EnvSeverity.Warning, scope, name,
                    $"Duplicate {name} entry: {trimmed}", $"'{raw}' (entry {i + 1}) repeats entry {firstIndex + 1}. Only the first one is ever used.", raw,
                    new EnvFix(EnvFixAction.RemoveListEntry, scope, name, raw, Description: "Remove the later copy", Index: i));
            }
            else
            {
                seen[normalized] = i;
                if (scope == EnvScope.User && otherSet.Contains(normalized))
                {
                    isDuplicate = true;
                    status = "Also in System PATH";
                    ctx.Add(EnvIssueCode.PathEntryDuplicateAcrossScopes, EnvSeverity.Info, scope, name,
                        $"User {name} repeats a System {name} entry: {trimmed}", $"'{raw}' is already in the System {name}, which is searched first.", raw,
                        new EnvFix(EnvFixAction.RemoveListEntry, scope, name, raw, Description: "Remove from the User list", Index: i));
                }
            }

            display?.Add(new EnvPathEntry(scope, i, raw, expanded, exists, isDuplicate, executables, status));
        }

        if (emptyCount > 0)
        {
            ctx.Add(EnvIssueCode.PathEntryEmpty, EnvSeverity.Info, scope, name,
                $"{name} has {emptyCount} empty entr{(emptyCount == 1 ? "y" : "ies")}", "Consecutive semicolons (';;') create empty entries that some tools treat as the current folder.", string.Empty,
                new EnvFix(EnvFixAction.RemoveListEntry, scope, name, string.Empty, Description: "Remove empty entries"));
        }

        _ = otherScope;
    }

    private void CheckEssentialMachinePath(ScanContext ctx)
    {
        var snap = ctx.Snapshot;
        var machine = snap.Get(EnvScope.Machine, "Path");
        var entries = SplitList(machine?.RawValue);
        var normalized = entries.Select(e => e.Trim().Length == 0 ? string.Empty : Normalize(snap, e)).ToList();

        for (var k = 0; k < EssentialMachinePath.Length; k++)
        {
            var (entry, severity) = EssentialMachinePath[k];
            var expanded = Expand(snap, entry);
            if (!Directory.Exists(expanded))
            {
                continue; // e.g. OpenSSH not installed
            }

            if (!normalized.Contains(Normalize(snap, entry)))
            {
                ctx.Add(EnvIssueCode.EssentialSystemPathMissing, severity, EnvScope.Machine, "Path",
                    $"System PATH is missing {entry}",
                    k < 3
                        ? "Windows commands (cmd tools, wmic, PowerShell scripts, installers) fail to start without it."
                        : "Tools that expect this standard Windows folder on PATH will not find their commands.",
                    entry,
                    new EnvFix(EnvFixAction.InsertListEntry, EnvScope.Machine, "Path", entry, InsertIndex: k, NewKind: EnvValueKind.ExpandString,
                        Description: $"Add {entry} at position {k + 1}"));
            }
        }
    }

    private void CheckPathExt(ScanContext ctx)
    {
        foreach (var scope in new[] { EnvScope.Machine, EnvScope.User })
        {
            var v = ctx.Snapshot.Get(scope, "PATHEXT");
            if (v == null)
            {
                if (scope == EnvScope.Machine)
                {
                    ctx.Add(EnvIssueCode.CoreVariableMissing, EnvSeverity.Error, scope, "PATHEXT",
                        "PATHEXT is missing", "Without it, typing a command without '.exe' fails everywhere.", null,
                        new EnvFix(EnvFixAction.SetValue, scope, "PATHEXT", NewValue: DefaultPathExt, NewKind: EnvValueKind.String, Description: "Restore the Windows default"));
                }

                continue;
            }

            var parts = SplitList(v.RawValue).Select(p => p.Trim().ToUpperInvariant()).Where(p => p.Length > 0).ToList();
            var cleaned = parts.Distinct().ToList();
            var missing = StandardPathExt.Where(e => !cleaned.Contains(e)).ToList();
            var malformed = cleaned.Where(e => !e.StartsWith('.')).ToList();
            if (missing.Count > 0 || malformed.Count > 0 || cleaned.Count != parts.Count || string.Join(';', cleaned) != v.RawValue.Trim().ToUpperInvariant())
            {
                var fixedValue = string.Join(';', missing.Concat(cleaned.Where(e => e.StartsWith('.'))).Distinct());
                var severity = missing.Count > 0 ? EnvSeverity.Error : EnvSeverity.Warning;
                var detail = missing.Count > 0
                    ? $"Missing {string.Join(", ", missing)} — commands with these extensions will not run without typing the extension."
                    : "Contains duplicates, empty items, spaces or items that do not start with '.'.";
                if (scope == EnvScope.User)
                {
                    detail += " A User PATHEXT replaces the System one completely for this user.";
                }

                ctx.Add(missing.Count > 0 ? EnvIssueCode.PathExtMissingStandardExtensions : EnvIssueCode.CoreVariableInvalid, severity, scope, "PATHEXT",
                    $"{scope} PATHEXT needs repair", detail, null,
                    new EnvFix(EnvFixAction.SetValue, scope, "PATHEXT", NewValue: fixedValue, NewKind: EnvValueKind.String, Description: $"Set to {fixedValue}"));
            }
        }
    }

    private void CheckCoreVariables(ScanContext ctx)
    {
        var snap = ctx.Snapshot;
        void Require(string name, string expected, Func<string, bool> valid, string why)
        {
            var v = snap.Get(EnvScope.Machine, name);
            if (v == null)
            {
                ctx.Add(EnvIssueCode.CoreVariableMissing, EnvSeverity.Error, EnvScope.Machine, name, $"{name} is missing", why, null,
                    new EnvFix(EnvFixAction.SetValue, EnvScope.Machine, name, NewValue: expected, NewKind: expected.Contains('%') ? EnvValueKind.ExpandString : EnvValueKind.String,
                        Description: $"Set to {expected}"));
            }
            else if (!valid(Expand(snap, v.RawValue)))
            {
                ctx.Add(EnvIssueCode.CoreVariableInvalid, EnvSeverity.Error, EnvScope.Machine, name, $"{name} is wrong", $"'{Show(v)}' — {why}", null,
                    new EnvFix(EnvFixAction.SetValue, EnvScope.Machine, name, NewValue: expected, NewKind: expected.Contains('%') ? EnvValueKind.ExpandString : EnvValueKind.String,
                        Description: $"Set to {expected}"));
            }
        }

        Require("ComSpec", @"%SystemRoot%\system32\cmd.exe", File.Exists, "batch files, installers and 'cmd /c' calls need it to find cmd.exe.");
        Require("windir", "%SystemRoot%", Directory.Exists, "older programs use it to find the Windows folder.");
        Require("OS", "Windows_NT", v => v == "Windows_NT", "scripts use it to detect Windows.");

        var psmp = snap.Get(EnvScope.Machine, "PSModulePath");
        const string corePsModules = @"%SystemRoot%\system32\WindowsPowerShell\v1.0\Modules";
        if (psmp != null && !SplitList(psmp.RawValue).Where(e => e.Trim().Length > 0).Select(e => Normalize(snap, e)).Contains(Normalize(snap, corePsModules)))
        {
            ctx.Add(EnvIssueCode.CoreVariableInvalid, EnvSeverity.Error, EnvScope.Machine, "PSModulePath",
                "PSModulePath is missing the built-in PowerShell modules", "Windows PowerShell cannot auto-load its own modules (Get-NetAdapter, etc.).", corePsModules,
                new EnvFix(EnvFixAction.InsertListEntry, EnvScope.Machine, "PSModulePath", corePsModules, InsertIndex: -1, NewKind: EnvValueKind.ExpandString, Description: "Add the built-in modules folder"));
        }
    }

    private void CheckTemp(ScanContext ctx)
    {
        var snap = ctx.Snapshot;
        foreach (var (scope, expected) in new[] { (EnvScope.User, @"%USERPROFILE%\AppData\Local\Temp"), (EnvScope.Machine, @"%SystemRoot%\TEMP") })
        {
            foreach (var name in new[] { "TEMP", "TMP" })
            {
                var v = snap.Get(scope, name);
                if (v == null)
                {
                    ctx.Add(EnvIssueCode.TempDirectoryInvalid, scope == EnvScope.User ? EnvSeverity.Warning : EnvSeverity.Info, scope, name,
                        $"{scope} {name} is not set", "Programs fall back to other locations or fail to create temporary files.", null,
                        new EnvFix(EnvFixAction.SetValue, scope, name, NewValue: expected, NewKind: EnvValueKind.ExpandString, Description: $"Set to {expected}"));
                    continue;
                }

                var dir = Expand(snap, v.RawValue);
                if (!Directory.Exists(dir))
                {
                    ctx.Add(EnvIssueCode.TempDirectoryInvalid, EnvSeverity.Error, scope, name,
                        $"{scope} {name} folder does not exist", $"'{Show(v)}' → {dir}. Installers and many programs fail when they cannot write temporary files.", null,
                        new EnvFix(EnvFixAction.SetValue, scope, name, NewValue: expected, NewKind: EnvValueKind.ExpandString, Description: $"Set to {expected}"));
                }
                else if (scope == EnvScope.User && !IsWritable(dir))
                {
                    ctx.Add(EnvIssueCode.TempDirectoryInvalid, EnvSeverity.Error, scope, name,
                        $"{name} folder is not writable", $"{dir} cannot be written to by this account.", null,
                        new EnvFix(EnvFixAction.SetValue, scope, name, NewValue: expected, NewKind: EnvValueKind.ExpandString, Description: $"Set to {expected}"));
                }
            }
        }
    }

    private void CheckGenericVariables(ScanContext ctx)
    {
        var snap = ctx.Snapshot;
        foreach (var v in snap.Variables)
        {
            var isList = DirectoryListVariables.Contains(v.Name, StringComparer.OrdinalIgnoreCase);

            if (v.RawValue.Length > MaxVariableLength)
            {
                ctx.Add(EnvIssueCode.VariableTooLong, EnvSeverity.Error, v.Scope, v.Name,
                    $"{v.Name} is longer than Windows allows", $"{v.RawValue.Length:N0} characters; the limit is {MaxVariableLength:N0}. Programs may get a truncated or empty value.");
            }

            if (v.Kind == EnvValueKind.String && VariableReference().IsMatch(v.RawValue))
            {
                var severity = isList ? EnvSeverity.Error : EnvSeverity.Warning;
                ctx.Add(EnvIssueCode.ExpandableValueStoredAsString, severity, v.Scope, v.Name,
                    $"{v.Name} contains %VARIABLES% but is stored as REG_SZ",
                    "Windows only expands %VARIABLES% in REG_EXPAND_SZ values, so entries like %SystemRoot%\\system32 are passed literally and never match. " +
                    "This is a classic cause of 'command not found' after a tool rewrites PATH.",
                    null,
                    new EnvFix(EnvFixAction.ChangeKind, v.Scope, v.Name, NewKind: EnvValueKind.ExpandString, Description: "Store as REG_EXPAND_SZ"));
            }

            if (v.Name != v.Name.Trim())
            {
                ctx.Add(EnvIssueCode.VariableNameOrValueWhitespace, EnvSeverity.Warning, v.Scope, v.Name,
                    $"Variable name '{v.Name}' has spaces around it", "Programs looking up the name without spaces will not find it. Recreate it with the correct name.");
            }
            else if (!isList && v.RawValue.Length > 0 && v.RawValue != v.RawValue.Trim())
            {
                ctx.Add(EnvIssueCode.VariableNameOrValueWhitespace, EnvSeverity.Warning, v.Scope, v.Name,
                    $"{v.Name} value has leading or trailing spaces", $"'{Show(v)}' — the spaces become part of paths and arguments.", null,
                    new EnvFix(EnvFixAction.SetValue, v.Scope, v.Name, NewValue: v.RawValue.Trim(), NewKind: v.Kind, Description: "Trim the value"));
            }

            if (v.RawValue.Trim().Length == 0)
            {
                ctx.Add(EnvIssueCode.VariableEmpty, EnvSeverity.Info, v.Scope, v.Name, $"{v.Name} is empty",
                    "An empty variable is usually left over from an uninstall and can make tools think a setting is present.", null,
                    ProtectedFromDelete.Contains(v.Name, StringComparer.OrdinalIgnoreCase) ? null
                        : new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: "Delete the variable"));
                continue;
            }

            if (v.Scope == EnvScope.User && !isList && !v.Name.Equals("TEMP", StringComparison.OrdinalIgnoreCase) && !v.Name.Equals("TMP", StringComparison.OrdinalIgnoreCase)
                && !ImportantOverrides.Contains(v.Name) && snap.Get(EnvScope.Machine, v.Name) is { } machine && machine.RawValue != v.RawValue)
            {
                ctx.Add(EnvIssueCode.VariableOverriddenByUser, EnvSeverity.Info, v.Scope, v.Name,
                    $"User {v.Name} overrides the System value", $"This user gets '{Show(v)}' instead of '{Show(machine)}'. Intended in most cases; listed so conflicts are visible.");
            }

            // Single-path values that point nowhere (not TEMP/TMP or home variables, which have their own checks).
            if (!isList && !HomeVariables.ContainsKey(v.Name) && !v.Name.Equals("TEMP", StringComparison.OrdinalIgnoreCase)
                && !v.Name.Equals("TMP", StringComparison.OrdinalIgnoreCase) && !v.RawValue.Contains(';') && LooksLikeLocalPath(v.RawValue))
            {
                var expanded = Expand(snap, Unquote(v.RawValue.Trim()));
                if (!VariableReference().IsMatch(expanded) && !File.Exists(expanded) && !Directory.Exists(expanded))
                {
                    ctx.Add(EnvIssueCode.VariablePathValueMissing, EnvSeverity.Warning, v.Scope, v.Name,
                        $"{v.Name} points to a path that does not exist", $"'{Show(v)}' → {expanded}. Probably left behind by an uninstalled program.", null,
                        ProtectedFromDelete.Contains(v.Name, StringComparer.OrdinalIgnoreCase) ? null
                            : new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: "Delete the variable"));
                }
            }
        }
    }

    private static bool LooksLikeLocalPath(string value)
    {
        var v = value.Trim().Trim('"');
        return (v.Length >= 3 && char.IsLetter(v[0]) && v[1] == ':' && (v[2] == '\\' || v[2] == '/'))
            || (v.StartsWith('%') && v.Contains("%\\"));
    }

    private void CheckHomeVariables(ScanContext ctx, IReadOnlyList<EnvCommandResolution> commands)
    {
        var snap = ctx.Snapshot;
        foreach (var v in snap.Variables)
        {
            if (!HomeVariables.TryGetValue(v.Name, out var spec) || v.RawValue.Trim().Length == 0)
            {
                continue;
            }

            if (v.Name.Equals("PYTHONHOME", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Add(EnvIssueCode.GlobalPythonHomeOrPath, EnvSeverity.Warning, v.Scope, v.Name,
                    "PYTHONHOME is set for all programs",
                    $"'{Show(v)}'. A global PYTHONHOME makes every other Python (virtual environments, tools that bundle Python) load the wrong standard library and fail with 'No module named encodings'.",
                    null, new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: "Delete PYTHONHOME"));
                continue;
            }

            var dir = Expand(snap, Unquote(v.RawValue.Trim())).TrimEnd('\\');
            var candidate = FindHomeCandidate(v.Name, spec, commands);
            EnvFix? replace = candidate != null && !string.Equals(candidate, dir, StringComparison.OrdinalIgnoreCase)
                ? new EnvFix(EnvFixAction.SetValue, v.Scope, v.Name, NewValue: candidate, NewKind: EnvValueKind.String, Description: $"Point to {candidate}")
                : null;

            if (!Directory.Exists(dir))
            {
                ctx.Add(EnvIssueCode.HomeVariableMissingDirectory, EnvSeverity.Error, v.Scope, v.Name,
                    $"{v.Name} points to a folder that does not exist",
                    $"'{Show(v)}' → {dir}. Build tools that read {v.Name} will fail." + (candidate != null ? $" Found an installation at {candidate}." : " No other installation was found."),
                    null, replace ?? new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: $"Delete {v.Name}"));
                continue;
            }

            if (spec.Marker.Length == 0)
            {
                continue;
            }

            var markerOk = File.Exists(Path.Combine(dir, spec.Marker)) || Directory.Exists(Path.Combine(dir, spec.Marker));
            if (!markerOk && Path.GetFileName(dir).Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                var parent = Path.GetDirectoryName(dir);
                if (parent != null && (File.Exists(Path.Combine(parent, spec.Marker)) || Directory.Exists(Path.Combine(parent, spec.Marker))))
                {
                    ctx.Add(EnvIssueCode.HomeVariablePointsToBin, EnvSeverity.Error, v.Scope, v.Name,
                        $"{v.Name} points to the bin folder", $"'{Show(v)}'. {v.Name} must be the installation root ({parent}); tools append '\\bin' themselves.",
                        null, new EnvFix(EnvFixAction.SetValue, v.Scope, v.Name, NewValue: parent, NewKind: EnvValueKind.String, Description: $"Point to {parent}"));
                    continue;
                }
            }

            if (!markerOk)
            {
                ctx.Add(EnvIssueCode.HomeVariableInvalidLayout, EnvSeverity.Warning, v.Scope, v.Name,
                    $"{v.Name} does not look like a valid installation", $"{dir} has no {spec.Marker}." + (candidate != null ? $" Found one at {candidate}." : string.Empty),
                    null, replace);
                continue;
            }

            // Home variable and the command on PATH should be the same installation.
            if (spec.Command != null && commands.FirstOrDefault(c => c.Command.Equals(spec.Command, StringComparison.OrdinalIgnoreCase)) is { Winner: { } winner }
                && !winner.StartsWith(dir + "\\", StringComparison.OrdinalIgnoreCase)
                && !winner.Contains(@"\Oracle\Java\javapath", StringComparison.OrdinalIgnoreCase)
                && !winner.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Add(EnvIssueCode.HomeVariableMismatchWithPath, EnvSeverity.Warning, v.Scope, v.Name,
                    $"{v.Name} and '{spec.Command}' on PATH are different installations",
                    $"{v.Name} = {dir}, but '{spec.Command}' runs {winner}. Build tools (Gradle, Maven, IDEs) and the command line will use different versions.",
                    null);
            }
        }

        foreach (var v in snap.Variables.Where(v => v.Name.Equals("PYTHONPATH", StringComparison.OrdinalIgnoreCase) && v.RawValue.Trim().Length > 0))
        {
            ctx.Add(EnvIssueCode.GlobalPythonHomeOrPath, EnvSeverity.Warning, v.Scope, v.Name,
                "PYTHONPATH is set for all programs",
                $"'{Show(v)}'. Every Python, including virtual environments and other versions, imports from these folders first — a common cause of version conflicts. Prefer per-project settings.",
                null, new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: "Delete PYTHONPATH"));
        }
    }

    private static string? FindHomeCandidate(string name, (string Marker, string? Command, int LevelsUp) spec, IReadOnlyList<EnvCommandResolution> commands)
    {
        bool Valid(string d) => spec.Marker.Length == 0 || File.Exists(Path.Combine(d, spec.Marker)) || Directory.Exists(Path.Combine(d, spec.Marker));

        if (spec.Command != null && commands.FirstOrDefault(c => c.Command.Equals(spec.Command, StringComparison.OrdinalIgnoreCase)) is { } res)
        {
            foreach (var match in res.AllMatches)
            {
                if (match.Contains(@"\Oracle\Java\javapath", StringComparison.OrdinalIgnoreCase) || match.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var d = match;
                for (var i = 0; i < spec.LevelsUp && d != null; i++)
                {
                    d = Path.GetDirectoryName(d);
                }

                if (d != null && Valid(d))
                {
                    return d;
                }
            }
        }

        if (name.Contains("JAVA", StringComparison.OrdinalIgnoreCase) || name.Equals("JDK_HOME", StringComparison.OrdinalIgnoreCase))
        {
            var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var roots = new[] { "Java", "Eclipse Adoptium", @"Microsoft", "Zulu", "Amazon Corretto", "BellSoft", "Semeru" }
                .Select(r => Path.Combine(pf, r)).Where(Directory.Exists);
            var found = roots.SelectMany(r => SafeDirectories(r)).Where(Valid).OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if (found != null)
            {
                return found;
            }
        }

        if (name.Equals("GOROOT", StringComparison.OrdinalIgnoreCase))
        {
            var go = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Go");
            if (Valid(go))
            {
                return go;
            }
        }

        return null;
    }

    private static IEnumerable<string> SafeDirectories(string root)
    {
        try
        {
            return Directory.EnumerateDirectories(root).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    // ------------------------------------------------------------------------------------------------------
    // Command resolution
    // ------------------------------------------------------------------------------------------------------

    private sealed record EffectiveEntry(EnvScope Scope, int Index, string Raw, string Expanded);

    private List<EffectiveEntry> EffectivePathEntries(EnvSnapshot snap)
    {
        // Windows builds a process PATH as System PATH followed by User PATH.
        var list = new List<EffectiveEntry>();
        foreach (var scope in new[] { EnvScope.Machine, EnvScope.User })
        {
            var entries = SplitList(snap.Get(scope, "Path")?.RawValue);
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].Trim().Length > 0)
                {
                    list.Add(new EffectiveEntry(scope, i, entries[i], Expand(snap, Unquote(entries[i].Trim()))));
                }
            }
        }

        return list;
    }

    private IReadOnlyList<string> EffectivePathExt(EnvSnapshot snap)
    {
        var raw = snap.Get(EnvScope.User, "PATHEXT")?.RawValue ?? snap.Get(EnvScope.Machine, "PATHEXT")?.RawValue ?? DefaultPathExt;
        var list = SplitList(raw).Select(e => e.Trim()).Where(e => e.StartsWith('.')).ToList();
        return list.Count > 0 ? list : SplitList(DefaultPathExt);
    }

    private List<EnvCommandResolution> ResolveCommands(EnvSnapshot snap, IEnumerable<string> names)
    {
        var dirs = EffectivePathEntries(snap)
            .Where(e => Path.IsPathFullyQualified(e.Expanded) && !e.Expanded.StartsWith(@"\\", StringComparison.Ordinal) && Directory.Exists(e.Expanded))
            .Select(e => e.Expanded.TrimEnd('\\'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var exts = EffectivePathExt(snap);
        var result = new List<EnvCommandResolution>();

        foreach (var name in names)
        {
            var matches = new List<string>();
            foreach (var dir in dirs)
            {
                foreach (var ext in exts)
                {
                    var candidate = Path.Combine(dir, name + ext.ToLowerInvariant());
                    if (File.Exists(candidate) && !matches.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                    {
                        matches.Add(candidate);
                    }
                }
            }

            var versions = matches.Select(GetVersion).ToList();
            result.Add(new EnvCommandResolution(name, matches.FirstOrDefault(), matches, versions));
        }

        return result;
    }

    private static string GetVersion(string file)
    {
        try
        {
            if (!file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return "script";
            }

            var info = new FileInfo(file);
            if (info.Length == 0)
            {
                return "App Execution Alias";
            }

            var fvi = FileVersionInfo.GetVersionInfo(file);
            var v = fvi.ProductVersion ?? fvi.FileVersion ?? string.Empty;
            var plus = v.IndexOf('+');
            return plus > 0 ? v[..plus] : v;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return string.Empty;
        }
    }

    private void CheckCommands(ScanContext ctx, IReadOnlyList<EnvCommandResolution> commands)
    {
        var snap = ctx.Snapshot;
        var system32 = Expand(snap, @"%SystemRoot%\system32");
        var effective = EffectivePathEntries(snap);

        foreach (var cmd in commands)
        {
            if (cmd.Winner == null || cmd.AllMatches.Count < 2)
            {
                continue;
            }

            var isSystemCommand = SystemCommands.Contains(cmd.Command, StringComparer.OrdinalIgnoreCase);
            var systemCopy = cmd.AllMatches.FirstOrDefault(m => string.Equals(Path.GetDirectoryName(m), system32, StringComparison.OrdinalIgnoreCase));
            if (isSystemCommand && systemCopy != null && !string.Equals(cmd.Winner, systemCopy, StringComparison.OrdinalIgnoreCase))
            {
                var winnerEntry = effective.FirstOrDefault(e => string.Equals(e.Expanded.TrimEnd('\\'), Path.GetDirectoryName(cmd.Winner), StringComparison.OrdinalIgnoreCase));
                var sysEntry = effective.FirstOrDefault(e => e.Scope == EnvScope.Machine && string.Equals(e.Expanded.TrimEnd('\\'), system32, StringComparison.OrdinalIgnoreCase));
                EnvFix? fix = winnerEntry?.Scope == EnvScope.Machine && sysEntry != null
                    ? new EnvFix(EnvFixAction.MoveListEntry, EnvScope.Machine, "Path", sysEntry.Raw, InsertIndex: 0, Description: "Move system32 to the front of the System PATH", Index: sysEntry.Index)
                    : null;
                ctx.Add(EnvIssueCode.SystemCommandShadowed, EnvSeverity.Warning, EnvScope.Machine, "Path",
                    $"'{cmd.Command}' runs {cmd.Winner} instead of the Windows command",
                    $"Scripts and installers that call '{cmd.Command}' expect {systemCopy} and can behave differently or fail.", cmd.Winner, fix);
                continue;
            }

            if (isSystemCommand && !Watched.Contains(cmd.Command, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var winnerIsAlias = cmd.Winner.Contains(@"\Microsoft\WindowsApps\", StringComparison.OrdinalIgnoreCase) && cmd.Versions.FirstOrDefault() == "App Execution Alias";
            var real = cmd.AllMatches.Where((m, i) => cmd.Versions[i] != "App Execution Alias").ToList();
            if (winnerIsAlias && real.Count > 0)
            {
                var fixes = CreatePreferFixes(snap, effective, cmd, real[0]);
                ctx.Add(EnvIssueCode.WindowsAppsAliasShadowsRuntime, EnvSeverity.Warning, null, "Path",
                    $"'{cmd.Command}' opens the Microsoft Store instead of your installation",
                    $"The App Execution Alias in WindowsApps comes first on PATH, so '{cmd.Command}' starts the Store stub instead of {real[0]}. " +
                    "Either move your installation ahead of WindowsApps (fix) or turn off the alias in Settings → Apps → Advanced app settings → App execution aliases.",
                    cmd.Winner, fixes.Count == 1 ? fixes[0] : null);
                continue;
            }

            var distinctVersions = cmd.Versions.Where(v => v.Length > 0 && v != "script" && v != "App Execution Alias").Distinct().ToList();
            if (RuntimeCommands.Contains(cmd.Command) && distinctVersions.Count > 1)
            {
                var list = string.Join("; ", cmd.AllMatches.Select((m, i) => $"{m} ({(cmd.Versions[i].Length > 0 ? cmd.Versions[i] : "?")})"));
                ctx.Add(EnvIssueCode.MultipleRuntimeVersions, EnvSeverity.Warning, null, "Path",
                    $"{cmd.AllMatches.Count} different '{cmd.Command}' installations on PATH",
                    $"'{cmd.Command}' runs {cmd.Winner}. Others: {list}. Pick the one you want in Command conflicts.", cmd.Winner);
            }
            else
            {
                ctx.Add(EnvIssueCode.CommandShadowed, EnvSeverity.Info, null, "Path",
                    $"'{cmd.Command}' is found {cmd.AllMatches.Count} times on PATH",
                    $"Runs {cmd.Winner}; later copies are never used: {string.Join("; ", cmd.AllMatches.Skip(1))}.", cmd.Winner);
            }
        }
    }

    public IReadOnlyList<EnvFix> CreatePreferFixes(EnvDiagnosis diagnosis, string command, string matchPath)
    {
        var cmd = diagnosis.Commands.FirstOrDefault(c => c.Command.Equals(command, StringComparison.OrdinalIgnoreCase));
        return cmd == null ? Array.Empty<EnvFix>() : CreatePreferFixes(diagnosis.Snapshot, EffectivePathEntries(diagnosis.Snapshot), cmd, matchPath);
    }

    private static IReadOnlyList<EnvFix> CreatePreferFixes(EnvSnapshot snap, List<EffectiveEntry> effective, EnvCommandResolution cmd, string matchPath)
    {
        if (cmd.Winner == null || string.Equals(cmd.Winner, matchPath, StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<EnvFix>();
        }

        var targetDir = Path.GetDirectoryName(matchPath) ?? string.Empty;
        var winnerDir = Path.GetDirectoryName(cmd.Winner) ?? string.Empty;
        var target = effective.FirstOrDefault(e => string.Equals(e.Expanded.TrimEnd('\\'), targetDir, StringComparison.OrdinalIgnoreCase));
        var winner = effective.FirstOrDefault(e => string.Equals(e.Expanded.TrimEnd('\\'), winnerDir, StringComparison.OrdinalIgnoreCase));
        if (target == null || winner == null)
        {
            return Array.Empty<EnvFix>();
        }

        var description = $"Make '{cmd.Command}' run {matchPath}";
        if (target.Scope == winner.Scope)
        {
            return new[] { new EnvFix(EnvFixAction.MoveListEntry, target.Scope, "Path", target.Raw, InsertIndex: winner.Index, Description: description, Index: target.Index) };
        }

        if (target.Scope == EnvScope.Machine)
        {
            return Array.Empty<EnvFix>(); // machine already precedes user; winner must be earlier in machine — handled above
        }

        // Target is in the User PATH but the winner is in the System PATH, which always comes first: put the
        // target folder in the System PATH just before the winner and drop it from the User PATH.
        return new[]
        {
            new EnvFix(EnvFixAction.InsertListEntry, EnvScope.Machine, "Path", target.Raw, InsertIndex: winner.Index, Description: description + " (add to System PATH)"),
            new EnvFix(EnvFixAction.RemoveListEntry, EnvScope.User, "Path", target.Raw, Description: "Remove it from the User PATH", Index: target.Index),
        };
    }

    // ------------------------------------------------------------------------------------------------------
    // Plan
    // ------------------------------------------------------------------------------------------------------

    public EnvRepairPlan BuildPlan(EnvSnapshot snapshot, IEnumerable<EnvFix> fixes)
    {
        var changes = new List<EnvChange>();
        var refused = new List<string>();

        foreach (var group in fixes.GroupBy(f => (f.Scope, Name: f.Variable.ToUpperInvariant())))
        {
            var groupFixes = group.ToList();
            var name = groupFixes[0].Variable;
            var current = snapshot.Get(group.Key.Scope, name);
            name = current?.Name ?? name;
            var reasons = groupFixes.Select(f => f.Description).Where(d => d.Length > 0).Distinct().ToList();
            var label = $"{group.Key.Scope} {name}";

            if (groupFixes.Any(f => f.Action == EnvFixAction.DeleteVariable))
            {
                if (ProtectedFromDelete.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    refused.Add($"{label}: deleting this variable would break Windows; refused.");
                }
                else if (current != null)
                {
                    if (groupFixes.Count > 1)
                    {
                        refused.Add($"{label}: other changes were ignored because the variable is being deleted.");
                    }

                    changes.Add(new EnvChange(group.Key.Scope, name, current.RawValue, null, current.Kind, null, reasons));
                }

                continue;
            }

            string? value = current?.RawValue;
            EnvValueKind kind = current?.Kind ?? EnvValueKind.String;

            var sets = groupFixes.Where(f => f.Action == EnvFixAction.SetValue).Select(f => f.NewValue ?? string.Empty).Distinct().ToList();
            if (sets.Count > 1)
            {
                refused.Add($"{label}: conflicting new values were selected; nothing changed for it.");
                continue;
            }

            if (sets.Count == 1)
            {
                value = sets[0];
                var setFix = groupFixes.First(f => f.Action == EnvFixAction.SetValue);
                kind = setFix.NewKind ?? kind;
            }

            var listFixes = groupFixes.Where(f => f.Action is EnvFixAction.RemoveListEntry or EnvFixAction.ReplaceListEntry or EnvFixAction.InsertListEntry or EnvFixAction.MoveListEntry).ToList();
            if (listFixes.Count > 0)
            {
                if (sets.Count == 1)
                {
                    refused.Add($"{label}: list edits were ignored because a new value was set.");
                }
                else
                {
                    var listResult = ApplyListFixes(snapshot, value, listFixes, label, refused);
                    if (listResult == null)
                    {
                        continue;
                    }

                    value = listResult;
                    if (listFixes.Any(f => f.NewKind == EnvValueKind.ExpandString))
                    {
                        kind = EnvValueKind.ExpandString;
                    }
                }
            }

            var kindFix = groupFixes.LastOrDefault(f => f.Action == EnvFixAction.ChangeKind && f.NewKind.HasValue);
            if (kindFix != null)
            {
                kind = kindFix.NewKind!.Value;
            }

            if (value != null && VariableReference().IsMatch(value) && kind == EnvValueKind.String)
            {
                kind = EnvValueKind.ExpandString;
                reasons.Add("Stored as REG_EXPAND_SZ so %VARIABLES% are expanded");
            }

            if (value == null || (current != null && value == current.RawValue && kind == current.Kind))
            {
                continue;
            }

            // Safety checks.
            if (value.Length > MaxVariableLength)
            {
                refused.Add($"{label}: the result would exceed {MaxVariableLength:N0} characters; refused.");
                continue;
            }

            if (group.Key.Scope == EnvScope.Machine && name.Equals("Path", StringComparison.OrdinalIgnoreCase))
            {
                var after = SplitList(value).Where(e => e.Trim().Length > 0).Select(e => Normalize(snapshot, e)).ToList();
                var sys32 = Normalize(snapshot, @"%SystemRoot%\system32");
                var hadSys32 = SplitList(current?.RawValue).Where(e => e.Trim().Length > 0).Select(e => Normalize(snapshot, e)).Contains(sys32);
                if (after.Count == 0)
                {
                    refused.Add($"{label}: the System PATH would become empty; refused.");
                    continue;
                }

                if (hadSys32 && !after.Contains(sys32))
                {
                    refused.Add($"{label}: the change would remove system32 from the System PATH; refused.");
                    continue;
                }
            }

            changes.Add(new EnvChange(group.Key.Scope, name, current?.RawValue, value, current?.Kind, kind, reasons));
        }

        return new EnvRepairPlan(changes, refused, changes.Any(c => c.Scope == EnvScope.Machine));
    }

    private string? ApplyListFixes(EnvSnapshot snapshot, string? value, List<EnvFix> fixes, string label, List<string> refused)
    {
        var original = SplitList(value);
        var removed = new HashSet<int>();
        var replaced = new Dictionary<int, string>();
        var removeEmpty = false;

        foreach (var f in fixes.Where(f => f.Action is EnvFixAction.RemoveListEntry or EnvFixAction.ReplaceListEntry))
        {
            if (f.Action == EnvFixAction.RemoveListEntry && f.Index < 0 && string.IsNullOrEmpty(f.Entry))
            {
                removeEmpty = true;
                continue;
            }

            if (f.Index < 0 || f.Index >= original.Count || original[f.Index] != f.Entry)
            {
                refused.Add($"{label}: '{f.Entry}' changed since the scan; scan again. Nothing changed for it.");
                return null;
            }

            if (f.Action == EnvFixAction.RemoveListEntry)
            {
                removed.Add(f.Index);
            }
            else
            {
                replaced[f.Index] = f.NewValue ?? f.Entry!;
            }
        }

        // Build the working list keeping track of original indexes for moves.
        var working = new List<(int Orig, string Text)>();
        for (var i = 0; i < original.Count; i++)
        {
            if (removed.Contains(i))
            {
                continue;
            }

            var text = replaced.TryGetValue(i, out var r) ? r : original[i];
            if (text.Trim().Length == 0 && (removeEmpty || i == original.Count - 1))
            {
                continue; // empty entries removed on request; trailing ';' always normalized away
            }

            working.Add((i, text));
        }

        foreach (var f in fixes.Where(f => f.Action == EnvFixAction.MoveListEntry))
        {
            var pos = working.FindIndex(w => w.Orig == f.Index);
            if (pos < 0 || f.Index >= original.Count || original[f.Index] != f.Entry)
            {
                refused.Add($"{label}: '{f.Entry}' changed since the scan; scan again. Nothing changed for it.");
                return null;
            }

            var item = working[pos];
            working.RemoveAt(pos);
            // InsertIndex is an original index (the entry to move before); map it to the working list.
            var targetPos = f.InsertIndex <= 0 ? 0 : working.FindIndex(w => w.Orig >= f.InsertIndex);
            working.Insert(targetPos < 0 ? working.Count : targetPos, item);
        }

        var present = new HashSet<string>(working.Where(w => w.Text.Trim().Length > 0).Select(w => Normalize(snapshot, w.Text)), StringComparer.Ordinal);
        foreach (var f in fixes.Where(f => f.Action == EnvFixAction.InsertListEntry).OrderBy(f => f.InsertIndex < 0 ? int.MaxValue : f.InsertIndex))
        {
            if (string.IsNullOrWhiteSpace(f.Entry) || !present.Add(Normalize(snapshot, f.Entry)))
            {
                continue; // already present: inserting again would create a duplicate
            }

            var pos = f.InsertIndex < 0 ? working.Count : Math.Min(f.InsertIndex, working.Count);
            working.Insert(pos, (-1, f.Entry));
        }

        return string.Join(';', working.Select(w => w.Text));
    }

    // ------------------------------------------------------------------------------------------------------
    // Apply, verify, rollback
    // ------------------------------------------------------------------------------------------------------

    public async Task<EnvApplyResult> ApplyAsync(EnvRepairPlan plan, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        if (plan.Changes.Count == 0)
        {
            return new EnvApplyResult(true, null, 0, false, errors);
        }

        foreach (var scope in plan.Changes.Select(c => c.Scope).Distinct())
        {
            if (!_store.CanWrite(scope))
            {
                errors.Add(scope == EnvScope.Machine
                    ? "System variables can only be changed when Winhance runs as administrator."
                    : "The User environment key cannot be written.");
                return new EnvApplyResult(false, null, 0, false, errors);
            }
        }

        // Drift check: refuse if anything changed since the plan was built (another installer, another tool).
        var now = TakeSnapshot();
        foreach (var c in plan.Changes)
        {
            var cur = now.Get(c.Scope, c.Name);
            if (cur?.RawValue != c.OldValue || (cur != null && c.OldKind.HasValue && cur.Kind != c.OldKind))
            {
                errors.Add($"{c.Scope} {c.Name} was changed by another program since the scan. Scan again before repairing.");
            }
        }

        if (errors.Count > 0)
        {
            return new EnvApplyResult(false, null, 0, false, errors);
        }

        var backup = await CreateBackupAsync($"Before repair: {plan.Changes.Count} change(s)", cancellationToken).ConfigureAwait(false);
        var applied = new List<EnvChange>();
        try
        {
            foreach (var c in plan.Changes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (c.NewValue == null)
                {
                    _store.Delete(c.Scope, c.Name);
                }
                else
                {
                    _store.Write(c.Scope, c.Name, c.NewValue, c.NewKind ?? EnvValueKind.String);
                }

                applied.Add(c);
            }

            // Verify by reading back.
            var after = TakeSnapshot();
            foreach (var c in plan.Changes)
            {
                var v = after.Get(c.Scope, c.Name);
                var ok = c.NewValue == null ? v == null : v != null && v.RawValue == c.NewValue && v.Kind == (c.NewKind ?? EnvValueKind.String);
                if (!ok)
                {
                    errors.Add($"{c.Scope} {c.Name} did not read back as written.");
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException or OperationCanceledException or ArgumentException)
        {
            errors.Add(ex.Message);
        }

        if (errors.Count > 0)
        {
            var rollbackErrors = Rollback(applied);
            errors.AddRange(rollbackErrors);
            _store.BroadcastChange();
            _log.Log(LogLevel.Warning, $"Environment repair rolled back: {string.Join(" | ", errors)}");
            return new EnvApplyResult(false, backup, 0, true, errors);
        }

        _store.BroadcastChange();
        _log.Log(LogLevel.Info, $"Environment repair applied: {plan.Changes.Count} change(s); backup {backup.Id}");
        return new EnvApplyResult(true, backup, plan.Changes.Count, false, errors);
    }

    private List<string> Rollback(IEnumerable<EnvChange> applied)
    {
        var errors = new List<string>();
        foreach (var c in applied.Reverse())
        {
            try
            {
                if (c.OldValue == null)
                {
                    _store.Delete(c.Scope, c.Name);
                }
                else
                {
                    _store.Write(c.Scope, c.Name, c.OldValue, c.OldKind ?? EnvValueKind.String);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException or ArgumentException)
            {
                errors.Add($"Rollback of {c.Scope} {c.Name} failed: {ex.Message}. Restore it from the backup.");
            }
        }

        return errors;
    }

    // ------------------------------------------------------------------------------------------------------
    // Backups
    // ------------------------------------------------------------------------------------------------------

    private sealed record BackupFile(string Id, DateTime CreatedUtc, string Reason, List<EnvVariable> Variables);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<EnvBackup> CreateBackupAsync(string reason, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_backupRoot);
        var snap = TakeSnapshot();
        var id = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid().ToString("N")[..6]}";
        var json = Path.Combine(_backupRoot, id + ".json");
        var reg = Path.Combine(_backupRoot, id + ".reg");

        var file = new BackupFile(id, DateTime.UtcNow, reason, snap.Variables.ToList());
        await File.WriteAllTextAsync(json, JsonSerializer.Serialize(file, JsonOptions), cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(reg, ToRegFile(snap), Encoding.Unicode, cancellationToken).ConfigureAwait(false);

        foreach (var old in ListBackups().Skip(MaxBackups))
        {
            TryDelete(old.JsonPath);
            TryDelete(old.RegPath);
        }

        return new EnvBackup(id, file.CreatedUtc, reason, json, reg, file.Variables.Count);
    }

    public IReadOnlyList<EnvBackup> ListBackups()
    {
        if (!Directory.Exists(_backupRoot))
        {
            return Array.Empty<EnvBackup>();
        }

        var list = new List<EnvBackup>();
        foreach (var json in Directory.EnumerateFiles(_backupRoot, "*.json"))
        {
            try
            {
                var file = JsonSerializer.Deserialize<BackupFile>(File.ReadAllText(json));
                if (file != null)
                {
                    list.Add(new EnvBackup(file.Id, file.CreatedUtc, file.Reason, json, Path.ChangeExtension(json, ".reg"), file.Variables.Count));
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                _log.Log(LogLevel.Warning, $"Skipping unreadable environment backup {json}: {ex.Message}");
            }
        }

        return list.OrderByDescending(b => b.CreatedUtc).ToList();
    }

    public async Task<EnvApplyResult> RestoreBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var backup = ListBackups().FirstOrDefault(b => b.Id == backupId);
        if (backup == null)
        {
            return new EnvApplyResult(false, null, 0, false, new[] { $"Backup {backupId} was not found." });
        }

        var file = JsonSerializer.Deserialize<BackupFile>(await File.ReadAllTextAsync(backup.JsonPath, cancellationToken).ConfigureAwait(false));
        if (file == null || !file.Variables.Any(v => v.Scope == EnvScope.Machine && v.Name.Equals("Path", StringComparison.OrdinalIgnoreCase)))
        {
            return new EnvApplyResult(false, null, 0, false, new[] { "The backup is unreadable or has no System PATH; refusing to restore it." });
        }

        var current = TakeSnapshot();
        var changes = new List<EnvChange>();
        foreach (var target in file.Variables)
        {
            var cur = current.Get(target.Scope, target.Name);
            if (cur == null || cur.RawValue != target.RawValue || cur.Kind != target.Kind)
            {
                changes.Add(new EnvChange(target.Scope, target.Name, cur?.RawValue, target.RawValue, cur?.Kind, target.Kind, new[] { $"Restore from backup {backupId}" }));
            }
        }

        foreach (var cur in current.Variables)
        {
            if (!file.Variables.Any(t => t.Scope == cur.Scope && t.Name.Equals(cur.Name, StringComparison.OrdinalIgnoreCase)))
            {
                changes.Add(new EnvChange(cur.Scope, cur.Name, cur.RawValue, null, cur.Kind, null, new[] { $"Not in backup {backupId}" }));
            }
        }

        var plan = new EnvRepairPlan(changes, Array.Empty<string>(), changes.Any(c => c.Scope == EnvScope.Machine));
        return await ApplyAsync(plan, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Regedit-compatible export of both keys (REG_EXPAND_SZ as hex(2) so %VARIABLES% survive).</summary>
    public static string ToRegFile(EnvSnapshot snap)
    {
        var sb = new StringBuilder();
        sb.Append("Windows Registry Editor Version 5.00\r\n");
        foreach (var (scope, key) in new[]
        {
            (EnvScope.Machine, @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"),
            (EnvScope.User, @"HKEY_CURRENT_USER\Environment"),
        })
        {
            sb.Append("\r\n[").Append(key).Append("]\r\n");
            foreach (var v in snap.Variables.Where(v => v.Scope == scope).OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append('"').Append(EscapeReg(v.Name)).Append("\"=");
                if (v.Kind == EnvValueKind.String)
                {
                    sb.Append('"').Append(EscapeReg(v.RawValue)).Append("\"\r\n");
                }
                else
                {
                    var bytes = Encoding.Unicode.GetBytes(v.RawValue + "\0");
                    sb.Append("hex(2):").Append(string.Join(",", bytes.Select(b => b.ToString("x2")))).Append("\r\n");
                }
            }
        }

        return sb.ToString();
    }

    private static string EscapeReg(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // ------------------------------------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------------------------------------

    private static int CountExecutables(string dir, IReadOnlyList<string> exts)
    {
        try
        {
            var set = new HashSet<string>(exts, StringComparer.OrdinalIgnoreCase);
            return Directory.EnumerateFiles(dir).Count(f => set.Contains(Path.GetExtension(f)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return -1; // unknown; do not report as empty
        }
    }

    private static bool IsWritable(string dir)
    {
        try
        {
            var probe = Path.Combine(dir, $".winhance-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, string.Empty);
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
