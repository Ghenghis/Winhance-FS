using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Xunit;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Forensics: alternate data streams, content/entropy verdicts, previous versions (with a fake snapshot folder) and
/// Recycle Bin restore (with a fake Recycle Bin folder). Nothing here touches the real Recycle Bin or real snapshots.
/// </summary>
public class ForensicsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "winhance-forensics-" + Guid.NewGuid().ToString("N"));
    private readonly List<ShadowCopyInfo> _shadows = new();
    private readonly string _bin;
    private readonly ForensicsService _service;

    public ForensicsTests()
    {
        Directory.CreateDirectory(_root);
        _bin = Path.Combine(_root, "bin", "S-1-5-21-test");
        Directory.CreateDirectory(_bin);
        _service = new ForensicsService(null, _ => Task.FromResult<IReadOnlyList<ShadowCopyInfo>>(_shadows), () => new[] { _bin });
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

    private string Path_(string relative) => Path.Combine(_root, relative);

    private static byte[] RandomBytes(int length, int seed = 7)
    {
        var bytes = new byte[length];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }

    private static byte[] FakeProgram(int length = 4096)
    {
        var bytes = RandomBytes(length, 3);
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BitConverter.GetBytes(0x80).CopyTo(bytes, 0x3C);
        new byte[] { 0x50, 0x45, 0, 0 }.CopyTo(bytes, 0x80);
        return bytes;
    }

    // ------------------------------------------------------------------ Alternate data streams

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Streams_AreFound_Classified_Previewed_AndRemovedWithoutTouchingTheFile()
    {
        var file = Path_("download.pdf");
        File.WriteAllText(file, "main content");
        File.WriteAllText(file + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\nHostUrl=https://example.com/download.pdf\r\n");
        File.WriteAllText(file + ":notes", "hidden text here");
        File.WriteAllBytes(file + ":payload", FakeProgram());

        var scan = await _service.ScanStreamsAsync(_root, recursive: true);

        scan.VolumeSupportsStreams.Should().BeTrue();
        scan.Streams.Select(s => s.StreamName).Should().BeEquivalentTo("Zone.Identifier", "notes", "payload");
        scan.Streams[0].Kind.Should().Be(StreamKind.HiddenExecutable, "the most worrying stream is listed first");
        var zone = scan.Streams.Single(s => s.StreamName == "Zone.Identifier");
        zone.Kind.Should().Be(StreamKind.MarkOfTheWeb);
        zone.Description.Should().Contain("the internet").And.Contain("example.com");
        _service.ReadZoneIdentifier(file)!.ZoneId.Should().Be(3);

        var notes = scan.Streams.Single(s => s.StreamName == "notes");
        notes.Kind.Should().Be(StreamKind.HiddenData);
        (await _service.ReadStreamPreviewAsync(notes)).Should().Be("hidden text here");

        var removed = await _service.RemoveStreamsAsync(new[] { notes, zone });

        removed.Succeeded.Should().Be(2);
        removed.Errors.Should().BeEmpty();
        File.ReadAllText(file).Should().Be("main content");
        (await _service.ScanStreamsAsync(file, recursive: false)).Streams.Select(s => s.StreamName).Should().Equal("payload");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task RemoveStreams_RefusesNamesThatCouldAddressTheFileItself()
    {
        var file = Path_("keep.txt");
        File.WriteAllText(file, "precious");

        var result = await _service.RemoveStreamsAsync(new[]
        {
            new AlternateStream(file, string.Empty, 0, StreamKind.HiddenData, string.Empty),
            new AlternateStream(file, "$DATA:x", 0, StreamKind.HiddenData, string.Empty),
        });

        result.Succeeded.Should().Be(0);
        result.Errors.Should().HaveCount(2);
        File.ReadAllText(file).Should().Be("precious");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void ZoneIdentifier_IsParsed()
    {
        var zone = ForensicsService.ParseZoneIdentifier("[ZoneTransfer]\nZoneId=3\nReferrerUrl=https://ref.example/\nHostUrl=https://host.example/f.zip\n");

        zone.ZoneId.Should().Be(3);
        zone.ZoneName.Should().Be("the internet");
        zone.HostUrl.Should().Be("https://host.example/f.zip");
        zone.ReferrerUrl.Should().Be("https://ref.example/");
    }

    // ------------------------------------------------------------------ Entropy and file types

    [Fact]
    [Trait("Category", "Backend")]
    public void Entropy_IsZeroForConstantData_AndEightForUniformData()
    {
        ForensicsService.CalculateEntropy(new byte[1000]).Should().Be(0);
        ForensicsService.CalculateEntropy(Enumerable.Range(0, 256 * 16).Select(i => (byte)i).ToArray()).Should().BeApproximately(8, 1e-9);
    }

    [Theory]
    [Trait("Category", "Backend")]
    [InlineData("invoice.docx", "random", FileVerdict.LikelyEncrypted)]
    [InlineData("notes.txt", "random", FileVerdict.LikelyEncrypted)]
    [InlineData("photo.jpg.locky", "random", FileVerdict.LikelyEncrypted)]
    [InlineData("holiday.pdf", "program", FileVerdict.DisguisedExecutable)]
    [InlineData("readme.txt", "text", FileVerdict.Normal)]
    [InlineData("image.jpg", "png", FileVerdict.ExtensionMismatch)]
    [InlineData("setup.exe", "program", FileVerdict.Compressed)]
    [InlineData("archive.zip", "zip", FileVerdict.Compressed)]
    [InlineData("blob.unknownext", "random", FileVerdict.Compressed)]
    [InlineData("text-mz.txt", "mztext", FileVerdict.Normal)]
    public async Task Inspect_GivesTheRightVerdict(string name, string content, FileVerdict expected)
    {
        var path = Path_(name);
        var bytes = content switch
        {
            "random" => RandomBytes(64 * 1024),
            "program" => FakeProgram(64 * 1024),
            "text" => Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. ", 500))),
            "png" => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(RandomBytes(8192)).ToArray(),
            "zip" => new byte[] { 0x50, 0x4B, 0x03, 0x04 }.Concat(RandomBytes(64 * 1024)).ToArray(),
            "mztext" => Encoding.ASCII.GetBytes("MZ is a fine pair of letters to start a note with. " + new string('a', 2000)),
            _ => throw new ArgumentOutOfRangeException(nameof(content)),
        };
        File.WriteAllBytes(path, bytes);

        var result = await _service.InspectFileAsync(path);

        result.Verdict.Should().Be(expected, result.Reason);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task EntropyScan_ReturnsOnlySuspiciousFiles_WorstFirst()
    {
        Directory.CreateDirectory(Path_("docs"));
        File.WriteAllBytes(Path_(@"docs\a.docx"), RandomBytes(32 * 1024));
        File.WriteAllBytes(Path_(@"docs\b.pdf"), FakeProgram(8192));
        File.WriteAllText(Path_(@"docs\c.txt"), string.Concat(Enumerable.Repeat("plain text ", 400)));

        var scan = await _service.ScanEntropyAsync(_root, new EntropyScanOptions());

        scan.FilesScanned.Should().Be(3);
        scan.Files.Select(f => System.IO.Path.GetFileName(f.Path)).Should().Equal("b.pdf", "a.docx");
    }

    // ------------------------------------------------------------------ Previous versions

    private (string Volume, string Current) MakeVolumeWithSnapshots()
    {
        var volume = Path_("vol") + "\\";
        var current = Path.Combine(volume, "docs", "plan.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(current)!);
        File.WriteAllText(current, "version 3 (today)");
        var now = File.GetLastWriteTime(current);

        void Snapshot(string name, DateTime created, string? content, DateTime modified)
        {
            var device = Path_(name);
            var copy = Path.Combine(device, "docs", "plan.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            if (content != null)
            {
                File.WriteAllText(copy, content);
                File.SetLastWriteTime(copy, modified);
            }

            _shadows.Add(new ShadowCopyInfo(name, device, volume, created));
        }

        Snapshot("shadow1", now.AddDays(-7), "version 1", now.AddDays(-8));
        Snapshot("shadow2", now.AddDays(-3), "version 2 (longer)", now.AddDays(-4));
        Snapshot("shadow3", now.AddDays(-2), "version 2 (longer)", now.AddDays(-4)); // same version again: listed once
        Snapshot("shadow4", now.AddMinutes(1), "version 3 (today)", now);
        return (volume, current);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task PreviousVersions_AreListedOncePerDistinctVersion_NewestFirst()
    {
        var (_, current) = MakeVolumeWithSnapshots();

        var versions = await _service.GetPreviousVersionsAsync(current);

        versions.Select(v => v.Snapshot.Id).Should().Equal("shadow4", "shadow3", "shadow1");
        versions[0].State.Should().Be(VersionState.SameAsCurrent);
        versions[1].State.Should().Be(VersionState.Different);
        versions[1].SnapshotPath.Should().Be(Path.Combine(Path_("shadow3"), "docs", "plan.txt"));
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task RestorePreviousVersion_CopiesToANewFile_AndNeverOverwritesUnlessAsked()
    {
        var (_, current) = MakeVolumeWithSnapshots();
        var old = (await _service.GetPreviousVersionsAsync(current)).Single(v => v.Snapshot.Id == "shadow1");

        var refused = await _service.RestorePreviousVersionAsync(old, current);
        refused.Success.Should().BeFalse();
        File.ReadAllText(current).Should().Be("version 3 (today)");

        var copy = Path_(@"vol\docs\plan (restored).txt");
        var restored = await _service.RestorePreviousVersionAsync(old, copy);
        restored.Success.Should().BeTrue(restored.Error);
        File.ReadAllText(copy).Should().Be("version 1");

        var replaced = await _service.RestorePreviousVersionAsync(old, current, overwrite: true);
        replaced.Success.Should().BeTrue(replaced.Error);
        File.ReadAllText(current).Should().Be("version 1");
        File.Exists(current + ".winhance-restore").Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task PreviousVersions_FindDeletedFiles_AndRestoreFolders()
    {
        var (volume, current) = MakeVolumeWithSnapshots();
        File.Delete(current);

        var versions = await _service.GetPreviousVersionsAsync(current);
        versions.Should().OnlyContain(v => v.State == VersionState.DeletedNow);

        var folder = (await _service.GetPreviousVersionsAsync(Path.Combine(volume, "docs"))).First();
        folder.IsFolder.Should().BeTrue();
        var target = Path.Combine(volume, "docs-restored");
        (await _service.RestorePreviousVersionAsync(folder, target)).Success.Should().BeTrue();
        File.ReadAllText(Path.Combine(target, "plan.txt")).Should().Be("version 3 (today)");
    }

    // ------------------------------------------------------------------ Recycle Bin

    private RecycleBinItem Recycle(string originalPath, string content, string id = "ABC123")
    {
        var ext = Path.GetExtension(originalPath);
        File.WriteAllText(Path.Combine(_bin, "$R" + id + ext), content);
        using (var w = new BinaryWriter(File.Create(Path.Combine(_bin, "$I" + id + ext))))
        {
            w.Write(2L);
            w.Write((long)content.Length);
            w.Write(new DateTime(2026, 11, 20, 9, 30, 0, DateTimeKind.Local).ToFileTimeUtc());
            w.Write(originalPath.Length + 1);
            w.Write(Encoding.Unicode.GetBytes(originalPath + "\0"));
        }

        return _service.ListRecycleBinAsync().Result.Single(i => i.InfoPath.EndsWith("$I" + id + ext, StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task RecycleBin_ItemsAreParsed_AndRestoredToTheirOriginalPlace()
    {
        var original = Path_(@"projects\gone\budget.xlsx");
        var item = Recycle(original, "spreadsheet bytes");

        item.OriginalPath.Should().Be(original);
        item.Name.Should().Be("budget.xlsx");
        item.Size.Should().Be("spreadsheet bytes".Length);
        item.Deleted.Should().Be(new DateTime(2026, 11, 20, 9, 30, 0));

        var result = await _service.RestoreFromRecycleBinAsync(item);

        result.Success.Should().BeTrue(result.Error);
        File.ReadAllText(original).Should().Be("spreadsheet bytes", "the missing folder is recreated");
        File.Exists(item.InfoPath).Should().BeFalse();
        (await _service.ListRecycleBinAsync()).Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task RecycleBin_RestoreNeverOverwrites_AndRefusesItemsOutsideTheBin()
    {
        var original = Path_("report.docx");
        File.WriteAllText(original, "newer file at the same place");
        var item = Recycle(original, "older deleted copy");

        var refused = await _service.RestoreFromRecycleBinAsync(item);
        refused.Success.Should().BeFalse();
        File.ReadAllText(original).Should().Be("newer file at the same place");

        var elsewhere = Path_(@"restored\report.docx");
        (await _service.RestoreFromRecycleBinAsync(item, elsewhere)).Success.Should().BeTrue();
        File.ReadAllText(elsewhere).Should().Be("older deleted copy");

        var victim = Path_("not-in-bin.txt");
        File.WriteAllText(victim, "x");
        var forged = new RecycleBinItem(Path_("moved.txt"), DateTime.Now, 1, false, victim, Path_("$Ifake.txt"));
        (await _service.RestoreFromRecycleBinAsync(forged)).Success.Should().BeFalse();
        File.Exists(victim).Should().BeTrue();
    }
}
