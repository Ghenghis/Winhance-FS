using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Xunit;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Safety tests for <see cref="OrganizerService.RelocateModelsAsync"/> (Space Recovery → Relocate).
/// </summary>
public class ModelRelocationTests : IDisposable
{
    private readonly string _root;
    private readonly string _source;
    private readonly string _destination;
    private readonly OrganizerService _service = new(Mock.Of<ILogService>());

    public ModelRelocationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "winhance-relocate-" + Guid.NewGuid().ToString("N"));
        _source = Path.Combine(_root, "models");
        _destination = Path.Combine(_root, "moved", "models");
        Directory.CreateDirectory(Path.Combine(_source, "publisher", "model-a"));
        File.WriteAllText(Path.Combine(_source, "publisher", "model-a", "weights.gguf"), new string('x', 4096));
        File.WriteAllText(Path.Combine(_source, "index.json"), "{}");
    }

    public void Dispose()
    {
        try
        {
            // Remove the junction first so the recursive delete never follows it.
            if (Directory.Exists(_source) && new DirectoryInfo(_source).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                Directory.Delete(_source);
            }

            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Relocate_MovesEverything_AndLeavesWorkingJunction()
    {
        var result = await _service.RelocateModelsAsync(_source, _destination, createSymlinks: true);

        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
        result.BytesProcessed.Should().Be(4096 + 2);
        File.Exists(Path.Combine(_destination, "publisher", "model-a", "weights.gguf")).Should().BeTrue();
        new DirectoryInfo(_source).Attributes.HasFlag(FileAttributes.ReparsePoint).Should().BeTrue();

        // Apps that still use the old path keep working through the junction.
        File.ReadAllText(Path.Combine(_source, "index.json")).Should().Be("{}");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Relocate_AcrossDrives_CopiesVerifiesAndRemovesSource()
    {
        // A SUBST drive letter has a different root, so the service takes the cross-volume copy path
        // (Directory.Move would throw here). Skipped quietly if no free letter or SUBST is unavailable.
        var letter = "WVUTSRQP".Select(c => c + ":").FirstOrDefault(d => !Directory.Exists(d + "\\"));
        var substRoot = Path.Combine(_root, "otherdrive");
        Directory.CreateDirectory(substRoot);
        if (letter == null || RunSubst($"{letter} \"{substRoot}\"") != 0)
        {
            return;
        }

        try
        {
            var target = letter + "\\models";
            var result = await _service.RelocateModelsAsync(_source, target, createSymlinks: true);

            result.Errors.Should().BeEmpty();
            result.Success.Should().BeTrue();
            File.Exists(Path.Combine(substRoot, "models", "publisher", "model-a", "weights.gguf")).Should().BeTrue();
            new DirectoryInfo(_source).Attributes.HasFlag(FileAttributes.ReparsePoint).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(_source) && new DirectoryInfo(_source).Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                Directory.Delete(_source);
            }

            RunSubst($"{letter} /D");
        }
    }

    private static int RunSubst(string args)
    {
        using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("subst.exe", args)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        p.WaitForExit();
        return p.ExitCode;
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Relocate_RefusesNonEmptyDestination_AndMovesNothing()
    {
        Directory.CreateDirectory(_destination);
        File.WriteAllText(Path.Combine(_destination, "existing.txt"), "keep");

        var result = await _service.RelocateModelsAsync(_source, _destination, createSymlinks: true);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("must be empty"));
        File.Exists(Path.Combine(_source, "index.json")).Should().BeTrue();
        Directory.EnumerateFileSystemEntries(_destination).Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Relocate_RefusesDestinationInsideSource()
    {
        var result = await _service.RelocateModelsAsync(_source, Path.Combine(_source, "inner"), createSymlinks: true);

        result.Success.Should().BeFalse();
        Directory.EnumerateFileSystemEntries(_source).Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Relocate_Twice_RefusesAlreadyLinkedSource()
    {
        (await _service.RelocateModelsAsync(_source, _destination, createSymlinks: true)).Success.Should().BeTrue();

        var second = await _service.RelocateModelsAsync(_source, Path.Combine(_root, "other"), createSymlinks: true);

        second.Success.Should().BeFalse();
        second.Errors.Should().Contain(e => e.Contains("already a link"));
        Directory.EnumerateFiles(_destination, "*", SearchOption.AllDirectories).Should().HaveCount(2);
    }
}
