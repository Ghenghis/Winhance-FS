using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services;

/// <summary>
/// The real Windows store for environment variables: HKCU\Environment and
/// HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment. Values are read raw (unexpanded)
/// with their registry type, which is what the doctor needs to detect REG_SZ/REG_EXPAND_SZ mistakes.
/// </summary>
public sealed class RegistryEnvironmentStore : IEnvironmentStore
{
    private const string UserKey = "Environment";
    private const string MachineKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment";

    public IReadOnlyList<EnvVariable> Read(EnvScope scope)
    {
        var list = new List<EnvVariable>();
        using var key = Open(scope, writable: false);
        if (key == null)
        {
            return list;
        }

        foreach (var name in key.GetValueNames())
        {
            if (string.IsNullOrEmpty(name))
            {
                continue; // the unnamed (default) value is not an environment variable
            }

            var kind = key.GetValueKind(name);
            if (kind is not (RegistryValueKind.String or RegistryValueKind.ExpandString))
            {
                continue;
            }

            var raw = key.GetValue(name, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;
            list.Add(new EnvVariable(scope, name, raw, kind == RegistryValueKind.ExpandString ? EnvValueKind.ExpandString : EnvValueKind.String));
        }

        return list;
    }

    public void Write(EnvScope scope, string name, string value, EnvValueKind kind)
    {
        using var key = Open(scope, writable: true)
            ?? throw new UnauthorizedAccessException($"Cannot open the {scope} environment key for writing.");
        key.SetValue(name, value, kind == EnvValueKind.ExpandString ? RegistryValueKind.ExpandString : RegistryValueKind.String);
    }

    public void Delete(EnvScope scope, string name)
    {
        using var key = Open(scope, writable: true)
            ?? throw new UnauthorizedAccessException($"Cannot open the {scope} environment key for writing.");
        key.DeleteValue(name, throwOnMissingValue: false);
    }

    public bool CanWrite(EnvScope scope)
    {
        try
        {
            using var key = Open(scope, writable: true);
            return key != null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return false;
        }
    }

    public void BroadcastChange()
    {
        // Same notification the System Properties dialog sends; Explorer and new processes pick up the change.
        SendMessageTimeout(new IntPtr(0xFFFF), 0x001A, UIntPtr.Zero, "Environment", 0x0002, 5000, out _);
    }

    private static RegistryKey? Open(EnvScope scope, bool writable) =>
        scope == EnvScope.User
            ? Registry.CurrentUser.OpenSubKey(UserKey, writable)
            : Registry.LocalMachine.OpenSubKey(MachineKey, writable);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint msg, UIntPtr wParam, string lParam, uint flags, uint timeout, out UIntPtr result);
}
