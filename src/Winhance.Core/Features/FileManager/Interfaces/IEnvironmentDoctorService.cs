using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Winhance.Core.Features.FileManager.Interfaces;

/// <summary>Where an environment variable is stored.</summary>
public enum EnvScope
{
    /// <summary>HKCU\Environment — current user only.</summary>
    User,

    /// <summary>HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment — all users (admin to change).</summary>
    Machine,
}

/// <summary>Registry value type of a stored variable.</summary>
public enum EnvValueKind
{
    /// <summary>REG_SZ — %VARIABLES% inside the value are NOT expanded by Windows.</summary>
    String,

    /// <summary>REG_EXPAND_SZ — %VARIABLES% are expanded when processes start.</summary>
    ExpandString,
}

public enum EnvSeverity
{
    Info,
    Warning,
    Error,
}

/// <summary>Stable codes for every problem the doctor can detect.</summary>
public enum EnvIssueCode
{
    // PATH-style lists (Path, PATHEXT, PSModulePath, ...)
    PathEntryMissing,
    PathEntryDuplicate,
    PathEntryDuplicateAcrossScopes,
    PathEntryEmpty,
    PathEntryQuoted,
    PathEntryWhitespace,
    PathEntryIsFile,
    PathEntryRelative,
    PathEntryUndefinedVariable,
    PathEntryNoExecutables,
    PathEntryUserWritableInMachinePath,
    PathTooLongForLegacyTools,
    VariableTooLong,
    EssentialSystemPathMissing,
    SystemPathNotFirst,

    // Registry storage
    ExpandableValueStoredAsString,

    // Command resolution / runtime conflicts
    CommandShadowed,
    WindowsAppsAliasShadowsRuntime,
    MultipleRuntimeVersions,
    SystemCommandShadowed,

    // Home / root variables
    HomeVariableMissingDirectory,
    HomeVariablePointsToBin,
    HomeVariableInvalidLayout,
    HomeVariableMismatchWithPath,
    GlobalPythonHomeOrPath,

    // Core Windows variables
    CoreVariableMissing,
    CoreVariableInvalid,
    PathExtMissingStandardExtensions,
    TempDirectoryInvalid,

    // Generic
    VariablePathValueMissing,
    VariableNameOrValueWhitespace,
    VariableOverriddenByUser,
    VariableEmpty,
    RedundantUserCopy,
    BackupCopyVariable,
    UnixStylePath,
    EnvironmentBlockLarge,

    // Security
    SecretInEnvironment,
    TlsKeyLogEnabled,

    // Developer-environment pitfalls
    GlobalNodeEnv,
    GlobalJavaOptions,
    PyLauncherVersionMissing,
    CondaBaseOnPath,
    ConflictingSdkVariables,
    ImportantVariableOverriddenByUser,
}

/// <summary>Detects and masks values that look like credentials so they are never shown or logged in full.</summary>
public static class EnvSecrets
{
    private static readonly string[] SecretWords =
    {
        "PASSWORD", "PASSWD", "PWD", "SECRET", "TOKEN", "APIKEY", "API_KEY", "ACCESS_KEY", "PRIVATE_KEY",
        "CLIENT_SECRET", "CONNECTION_STRING", "CREDENTIAL", "AUTH",
    };

