using System;
using System.Diagnostics;
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
/// Safety tests for the duplicate finder: it must never report one file as its own duplicate (junctions, overlapping
/// folders, hard links) and must re-verify content right before deleting anything.
/// </summary>
public class DuplicateFinderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "winhance-dupes-" + Guid.NewGuid().ToString("N"));
    private readonly DuplicateFinderService _service = new(Mock.Of<ILogService>());
    private readonly DuplicateScanOptions _options = new() { VerifyWithFullHash = true };

    public DuplicateFinderTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(_root, "*", SearchOption.TopDirectoryOnly))
            {
                if (new DirectoryInfo(dir).Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    Directory.Delete(dir);
                }
            }

            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string Write(string relative, string content, DateTime? modified = null)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        if (modified != null)
        {
            File.SetLastWriteTime(path, modified.Value);
        }

        return path;
    }

    private static void Run(string args)
    {
        using var p = Process.Start(new ProcessStartInfo("cmd.exe", "/c " + args) { CreateNoWindow = true, UseShellExecute = false })!;
        p.WaitForExit();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Scan_FindsRealDuplicates_AndIgnoresDifferentFiles()
    {
        Write(@"a\photo.jpg", new string('p', 5000));
        Write(@"b\photo (1).jpg", new string('p', 5000));
        Write(@"b\other.jpg", new string('q', 5000));

        var result = await _service.ScanForDuplicatesAsync(new[] { _root }, _options);

        result.Groups.Should().ContainSingle();
        result.Groups[0].Files.Select(f => f.Name).Should().BeEquivalentTo("photo.jpg", "photo (1).jpg");
        result.WastedBytes.Should().Be(5000);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Scan_OverlappingFolders_DoNotMakeAFileItsOwnDuplicate()
    {
        Write(@"sub\only.bin", new string('x', 3000));

        var result = await _service.ScanForDuplicatesAsync(new[] { _root, Path.Combine(_root, "sub") }, _options);

        result.Groups.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Scan_DoesNotFollowJunctions()
    {
        Write(@"real\only.bin", new string('x', 3000));
        Run($"mklink /J \"{Path.Combine(_root, "link")}\" \"{Path.Combine(_root, "real")}\"");
        Directory.Exists(Path.Combine(_root, "link", "only.bin").Replace("only.bin", string.Empty)).Should().BeTrue("the test junction must exist");

        var result = await _service.ScanForDuplicatesAsync(new[] { _root }, _options);

        result.Groups.Should().BeEmpty("the junction reaches the same file, which is not a duplicate");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Scan_HardLinks_AreNotDuplicates()
    {
        var original = Write("data.bin", new string('h', 4000));
        Run($"mklink /H \"{Path.Combine(_root, "same-file.bin")}\" \"{original}\"");
        File.Exists(Path.Combine(_root, "same-file.bin")).Should().BeTrue("the test hard link must exist");

        var result = await _service.ScanForDuplicatesAsync(new[] { _root }, _options);

        result.Groups.Should().BeEmpty("deleting one name of a hard-linked file frees nothing");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task DeleteMarked_NeverDeletesEveryCopy()
    {
        Write("one.txt", new string('d', 2000));
        Write("two.txt", new string('d', 2000));
        var scan = await _service.ScanForDuplicatesAsync(new[] { _root }, _options);
        scan.Groups[0].Files.ForEach(f => f.MarkedForDeletion = true);

        var result = await _service.DeleteMarkedAsync(scan.Groups, useRecycleBin: false);

        result.DeletedCount.Should().Be(0);
        result.SkippedCount.Should().Be(2);
        File.Exists(Path.Combine(_root, "one.txt")).Should().BeTrue();
        File.Exists(Path.Combine(_root, "two.txt")).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task DeleteMarked_SkipsFilesChangedSinceTheScan()
    {
        Write("keep.txt", new string('d', 2000));
        var changed = Write("changed.txt", new string('d', 2000));
        var scan = await _service.ScanForDuplicatesAsync(new[] { _root }, _options);
        scan.Groups[0].Files.Single(f => f.Path == changed).MarkedForDeletion = true;
        File.WriteAllText(changed, new string('e', 2000)); // same size, different content

        var result = await _service.DeleteMarkedAsync(scan.Groups, useRecycleBin: false);

        result.DeletedCount.Should().Be(0);
        result.SkippedCount.Should().Be(1);
        File.Exists(changed).Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task DeleteDuplicates_KeepNewest_DeletesOnlyOlderCopies()
    {
        var older = Write(@"x\report.pdf", new string('r', 2500), DateTime.Now.AddDays(-10));
        var newest = Write(@"y\report.pdf", new string('r', 2500), DateTime.Now.AddDays(-1));
        var oldest = Write(@"z\report.pdf", new string('r', 2500), DateTime.Now.AddDays(-30));
        var scan = await _service.ScanForDuplicatesAsync(new[] { _root }, _options);

        var result = await _service.DeleteDuplicatesAsync(scan.Groups, DuplicateKeepStrategy.KeepNewest, useRecycleBin: false);

        result.DeletedCount.Should().Be(2);
        result.BytesRecovered.Should().Be(5000);
        File.Exists(newest).Should().BeTrue();
        File.Exists(older).Should().BeFalse();
        File.Exists(oldest).Should().BeFalse();
    }
}
