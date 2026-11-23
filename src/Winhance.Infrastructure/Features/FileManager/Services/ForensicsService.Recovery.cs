using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using EnumerationOptions = System.IO.EnumerationOptions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Winhance.Core.Features.Common.Enums;
using Winhance.Core.Features.FileManager.Interfaces;

namespace Winhance.Infrastructure.Features.FileManager.Services;

/// <summary>Recovery: previous versions from Volume Shadow Copies, and the Recycle Bin.</summary>
public sealed partial class ForensicsService
{
    // ---------------------------------------------------------------- Shadow copies

    public Task<IReadOnlyList<ShadowCopyInfo>> ListShadowCopiesAsync(CancellationToken ct = default) => _shadowSource(ct);

    public async Task<IReadOnlyList<VolumeProtection>> GetVolumeProtectionAsync(CancellationToken ct = default)
    {
        var shadows = await _shadowSource(ct);
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d => d.RootDirectory.FullName)
            .Union(shadows.Select(s => s.VolumeRoot), StringComparer.OrdinalIgnoreCase);
        return drives
            .Select(root =>
            {
                var mine = shadows.Where(s => s.VolumeRoot.Equals(root, StringComparison.OrdinalIgnoreCase)).ToList();
                return new VolumeProtection(root, mine.Count, mine.Count == 0 ? null : mine.Max(s => s.Created));
            })
            .OrderBy(v => v.VolumeRoot, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<PreviousVersion>> GetPreviousVersionsAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path).TrimEnd('\\');
        var shadows = await _shadowSource(ct);
        return await Task.Run(
            () =>
            {
                var currentIsFile = File.Exists(full);
                var currentIsFolder = Directory.Exists(full);
                var found = new List<PreviousVersion>();
                foreach (var shadow in shadows.OrderByDescending(s => s.Created))
                {
                    ct.ThrowIfCancellationRequested();
                    var root = shadow.VolumeRoot.EndsWith('\\') ? shadow.VolumeRoot : shadow.VolumeRoot + "\\";
                    if (!(full + "\\").StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var snapshotPath = MapToSnapshot(shadow, full);
                    try
                    {
                        if (File.Exists(snapshotPath))
                        {
                            var fi = new FileInfo(snapshotPath);
                            var state = !currentIsFile ? VersionState.DeletedNow
                                : fi.Length == new FileInfo(full).Length && fi.LastWriteTimeUtc == File.GetLastWriteTimeUtc(full) ? VersionState.SameAsCurrent
                                : VersionState.Different;
                            found.Add(new PreviousVersion(shadow, full, snapshotPath, false, fi.Length, fi.LastWriteTime, state));
                        }
                        else if (Directory.Exists(snapshotPath))
                        {
                            var di = new DirectoryInfo(snapshotPath);
                            var state = !currentIsFolder ? VersionState.DeletedNow
                                : di.LastWriteTimeUtc == Directory.GetLastWriteTimeUtc(full) ? VersionState.SameAsCurrent
                                : VersionState.Different;
                            found.Add(new PreviousVersion(shadow, full, snapshotPath, true, 0, di.LastWriteTime, state));
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        _log?.Log(LogLevel.Warning, $"Can't read {snapshotPath}: {ex.Message}");
                    }
                }

                // Like Explorer's Previous Versions: one row per distinct version (newest snapshot that holds it).
                return (IReadOnlyList<PreviousVersion>)found
                    .GroupBy(v => (v.IsFolder, v.Size, v.Modified))
                    .Select(g => g.First())
                    .OrderByDescending(v => v.Snapshot.Created)
                    .ToList();
            },
            ct);
    }

    public Task<RestoreResult> RestorePreviousVersionAsync(PreviousVersion version, string destinationPath, bool overwrite = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        return Task.Run(
            () =>
            {
                var dest = Path.GetFullPath(destinationPath);
                try
                {
                    if (version.IsFolder)
                    {
                        if (Directory.Exists(dest) || File.Exists(dest))
                        {
                            return new RestoreResult(false, null, $"'{dest}' already exists. Restore the folder to a new location.");
                        }

                        var copied = CopyDirectory(version.SnapshotPath, dest, ct);
                        _log?.Log(LogLevel.Info, $"Restored folder {version.OriginalPath} ({copied} files) from snapshot {version.Snapshot.Created:u} to {dest}");
                        return new RestoreResult(true, dest, null);
                    }

                    if (Directory.Exists(dest))
                    {
                        return new RestoreResult(false, null, $"'{dest}' is a folder.");
                    }

                    if (File.Exists(dest) && !overwrite)
                    {
                        return new RestoreResult(false, null, $"'{dest}' already exists.");
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

                    // Copy next to the destination first, verify, then swap in: a failed copy never damages the target.
                    var temp = dest + ".winhance-restore";
                    File.Copy(version.SnapshotPath, temp, overwrite: true);
                    if (new FileInfo(temp).Length != version.Size)
                    {
                        File.Delete(temp);
                        return new RestoreResult(false, null, "The restored copy's size does not match the snapshot; nothing was changed.");
                    }

                    File.Move(temp, dest, overwrite);
                    _log?.Log(LogLevel.Info, $"Restored {version.OriginalPath} from snapshot {version.Snapshot.Created:u} to {dest}");
                    return new RestoreResult(true, dest, null);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return new RestoreResult(false, null, ex.Message);
                }
            },
            ct);
    }

    internal static string MapToSnapshot(ShadowCopyInfo shadow, string fullPath)
    {
        var root = shadow.VolumeRoot.EndsWith('\\') ? shadow.VolumeRoot : shadow.VolumeRoot + "\\";
        var relative = fullPath.Length > root.Length ? fullPath[root.Length..] : string.Empty;
        return shadow.DeviceObject.TrimEnd('\\') + "\\" + relative;
    }

    private static int CopyDirectory(string source, string dest, CancellationToken ct)
    {
        var count = 0;
        Directory.CreateDirectory(dest);
        foreach (var dir in Directory.EnumerateDirectories(source, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
        {
            Directory.CreateDirectory(Path.Combine(dest, Path.GetRelativePath(source, dir)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint }))
        {
            ct.ThrowIfCancellationRequested();
            File.Copy(file, Path.Combine(dest, Path.GetRelativePath(source, file)), overwrite: false);
            count++;
        }

        return count;
    }

    private Task<IReadOnlyList<ShadowCopyInfo>> QueryShadowCopiesAsync(CancellationToken ct) => Task.Run<IReadOnlyList<ShadowCopyInfo>>(
        () =>
        {
            try
            {
                var volumes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var vs = new ManagementObjectSearcher(@"root\cimv2", "SELECT DeviceID, Name FROM Win32_Volume"))
                {
                    foreach (ManagementObject v in vs.Get())
                    {
                        if (v["DeviceID"] is string id && v["Name"] is string name)
                        {
                            volumes[id] = name;
                        }
                    }
                }

                var list = new List<ShadowCopyInfo>();
                using var searcher = new ManagementObjectSearcher(@"root\cimv2", "SELECT ID, DeviceObject, VolumeName, InstallDate FROM Win32_ShadowCopy");
                foreach (ManagementObject s in searcher.Get())
                {
                    ct.ThrowIfCancellationRequested();
                    var device = s["DeviceObject"] as string;
                    var volume = s["VolumeName"] as string;
                    if (string.IsNullOrEmpty(device) || string.IsNullOrEmpty(volume) || !volumes.TryGetValue(volume, out var root))
                    {
                        continue;
                    }

                    var created = s["InstallDate"] is string d ? ManagementDateTimeConverter.ToDateTime(d) : DateTime.MinValue;

                    // \\?\GLOBALROOT\Device\HarddiskVolumeShadowCopyN is readable with ordinary file APIs.
                    var devicePath = device.StartsWith(@"\\?\", StringComparison.Ordinal) ? device : @"\\?\GLOBALROOT" + device;
                    list.Add(new ShadowCopyInfo(s["ID"] as string ?? device, devicePath, root, created));
                }

                return list.OrderByDescending(s => s.Created).ToList();
            }
            catch (ManagementException ex)
            {
                throw new InvalidOperationException($"Windows did not list its snapshots ({ex.Message.Trim()}). Winhance-FS must run as administrator.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("Listing snapshots needs administrator rights.", ex);
            }
        },
        ct);

    // ---------------------------------------------------------------- Recycle Bin

    public Task<IReadOnlyList<RecycleBinItem>> ListRecycleBinAsync(CancellationToken ct = default) => Task.Run<IReadOnlyList<RecycleBinItem>>(
        () =>
        {
            var items = new List<RecycleBinItem>();
            foreach (var folder in _recycleBinFolders())
            {
                ct.ThrowIfCancellationRequested();
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                IEnumerable<string> infos;
                try
                {
                    infos = Directory.EnumerateFiles(folder, "$I*", new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = 0 }).ToList();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _log?.Log(LogLevel.Warning, $"Can't read Recycle Bin folder {folder}: {ex.Message}");
                    continue;
                }

                foreach (var info in infos)
                {
                    var item = TryParseRecycleRecord(info);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }
            }

            return items.OrderByDescending(i => i.Deleted).ToList();
        },
        ct);

    /// <summary>Parse a Recycle Bin <c>$I</c> record (Vista-8: version 1, Windows 10/11: version 2).</summary>
    internal static RecycleBinItem? TryParseRecycleRecord(string infoPath)
    {
        try
        {
            var bytes = File.ReadAllBytes(infoPath);
            if (bytes.Length < 24)
            {
                return null;
            }

            var version = BitConverter.ToInt64(bytes, 0);
            var size = BitConverter.ToInt64(bytes, 8);
            var deleted = DateTime.FromFileTimeUtc(BitConverter.ToInt64(bytes, 16)).ToLocalTime();
            string original;
            if (version == 2 && bytes.Length >= 28)
            {
                var chars = BitConverter.ToInt32(bytes, 24);
                var available = (bytes.Length - 28) / 2;
                original = Encoding.Unicode.GetString(bytes, 28, Math.Clamp(chars, 0, available) * 2);
            }
            else if (version == 1)
            {
                original = Encoding.Unicode.GetString(bytes, 24, Math.Min(520, bytes.Length - 24));
            }
            else
            {
                return null;
            }

            original = original.TrimEnd('\0');
            var nul = original.IndexOf('\0', StringComparison.Ordinal);
            if (nul >= 0)
            {
                original = original[..nul];
            }

            var dir = Path.GetDirectoryName(infoPath)!;
            var data = Path.Combine(dir, "$R" + Path.GetFileName(infoPath)[2..]);
            var isFolder = Directory.Exists(data);
            if (string.IsNullOrWhiteSpace(original) || (!isFolder && !File.Exists(data)))
            {
                return null; // orphan record: the content is gone
            }

            return new RecycleBinItem(original, deleted, size, isFolder, data, infoPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public Task<RestoreResult> RestoreFromRecycleBinAsync(RecycleBinItem item, string? destinationPath = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Task.Run(
            () =>
            {
                // Only move things that really are in one of our Recycle Bin folders, as a matching $I/$R pair.
                var folder = Path.GetDirectoryName(Path.GetFullPath(item.InfoPath));
                var known = _recycleBinFolders().Select(f => Path.GetFullPath(f).TrimEnd('\\'));
                if (folder == null
                    || !known.Contains(folder.TrimEnd('\\'), StringComparer.OrdinalIgnoreCase)
                    || !Path.GetFileName(item.InfoPath).StartsWith("$I", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(Path.GetFullPath(item.DataPath), Path.Combine(folder, "$R" + Path.GetFileName(item.InfoPath)[2..]), StringComparison.OrdinalIgnoreCase))
                {
                    return new RestoreResult(false, null, "That item is not in the Recycle Bin.");
                }

                var target = Path.GetFullPath(destinationPath ?? item.OriginalPath);
                if (File.Exists(target) || Directory.Exists(target))
                {
                    return new RestoreResult(false, null, $"Something named '{Path.GetFileName(target)}' already exists in {Path.GetDirectoryName(target)}. Choose 'Restore To...' to put it somewhere else.");
                }

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    if (item.IsFolder)
                    {
                        if (string.Equals(Path.GetPathRoot(target), Path.GetPathRoot(item.DataPath), StringComparison.OrdinalIgnoreCase))
                        {
                            Directory.Move(item.DataPath, target);
                        }
                        else
                        {
                            CopyDirectory(item.DataPath, target, ct);
                            Directory.Delete(item.DataPath, recursive: true);
                        }
                    }
                    else
                    {
                        File.Move(item.DataPath, target);
                    }

                    try
                    {
                        File.Delete(item.InfoPath);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        _log?.Log(LogLevel.Warning, $"Restored {target} but could not remove its Recycle Bin record: {ex.Message}");
                    }

                    _log?.Log(LogLevel.Info, $"Restored {item.OriginalPath} from the Recycle Bin to {target}");
                    return new RestoreResult(true, target, null);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    return new RestoreResult(false, null, ex.Message);
                }
            },
            ct);
    }

    private static IEnumerable<string> DefaultRecycleBinFolders()
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value;
        if (sid == null)
        {
            return Array.Empty<string>();
        }

        return DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType is DriveType.Fixed or DriveType.Removable)
            .Select(d => Path.Combine(d.RootDirectory.FullName, "$Recycle.Bin", sid))
            .ToList();
    }
}
