using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Xunit;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Environment &amp; PATH Doctor: detection, planning safety, apply/verify/rollback, backup/restore.
/// Uses an in-memory store, so the real registry is never touched.
/// </summary>
public class EnvironmentDoctorTests : IDisposable
{
    private static readonly string Sys32 = @"%SystemRoot%\system32";
    private readonly string _root;
    private readonly InMemoryStore _store = new();
    private readonly EnvironmentDoctorService _doctor;

    public EnvironmentDoctorTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "winhance-envdoc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _doctor = new EnvironmentDoctorService(_store, Mock.Of<ILogService>(), Path.Combine(_root, "backups"));
        _store.Set(EnvScope.Machine, "Path", $@"{Sys32};%SystemRoot%;%SystemRoot%\System32\Wbem", EnvValueKind.ExpandString);
        _store.Set(EnvScope.Machine, "PATHEXT", ".COM;.EXE;.BAT;.CMD", EnvValueKind.String);
        _store.Set(EnvScope.Machine, "ComSpec", @"%SystemRoot%\system32\cmd.exe", EnvValueKind.ExpandString);
        _store.Set(EnvScope.Machine, "windir", "%SystemRoot%", EnvValueKind.ExpandString);
        _store.Set(EnvScope.Machine, "OS", "Windows_NT", EnvValueKind.String);
        _store.Set(EnvScope.User, "TEMP", Path.GetTempPath().TrimEnd('\\'), EnvValueKind.String);
        _store.Set(EnvScope.User, "TMP", Path.GetTempPath().TrimEnd('\\'), EnvValueKind.String);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string Dir(string name)
    {
        var d = Path.Combine(_root, name);
        Directory.CreateDirectory(d);
        return d;
    }

    private string Exe(string dir, string name, int bytes = 64)
    {
        var f = Path.Combine(dir, name);
        File.WriteAllBytes(f, new byte[bytes]);
        return f;
    }

    private EnvDiagnosis Scan() => _doctor.Diagnose(_doctor.TakeSnapshot());

