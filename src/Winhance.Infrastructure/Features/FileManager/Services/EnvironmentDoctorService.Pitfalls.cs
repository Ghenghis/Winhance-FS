using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services;

/// <summary>
/// Security and developer-environment checks drawn from real workstation problems: secrets and TLS key logging
/// in the environment, global NODE_ENV/_JAVA_OPTIONS/PYTHONPATH, py-launcher versions that are not installed,
/// conda on the global PATH, conflicting SDK variables, backup copies and Unix-style paths.
/// </summary>
public sealed partial class EnvironmentDoctorService
{
    /// <summary>Variables where a User value silently replacing the System value usually means a version conflict.</summary>
    private static readonly HashSet<string> ImportantOverrides = new(StringComparer.OrdinalIgnoreCase)
    {
        "CUDA_PATH", "CUDA_HOME", "JAVA_HOME", "JDK_HOME", "ANDROID_HOME", "ANDROID_SDK_ROOT", "GOROOT", "GOPATH",
        "DOTNET_ROOT", "PYTHONHOME", "PYTHONPATH", "NODE_PATH", "NVM_HOME", "NVM_SYMLINK", "VULKAN_SDK", "VK_SDK_PATH",
        "MAVEN_HOME", "M2_HOME", "GRADLE_HOME", "CARGO_HOME", "RUSTUP_HOME", "PATHEXT",
    };

    private static readonly string[] CondaMarkers = { @"\miniconda3", @"\anaconda3", @"\miniforge3", @"\mambaforge" };

    private static string Show(EnvVariable v) => EnvSecrets.Mask(v.Name, v.RawValue);

    private void CheckSecurityAndPitfalls(ScanContext ctx, IReadOnlyList<EnvCommandResolution> commands)
    {
        var snap = ctx.Snapshot;

        foreach (var v in snap.Variables)
        {
            var name = v.Name.ToUpperInvariant();

            if (name == "SSLKEYLOGFILE")
            {
                ctx.Add(EnvIssueCode.TlsKeyLogEnabled, EnvSeverity.Error, v.Scope, v.Name,
                    "TLS key logging is on for every program (SSLKEYLOGFILE)",
                    $"Browsers, curl, Node and Python write the secret keys of every HTTPS connection to {v.RawValue}. Anyone who can read that file can decrypt captured traffic, including logins. Only set it temporarily while debugging with Wireshark.",
                    null, new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: "Delete SSLKEYLOGFILE"));
                continue;
            }

            if (EnvSecrets.IsSecretName(v.Name) || EnvSecrets.ValueContainsPassword(v.RawValue))
            {
                if (v.RawValue.Trim().Length > 0)
                {
                    ctx.Add(EnvIssueCode.SecretInEnvironment, v.Scope == EnvScope.Machine ? EnvSeverity.Error : EnvSeverity.Warning, v.Scope, v.Name,
                        $"{v.Name} looks like a password or key stored in plain text",
                        (v.Scope == EnvScope.Machine ? "System variables are readable by every account and service on this PC. " : "Every program you run, including browser extensions' native helpers and crash reporters, can read it. ") +
                        "Keep secrets in Windows Credential Manager, a .env file outside the repository, or the tool's own secret store. The value is hidden here.",
                        null, new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: $"Delete {v.Name} (store it somewhere safer first)"));
                }

                continue;
            }