    /// <summary>True for names like DB_PASSWORD, GITHUB_TOKEN, SUPABASE_KEY, OPENAI_API_KEY.</summary>
    public static bool IsSecretName(string name)
    {
        var n = name.ToUpperInvariant().Replace("-", "_");
        if (n is "PWD" or "OLDPWD")
        {
            return false; // shell working-directory variables, not passwords
        }

        if (n.EndsWith("_PATH", StringComparison.Ordinal) || n.EndsWith("_DIR", StringComparison.Ordinal)
            || n.EndsWith("_FILE", StringComparison.Ordinal) || n.EndsWith("_URL", StringComparison.Ordinal)
            || n.EndsWith("_HOST", StringComparison.Ordinal) || n.EndsWith("_PORT", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var w in SecretWords)
        {
            if (n.Contains(w, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return n.EndsWith("_KEY", StringComparison.Ordinal) || n == "KEY";
    }

    /// <summary>Connection strings with an inline password (user:pass@host or Password=...).</summary>
    public static bool ValueContainsPassword(string value) =>
        System.Text.RegularExpressions.Regex.IsMatch(value, @"://[^/\s:@]+:[^@\s]+@|(?i)\b(password|pwd)\s*=\s*[^;\s]+");

    public static string Mask(string name, string value) =>
        IsSecretName(name) || ValueContainsPassword(value)
            ? (value.Length == 0 ? string.Empty : $"•••••• ({value.Length} characters, hidden)")
            : value;
}

/// <summary>What a fix does to the stored value.</summary>
public enum EnvFixAction
{
    /// <summary>Remove one list entry (by normalized match) from a PATH-style variable.</summary>
    RemoveListEntry,

    /// <summary>Replace one list entry with another (e.g. strip quotes, trim, make absolute).</summary>
    ReplaceListEntry,

    /// <summary>Insert a list entry at <see cref="EnvFix.InsertIndex"/> (or append when -1).</summary>
    InsertListEntry,

    /// <summary>Move a list entry to <see cref="EnvFix.InsertIndex"/>.</summary>
    MoveListEntry,

    /// <summary>Set the whole value.</summary>
    SetValue,

    /// <summary>Delete the variable.</summary>
    DeleteVariable,

    /// <summary>Change only the registry value type (REG_SZ ↔ REG_EXPAND_SZ).</summary>
    ChangeKind,
}

/// <summary>One stored variable as read from the registry (value is raw, not expanded).</summary>
public sealed record EnvVariable(EnvScope Scope, string Name, string RawValue, EnvValueKind Kind);

/// <summary>Both registry scopes at one moment.</summary>
public sealed record EnvSnapshot(DateTime TakenUtc, IReadOnlyList<EnvVariable> Variables)
{
    public EnvVariable? Get(EnvScope scope, string name)
    {
        foreach (var v in Variables)
        {
            if (v.Scope == scope && string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return v;
            }
        }

        return null;
    }
}

/// <summary>
/// A proposed, machine-applicable correction for an issue. For list fixes, <see cref="Index"/> is the entry's
/// position in the value as scanned and <see cref="Entry"/> its raw text; the plan refuses the fix if the value
/// changed since the scan. <see cref="InsertIndex"/> is the target position for insert/move (-1 = end).
/// </summary>
public sealed record EnvFix(
    EnvFixAction Action,
    EnvScope Scope,
    string Variable,
    string? Entry = null,
    string? NewValue = null,
    int InsertIndex = -1,
    EnvValueKind? NewKind = null,
    string Description = "",
    int Index = -1);

/// <summary>A detected problem, with an optional fix.</summary>
public sealed record EnvIssue(
    string Id,
    EnvIssueCode Code,
    EnvSeverity Severity,
    EnvScope? Scope,
    string Variable,
    string Title,
    string Detail,
    string? Entry = null,
    EnvFix? Fix = null);

/// <summary>Status of one entry of a PATH-style variable, for display.</summary>
public sealed record EnvPathEntry(
    EnvScope Scope,
    int Index,
    string Raw,
    string Expanded,
    bool Exists,
    bool IsDuplicate,
    int ExecutableCount,
    string Status)
{
    /// <summary>1-based position, as shown in the Windows editor.</summary>
    public int Position => Index + 1;
}

/// <summary>Every match for a command along the effective PATH (Machine first, then User).</summary>
public sealed record EnvCommandResolution(
    string Command,
    string? Winner,
    IReadOnlyList<string> AllMatches,
    IReadOnlyList<string> Versions);

/// <summary>Full result of a scan.</summary>
public sealed record EnvDiagnosis(
    EnvSnapshot Snapshot,
    IReadOnlyList<EnvIssue> Issues,
    IReadOnlyList<EnvPathEntry> PathEntries,
    IReadOnlyList<EnvCommandResolution> Commands,
    int EffectivePathLength);

/// <summary>The net change to one variable after merging all selected fixes.</summary>
public sealed record EnvChange(
    EnvScope Scope,
    string Name,
    string? OldValue,
    string? NewValue,
    EnvValueKind? OldKind,
    EnvValueKind? NewKind,
    IReadOnlyList<string> Reasons)
{
    public bool IsDelete => NewValue is null;

    public bool IsCreate => OldValue is null && NewValue is not null;
}

/// <summary>Changes to apply, plus anything the safety checks refused.</summary>
public sealed record EnvRepairPlan(
    IReadOnlyList<EnvChange> Changes,
    IReadOnlyList<string> Refused,
    bool RequiresAdmin);

/// <summary>A saved copy of both scopes taken before a repair or on request.</summary>
public sealed record EnvBackup(string Id, DateTime CreatedUtc, string Reason, string JsonPath, string RegPath, int VariableCount);

public sealed record EnvApplyResult(
    bool Success,
    EnvBackup? Backup,
    int ChangesApplied,
    bool RolledBack,
    IReadOnlyList<string> Errors);

/// <summary>Reads and writes environment variables in one scope. Abstracted so the doctor can be tested.</summary>
public interface IEnvironmentStore
{
    IReadOnlyList<EnvVariable> Read(EnvScope scope);

    void Write(EnvScope scope, string name, string value, EnvValueKind kind);

    void Delete(EnvScope scope, string name);

    /// <summary>Tells running programs (Explorer, new terminals) that the environment changed.</summary>
    void BroadcastChange();

    bool CanWrite(EnvScope scope);
}

/// <summary>
/// Diagnoses and repairs Windows environment variables: PATH problems, broken runtime homes, command
/// conflicts, registry type errors and damaged core variables — with preview, backup, verify and undo.
/// </summary>
public interface IEnvironmentDoctorService
{
    /// <summary>Commands checked for conflicts (python, node, java, git, ...).</summary>
    IReadOnlyList<string> WatchedCommands { get; }

    EnvSnapshot TakeSnapshot();

    Task<EnvDiagnosis> DiagnoseAsync(CancellationToken cancellationToken = default);

    /// <summary>Merges the fixes into per-variable changes and runs safety checks. Changes nothing.</summary>
    EnvRepairPlan BuildPlan(EnvSnapshot snapshot, IEnumerable<EnvFix> fixes);

    /// <summary>Backs up, applies, verifies by re-reading and rolls back automatically on any mismatch.</summary>
    Task<EnvApplyResult> ApplyAsync(EnvRepairPlan plan, CancellationToken cancellationToken = default);

    Task<EnvBackup> CreateBackupAsync(string reason, CancellationToken cancellationToken = default);

    IReadOnlyList<EnvBackup> ListBackups();

    /// <summary>Restores every variable in both scopes to the backup (variables added since are removed).</summary>
    Task<EnvApplyResult> RestoreBackupAsync(string backupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fixes that make <paramref name="matchPath"/> the copy of <paramref name="command"/> that runs (moves its
    /// folder ahead of the current winner; across scopes, adds it to the front of the Machine PATH and removes
    /// it from the User PATH). Empty if it already wins or cannot be made to win safely.
    /// </summary>
    IReadOnlyList<EnvFix> CreatePreferFixes(EnvDiagnosis diagnosis, string command, string matchPath);

    /// <summary>Expands %VARIABLES% using the given snapshot (process, then Machine, then User overrides).</summary>
    string Expand(EnvSnapshot snapshot, string value);
}