    private EnvApplyResult Fix(EnvDiagnosis d, Func<EnvIssue, bool> which)
    {
        var plan = _doctor.BuildPlan(d.Snapshot, d.Issues.Where(which).Where(i => i.Fix != null).Select(i => i.Fix!));
        plan.Refused.Should().BeEmpty();
        return _doctor.ApplyAsync(plan).GetAwaiter().GetResult();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void Detects_Missing_Duplicate_Quoted_Empty_AndFixesThemTogether()
    {
        var tools = Dir("tools");
        Exe(tools, "tool.exe");
        var missing = Path.Combine(_root, "uninstalled");
        _store.Set(EnvScope.User, "Path", $"{tools};{missing};;\"{tools}\";{tools}\\;", EnvValueKind.String);

        var d = Scan();
        d.Issues.Should().Contain(i => i.Code == EnvIssueCode.PathEntryMissing && i.Entry == missing);
        d.Issues.Should().Contain(i => i.Code == EnvIssueCode.PathEntryQuoted);
        d.Issues.Should().Contain(i => i.Code == EnvIssueCode.PathEntryEmpty);
        d.Issues.Count(i => i.Code == EnvIssueCode.PathEntryDuplicate).Should().Be(2);

        var result = Fix(d, i => i.Scope == EnvScope.User && i.Variable == "Path"
            && i.Code is EnvIssueCode.PathEntryMissing or EnvIssueCode.PathEntryEmpty or EnvIssueCode.PathEntryDuplicate);

        result.Success.Should().BeTrue();
        result.Backup.Should().NotBeNull();
        _store.Get(EnvScope.User, "Path")!.Value.Value.Should().Be(tools);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void RegSzWithVariables_IsDetected_AndConvertedToExpandString()
    {
        _store.Set(EnvScope.Machine, "Path", $@"{Sys32};%SystemRoot%;%SystemRoot%\System32\Wbem", EnvValueKind.String);

        var d = Scan();
        var issue = d.Issues.Single(i => i.Code == EnvIssueCode.ExpandableValueStoredAsString && i.Variable == "Path");
        issue.Severity.Should().Be(EnvSeverity.Error);

        Fix(d, i => i == issue).Success.Should().BeTrue();
        _store.Get(EnvScope.Machine, "Path")!.Value.Kind.Should().Be(EnvValueKind.ExpandString);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void MissingSystem32_IsInsertedAtTheFront()
    {
        var tools = Dir("tools");
        _store.Set(EnvScope.Machine, "Path", $@"{tools};%SystemRoot%;%SystemRoot%\System32\Wbem", EnvValueKind.ExpandString);

        var d = Scan();
        var issue = d.Issues.Single(i => i.Code == EnvIssueCode.EssentialSystemPathMissing && i.Entry == Sys32);
        issue.Severity.Should().Be(EnvSeverity.Error);

        Fix(d, i => i == issue).Success.Should().BeTrue();
        _store.Get(EnvScope.Machine, "Path")!.Value.Value.Split(';')[0].Should().Be(Sys32);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void Plan_Refuses_RemovingSystem32_EmptyingSystemPath_AndDeletingPath()
    {
        var snap = _doctor.TakeSnapshot();

        var removeSys32 = _doctor.BuildPlan(snap, new[] { new EnvFix(EnvFixAction.RemoveListEntry, EnvScope.Machine, "Path", Sys32, Index: 0) });
        removeSys32.Changes.Should().BeEmpty();
        removeSys32.Refused.Should().ContainSingle(r => r.Contains("system32"));

        var deletePath = _doctor.BuildPlan(snap, new[] { new EnvFix(EnvFixAction.DeleteVariable, EnvScope.Machine, "Path") });
        deletePath.Changes.Should().BeEmpty();
        deletePath.Refused.Should().ContainSingle(r => r.Contains("break Windows"));

        var empty = _doctor.BuildPlan(snap, new[] { new EnvFix(EnvFixAction.SetValue, EnvScope.Machine, "Path", NewValue: ";") });
        empty.Changes.Should().BeEmpty();
        empty.Refused.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void Plan_RefusesStaleFix_WhenValueChangedSinceScan()
    {
        var missing = Path.Combine(_root, "gone");
        _store.Set(EnvScope.User, "Path", missing, EnvValueKind.String);
        var d = Scan();
        _store.Set(EnvScope.User, "Path", Dir("new") + ";" + missing, EnvValueKind.String);

        var plan = _doctor.BuildPlan(_doctor.TakeSnapshot(), d.Issues.Where(i => i.Code == EnvIssueCode.PathEntryMissing).Select(i => i.Fix!));

        plan.Changes.Should().BeEmpty();
        plan.Refused.Should().ContainSingle(r => r.Contains("changed since the scan"));
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Apply_RefusesWhenAnotherProgramChangedTheValue_AndWritesNothing()
    {
        var missing = Path.Combine(_root, "gone");
        _store.Set(EnvScope.User, "Path", missing, EnvValueKind.String);
        var d = Scan();
        var plan = _doctor.BuildPlan(d.Snapshot, d.Issues.Where(i => i.Code == EnvIssueCode.PathEntryMissing).Select(i => i.Fix!));
        _store.Set(EnvScope.User, "Path", "changed-by-installer", EnvValueKind.String);

        var result = await _doctor.ApplyAsync(plan);

        result.Success.Should().BeFalse();
        result.Backup.Should().BeNull();
        _store.Get(EnvScope.User, "Path")!.Value.Value.Should().Be("changed-by-installer");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Apply_RollsBack_WhenValuesDoNotReadBack()
    {
        var missing = Path.Combine(_root, "gone");
        var keep = Dir("keep");
        var original = $"{keep};{missing}";
        _store.Set(EnvScope.User, "Path", original, EnvValueKind.String);
        var d = Scan();
        var plan = _doctor.BuildPlan(d.Snapshot, d.Issues.Where(i => i.Code == EnvIssueCode.PathEntryMissing).Select(i => i.Fix!));
        _store.CorruptNextWrite = true;

        var result = await _doctor.ApplyAsync(plan);

        result.Success.Should().BeFalse();
        result.RolledBack.Should().BeTrue();
        _store.Get(EnvScope.User, "Path")!.Value.Value.Should().Be(original);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Backup_Restore_PutsEverythingBack_IncludingRemovingAddedVariables()
    {
        var backup = await _doctor.CreateBackupAsync("test");
        _store.Set(EnvScope.User, "NEW_TOOL_HOME", @"C:\nowhere", EnvValueKind.String);
        _store.Set(EnvScope.Machine, "OS", "broken", EnvValueKind.String);

        var result = await _doctor.RestoreBackupAsync(backup.Id);

        result.Success.Should().BeTrue();
        _store.Get(EnvScope.User, "NEW_TOOL_HOME").Should().BeNull();
        _store.Get(EnvScope.Machine, "OS")!.Value.Value.Should().Be("Windows_NT");
        _doctor.ListBackups().Should().HaveCount(2, "restoring takes its own backup first, so a restore can be undone too");
        File.ReadAllText(backup.RegPath).Should().Contain("Windows Registry Editor Version 5.00");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void RegExport_EncodesExpandStringAsHex2()
    {
        var reg = EnvironmentDoctorService.ToRegFile(_doctor.TakeSnapshot());

        reg.Should().Contain("\"Path\"=hex(2):25,00,53,00"); // "%S" in UTF-16LE
        reg.Should().Contain("\"OS\"=\"Windows_NT\"");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void HomeVariablePointingToBin_IsCorrectedToTheRoot()
    {
        var jdk = Dir("jdk-21");
        Exe(Dir(@"jdk-21\bin"), "java.exe");
        _store.Set(EnvScope.Machine, "JAVA_HOME", Path.Combine(jdk, "bin"), EnvValueKind.String);

        var d = Scan();
        var issue = d.Issues.Single(i => i.Code == EnvIssueCode.HomeVariablePointsToBin);

        Fix(d, i => i == issue).Success.Should().BeTrue();
        _store.Get(EnvScope.Machine, "JAVA_HOME")!.Value.Value.Should().Be(jdk);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void HomeVariableMissing_IsAnError()
    {
        _store.Set(EnvScope.User, "GRADLE_HOME", Path.Combine(_root, "gradle-uninstalled"), EnvValueKind.String);

        Scan().Issues.Should().Contain(i => i.Code == EnvIssueCode.HomeVariableMissingDirectory && i.Severity == EnvSeverity.Error && i.Fix != null);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void PathExtMissingExe_IsRepaired()
    {
        _store.Set(EnvScope.Machine, "PATHEXT", ".COM;.BAT;.CMD;.PY", EnvValueKind.String);

        var d = Scan();
        var issue = d.Issues.Single(i => i.Code == EnvIssueCode.PathExtMissingStandardExtensions);

        Fix(d, i => i == issue).Success.Should().BeTrue();
        _store.Get(EnvScope.Machine, "PATHEXT")!.Value.Value.Split(';').Should().Contain(new[] { ".EXE", ".PY" });
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void Secrets_AreFlagged_AndNeverShownInMessages()
    {
        const string secret = "sk-super-secret-value-123";
        _store.Set(EnvScope.User, "SUPABASE_KEY", secret, EnvValueKind.String);
        _store.Set(EnvScope.Machine, "CONNECTION_STRING", "postgresql://admin:hunter2@localhost/db", EnvValueKind.String);

        var d = Scan();

        d.Issues.Should().Contain(i => i.Code == EnvIssueCode.SecretInEnvironment && i.Variable == "SUPABASE_KEY" && i.Severity == EnvSeverity.Warning);
        d.Issues.Should().Contain(i => i.Code == EnvIssueCode.SecretInEnvironment && i.Variable == "CONNECTION_STRING" && i.Severity == EnvSeverity.Error);
        d.Issues.Should().NotContain(i => i.Detail.Contains(secret) || i.Title.Contains(secret) || i.Detail.Contains("hunter2"));
        EnvSecrets.Mask("DB_PASSWORD", "postgres").Should().NotContain("postgres");
        EnvSecrets.Mask("OLLAMA_HOST", "127.0.0.1:11500").Should().Be("127.0.0.1:11500");
        EnvSecrets.IsSecretName("SSL_KEY_PATH").Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void WorkstationPitfalls_AreDetected()
    {
        _store.Set(EnvScope.User, "SSLKEYLOGFILE", Path.Combine(_root, "keys.log"), EnvValueKind.String);
        _store.Set(EnvScope.Machine, "NODE_ENV", "production", EnvValueKind.String);
        _store.Set(EnvScope.User, "_JAVA_OPTIONS", "-Xmx2g", EnvValueKind.String);
        _store.Set(EnvScope.User, "PYTHONPATH", Dir("pylibs"), EnvValueKind.String);
        _store.Set(EnvScope.User, "PATH_BACKUP", "C:\\old", EnvValueKind.String);
        _store.Set(EnvScope.User, "PROJECTS_PATH", "/projects", EnvValueKind.String);
        _store.Set(EnvScope.Machine, "CUDA_PATH", Dir("cuda13"), EnvValueKind.String);
        _store.Set(EnvScope.User, "CUDA_PATH", Dir("cuda12"), EnvValueKind.String);
        _store.Set(EnvScope.Machine, "OPENAI_BASE_URL", "https://example.invalid/v1", EnvValueKind.String);
        _store.Set(EnvScope.User, "OPENAI_BASE_URL", "https://example.invalid/v1", EnvValueKind.String);

        var codes = Scan().Issues.Select(i => i.Code).ToList();

        codes.Should().Contain(new[]
        {
            EnvIssueCode.TlsKeyLogEnabled, EnvIssueCode.GlobalNodeEnv, EnvIssueCode.GlobalJavaOptions,
            EnvIssueCode.GlobalPythonHomeOrPath, EnvIssueCode.BackupCopyVariable, EnvIssueCode.UnixStylePath,
            EnvIssueCode.ImportantVariableOverriddenByUser, EnvIssueCode.RedundantUserCopy,
        });
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void CondaOnPath_IsFlaggedPerFolder_WithRemoveFix()
    {
        var conda = Dir("miniconda3");
        var usrBin = Dir(@"miniconda3\Library\usr\bin");
        _store.Set(EnvScope.User, "Path", $"{conda};{usrBin}", EnvValueKind.String);

        var issues = Scan().Issues.Where(i => i.Code == EnvIssueCode.CondaBaseOnPath).ToList();

        issues.Should().HaveCount(2);
        issues.Should().Contain(i => i.Entry == usrBin && i.Severity == EnvSeverity.Warning);
        issues.Should().OnlyContain(i => i.Fix != null && i.Fix.Action == EnvFixAction.RemoveListEntry);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void StoreAlias_ShadowingRealPython_IsDetected_AndFixMovesRealPythonFirst()
    {
        var alias = Dir(@"Microsoft\WindowsApps");
        Exe(alias, "python.exe", bytes: 0);
        var real = Dir("Python312");
        Exe(real, "python.exe");
        _store.Set(EnvScope.User, "Path", $"{alias};{real}", EnvValueKind.String);

        var d = Scan();
        var issue = d.Issues.Single(i => i.Code == EnvIssueCode.WindowsAppsAliasShadowsRuntime);

        Fix(d, i => i == issue).Success.Should().BeTrue();
        _store.Get(EnvScope.User, "Path")!.Value.Value.Should().Be($"{real};{alias}");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task PreferFixes_AcrossScopes_AddToSystemPathAndRemoveFromUser()
    {
        var sysNode = Dir("node-system");
        Exe(sysNode, "node.exe");
        var userNode = Dir("node-user");
        Exe(userNode, "node.exe");
        _store.Set(EnvScope.Machine, "Path", $@"{Sys32};{sysNode}", EnvValueKind.ExpandString);
        _store.Set(EnvScope.User, "Path", userNode, EnvValueKind.String);

        var d = Scan();
        var fixes = _doctor.CreatePreferFixes(d, "node", Path.Combine(userNode, "node.exe"));
        fixes.Should().HaveCount(2);

        var plan = _doctor.BuildPlan(d.Snapshot, fixes);
        plan.Refused.Should().BeEmpty();
        (await _doctor.ApplyAsync(plan)).Success.Should().BeTrue();

        _doctor.Diagnose(_doctor.TakeSnapshot()).Commands.Single(c => c.Command == "node").Winner
            .Should().Be(Path.Combine(userNode, "node.exe"));
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void Diagnosis_OnThisPc_RunsWithoutChangingAnything()
    {
        // Read-only smoke test against the real registry.
        var real = new EnvironmentDoctorService(new RegistryEnvironmentStore(), Mock.Of<ILogService>(), Path.Combine(_root, "real-backups"));
        var before = real.TakeSnapshot();

        var d = real.Diagnose(before);

        d.Snapshot.Variables.Should().Contain(v => v.Scope == EnvScope.Machine && v.Name.Equals("Path", StringComparison.OrdinalIgnoreCase));
        d.Issues.Should().NotContain(i => i.Detail.Length == 0);
        real.TakeSnapshot().Variables.Should().BeEquivalentTo(before.Variables);
    }

    private sealed class InMemoryStore : IEnvironmentStore
    {
        private readonly Dictionary<(EnvScope, string), (string Value, EnvValueKind Kind)> _values = new();

        public bool CorruptNextWrite { get; set; }

        public void Set(EnvScope scope, string name, string value, EnvValueKind kind) => _values[(scope, name.ToUpperInvariant())] = (value, kind);

        public (string Value, EnvValueKind Kind)? Get(EnvScope scope, string name) =>
            _values.TryGetValue((scope, name.ToUpperInvariant()), out var v) ? v : null;

        public IReadOnlyList<EnvVariable> Read(EnvScope scope) =>
            _values.Where(kv => kv.Key.Item1 == scope).Select(kv => new EnvVariable(scope, Canonical(kv.Key.Item2), kv.Value.Value, kv.Value.Kind)).ToList();

        public void Write(EnvScope scope, string name, string value, EnvValueKind kind)
        {
            if (CorruptNextWrite)
            {
                CorruptNextWrite = false;
                value += "-corrupted";
            }

            Set(scope, name, value, kind);
        }

        public void Delete(EnvScope scope, string name) => _values.Remove((scope, name.ToUpperInvariant()));

        public void BroadcastChange()
        {
        }

        public bool CanWrite(EnvScope scope) => true;

        private static string Canonical(string upper) => upper switch
        {
            "PATH" => "Path",
            "COMSPEC" => "ComSpec",
            "WINDIR" => "windir",
            _ => upper,
        };
    }
}