            switch (name)
            {
                case "NODE_ENV":
                    ctx.Add(EnvIssueCode.GlobalNodeEnv, EnvSeverity.Warning, v.Scope, v.Name,
                        $"NODE_ENV={v.RawValue} is set for every program",
                        "With NODE_ENV=production, 'npm install' and 'yarn' skip devDependencies in every project, so builds and tests fail with missing modules; with 'development', production builds ship debug code. Set it per project or per script instead.",
                        null, new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: "Delete NODE_ENV"));
                    break;
                case "_JAVA_OPTIONS":
                case "JAVA_TOOL_OPTIONS":
                case "JDK_JAVA_OPTIONS":
                    ctx.Add(EnvIssueCode.GlobalJavaOptions, EnvSeverity.Warning, v.Scope, v.Name,
                        $"{v.Name} applies to every Java program",
                        $"'{Show(v)}'. Every JVM prints 'Picked up {v.Name}' on stderr, which breaks tools that parse 'java -version' (Gradle, Android Studio, Minecraft launchers), and the options apply to every Java app on the PC.",
                        null, new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: $"Delete {v.Name}"));
                    break;
                case "JAVA_OPTIONS":
                    ctx.Add(EnvIssueCode.GlobalJavaOptions, EnvSeverity.Info, v.Scope, v.Name,
                        "JAVA_OPTIONS is not read by Java itself",
                        "The JVM reads _JAVA_OPTIONS, JAVA_TOOL_OPTIONS or JDK_JAVA_OPTIONS. JAVA_OPTIONS only affects scripts that pass it explicitly — check whether it does anything.");
                    break;
            }

            if (BackupNamePattern().IsMatch(v.Name))
            {
                ctx.Add(EnvIssueCode.BackupCopyVariable, EnvSeverity.Info, v.Scope, v.Name,
                    $"{v.Name} looks like a manual backup copy",
                    $"A backup stored as a variable is passed to every program and is not a reliable backup. Winhance keeps proper backups (JSON + .reg) before every repair; export one, then delete {v.Name}.",
                    null, new EnvFix(EnvFixAction.DeleteVariable, v.Scope, v.Name, Description: $"Delete {v.Name}"));
            }

            var trimmed = v.RawValue.Trim();
            if (UnixPathPattern().IsMatch(trimmed) && !trimmed.Contains(';'))
            {
                ctx.Add(EnvIssueCode.UnixStylePath, EnvSeverity.Info, v.Scope, v.Name,
                    $"{v.Name} uses a Linux-style path ({trimmed})",
                    $"On Windows '{trimmed}' means the root of whatever drive the program runs from (for example C:{trimmed.Replace('/', '\\')}), so it changes with the working drive. Common when settings are copied from Docker or WSL configs; use a full Windows path.");
            }
        }

        CheckUserOverrides(ctx);
        CheckSdkConflicts(ctx);
        CheckPyLauncher(ctx, commands);
        CheckConda(ctx, commands);

        var total = snap.Variables.Sum(v => v.Name.Length + Expand(snap, v.RawValue).Length + 2);
        if (total > 32767)
        {
            ctx.Add(EnvIssueCode.EnvironmentBlockLarge, EnvSeverity.Info, null, "(all)",
                $"The environment is very large ({total:N0} characters, {snap.Variables.Count} variables)",
                "Every program gets a copy at start-up. Some tools (older MSBuild tasks, installers, 'cmd /c' with long command lines) fail when the environment is this large. Removing unused variables helps.");
        }
    }

    private void CheckUserOverrides(ScanContext ctx)
    {
        var snap = ctx.Snapshot;
        foreach (var user in snap.Variables.Where(v => v.Scope == EnvScope.User))
        {
            if (DirectoryListVariables.Contains(user.Name, StringComparer.OrdinalIgnoreCase) || snap.Get(EnvScope.Machine, user.Name) is not { } machine)
            {
                continue;
            }

            if (machine.RawValue == user.RawValue)
            {
                if (!user.Name.Equals("TEMP", StringComparison.OrdinalIgnoreCase) && !user.Name.Equals("TMP", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Add(EnvIssueCode.RedundantUserCopy, EnvSeverity.Info, EnvScope.User, user.Name,
                        $"User {user.Name} repeats the System value",
                        "The User copy has no effect today, but if the System value is updated later (for example by an installer), this account keeps the old one.",
                        null, new EnvFix(EnvFixAction.DeleteVariable, EnvScope.User, user.Name, Description: "Delete the User copy"));
                }

                continue;
            }

            if (ImportantOverrides.Contains(user.Name))
            {
                ctx.Add(EnvIssueCode.ImportantVariableOverriddenByUser, EnvSeverity.Warning, EnvScope.User, user.Name,
                    $"User {user.Name} overrides the System {user.Name}",
                    $"This account uses '{Show(user)}' while other accounts and services use '{Show(machine)}'. Tools started as administrator, services and scheduled tasks therefore use a different version than your terminal.",
                    null, new EnvFix(EnvFixAction.DeleteVariable, EnvScope.User, user.Name, Description: $"Delete the User value (use the System {user.Name})"));
            }
        }
    }

    private void CheckSdkConflicts(ScanContext ctx)
    {
        var snap = ctx.Snapshot;
        string? Effective(string name) => (snap.Get(EnvScope.User, name) ?? snap.Get(EnvScope.Machine, name)) is { } v
            ? Expand(snap, v.RawValue).Trim().TrimEnd('\\')
            : null;

        foreach (var (a, b, why) in new[]
        {
            ("ANDROID_HOME", "ANDROID_SDK_ROOT", "Android Gradle Plugin fails the build when both are set to different folders."),
            ("CUDA_PATH", "CUDA_HOME", "PyTorch, TensorFlow and CMake pick different CUDA toolkits, causing 'CUDA version mismatch' and missing-DLL errors."),
            ("JAVA_HOME", "JDK_HOME", "IDEs and build tools disagree about which JDK to use."),
            ("VULKAN_SDK", "VK_SDK_PATH", "Vulkan tools and build scripts use different SDK versions."),
            ("M2_HOME", "MAVEN_HOME", "Maven wrappers and IDEs use different Maven installations."),
        })
        {
            var va = Effective(a);
            var vb = Effective(b);
            if (va != null && vb != null && !string.Equals(va, vb, StringComparison.OrdinalIgnoreCase))
            {
                ctx.Add(EnvIssueCode.ConflictingSdkVariables, EnvSeverity.Warning, null, $"{a} / {b}",
                    $"{a} and {b} point to different folders", $"{a} = {va}; {b} = {vb}. {why}");
            }
        }

        // CUDA_PATH vs the newest installed toolkit (CUDA_PATH_Vxx_y).
        var versions = snap.Variables
            .Where(v => v.Scope == EnvScope.Machine && v.Name.StartsWith("CUDA_PATH_V", StringComparison.OrdinalIgnoreCase))
            .Select(v => (v.Name, Dir: Expand(snap, v.RawValue).TrimEnd('\\'), Ver: ParseCudaVersion(v.Name)))
            .Where(x => x.Ver != null && Directory.Exists(x.Dir))
            .OrderByDescending(x => x.Ver)
            .ToList();
        var cuda = Effective("CUDA_PATH");
        if (versions.Count > 1 && cuda != null && !string.Equals(cuda, versions[0].Dir, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Add(EnvIssueCode.ConflictingSdkVariables, EnvSeverity.Info, null, "CUDA_PATH",
                $"{versions.Count} CUDA toolkits installed; CUDA_PATH is not the newest",
                $"CUDA_PATH = {cuda}; newest is {versions[0].Dir}. Fine if intentional — make sure PyTorch/TensorFlow builds match the toolkit on PATH.");
        }
    }

    private static Version? ParseCudaVersion(string name)
    {
        var m = Regex.Match(name, @"CUDA_PATH_V(\d+)_(\d+)", RegexOptions.IgnoreCase);
        return m.Success ? new Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)) : null;
    }

    private void CheckPyLauncher(ScanContext ctx, IReadOnlyList<EnvCommandResolution> commands)
    {
        var snap = ctx.Snapshot;
        var installed = InstalledPythonVersions();
        foreach (var v in snap.Variables.Where(v => v.Name.StartsWith("PY_PYTHON", StringComparison.OrdinalIgnoreCase)))
        {
            var want = v.RawValue.Trim();
            var m = Regex.Match(want, @"^(\d+)(?:\.(\d+))?");
            if (!m.Success)
            {
                continue;
            }

            var ok = installed.Any(i => m.Groups[2].Success ? i == $"{m.Groups[1].Value}.{m.Groups[2].Value}" : i.StartsWith(m.Groups[1].Value + ".", StringComparison.Ordinal));
            if (!ok && installed.Count > 0)
            {
                var best = installed.OrderByDescending(i => Version.TryParse(i, out var pv) ? pv : new Version(0, 0)).First();
                ctx.Add(EnvIssueCode.PyLauncherVersionMissing, EnvSeverity.Error, v.Scope, v.Name,
                    $"{v.Name}={want}, but Python {want} is not installed",
                    $"'py' (and 'py -3') fail with 'No suitable Python runtime found'. Installed: {string.Join(", ", installed)}.",
                    null, new EnvFix(EnvFixAction.SetValue, v.Scope, v.Name, NewValue: best, NewKind: EnvValueKind.String, Description: $"Set to {best} (newest installed)"));
            }
            else if (ok && commands.FirstOrDefault(c => c.Command == "python") is { Winner: { } winner } py)
            {
                var ver = py.Versions.FirstOrDefault() ?? string.Empty;
                if (ver.Length > 0 && ver != "App Execution Alias" && !ver.StartsWith(want, StringComparison.Ordinal))
                {
                    ctx.Add(EnvIssueCode.PyLauncherVersionMissing, EnvSeverity.Info, v.Scope, v.Name,
                        $"'py' runs Python {want} but 'python' runs {ver}",
                        $"'python' resolves to {winner}. Scripts started with 'py' and with 'python' use different interpreters and installed packages.");
                }
            }
        }
    }

    /// <summary>Versions registered by python.org / Store installers (PEP 514 registry layout).</summary>
    private static List<string> InstalledPythonVersions()
    {
        var versions = new HashSet<string>(StringComparer.Ordinal);
        void Scan(RegistryKey root, string path)
        {
            try
            {
                using var key = root.OpenSubKey(path);
                if (key == null)
                {
                    return;
                }

                foreach (var sub in key.GetSubKeyNames())
                {
                    var m = Regex.Match(sub, @"^(\d+\.\d+)");
                    if (m.Success)
                    {
                        versions.Add(m.Groups[1].Value);
                    }
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
            }
        }

        Scan(Registry.CurrentUser, @"Software\Python\PythonCore");
        Scan(Registry.LocalMachine, @"Software\Python\PythonCore");
        Scan(Registry.LocalMachine, @"Software\WOW6432Node\Python\PythonCore");
        return versions.ToList();
    }

    private void CheckConda(ScanContext ctx, IReadOnlyList<EnvCommandResolution> commands)
    {
        var snap = ctx.Snapshot;
        var otherPython = commands.FirstOrDefault(c => c.Command == "python")?.AllMatches
            .Any(m => !CondaMarkers.Any(cm => m.Contains(cm + "\\", StringComparison.OrdinalIgnoreCase)) && !m.Contains(@"\WindowsApps\", StringComparison.OrdinalIgnoreCase)) ?? false;

        foreach (var scope in new[] { EnvScope.Machine, EnvScope.User })
        {
            var entries = SplitList(snap.Get(scope, "Path")?.RawValue);
            for (var i = 0; i < entries.Count; i++)
            {
                var expanded = Expand(snap, Unquote(entries[i].Trim()));
                var marker = CondaMarkers.FirstOrDefault(cm => expanded.Contains(cm, StringComparison.OrdinalIgnoreCase));
                if (marker == null || expanded.Trim().Length == 0)
                {
                    continue;
                }

                var isUsrBin = expanded.EndsWith(@"Library\usr\bin", StringComparison.OrdinalIgnoreCase) || expanded.EndsWith(@"Library\mingw-w64\bin", StringComparison.OrdinalIgnoreCase);
                ctx.Add(EnvIssueCode.CondaBaseOnPath, isUsrBin || otherPython ? EnvSeverity.Warning : EnvSeverity.Info, scope, "Path",
                    $"Conda folder on the {scope} PATH: {entries[i]}",
                    (isUsrBin
                        ? "This folder holds Unix tools and DLLs (OpenSSL, zlib, libcurl) that shadow Windows commands and get loaded by unrelated programs, causing crashes and 'procedure entry point not found'. "
                        : "Conda's base Python is on PATH for every program. ") +
                    (otherPython ? "It also conflicts with your other Python installation. " : string.Empty) +
                    "Conda recommends using 'conda init' / the Anaconda Prompt instead of adding these folders to PATH.",
                    entries[i],
                    new EnvFix(EnvFixAction.RemoveListEntry, scope, "Path", entries[i], Description: "Remove this conda folder from PATH", Index: i));
            }
        }
    }

    [GeneratedRegex(@"^(PATH|Path)[_\-\.]?(BACKUP|BAK|OLD|ORIG|ORIGINAL|SAVE|SAVED|COPY)\d*$|[_\-](BACKUP|BAK|OLD)\d*$", RegexOptions.IgnoreCase)]
    private static partial Regex BackupNamePattern();

    [GeneratedRegex(@"^/[A-Za-z0-9._\-]+(/[A-Za-z0-9._\-]*)*$")]
    private static partial Regex UnixPathPattern();
}
