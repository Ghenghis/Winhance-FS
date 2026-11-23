using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Winhance.WPF.Features.Common.Extensions.DI;
using Winhance.WPF.Features.FileManager.ViewModels;

namespace Winhance.Tests.FileManager;

/// <summary>The native engine keeps one index per process, so these tests run one at a time.</summary>
[CollectionDefinition("NexusEngine", DisableParallelization = true)]
public class NexusEngineCollection
{
}

/// <summary>
/// End-to-end tests of the Rust search engine (nexus_core.dll) through the C# service and the Search tab.
/// Every test indexes a temporary folder and saves to a temporary file; the user's real index is never touched.
/// </summary>
[Collection("NexusEngine")]
public sealed class NexusSearchTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "winhance-nexus-" + Guid.NewGuid().ToString("N"));

    public NexusSearchTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Projects", "winhance", "src"));
        Directory.CreateDirectory(Path.Combine(_root, "Photos", "2024 Trip"));
        File.WriteAllText(Path.Combine(_root, "Projects", "winhance", "README.md"), "readme");
        File.WriteAllText(Path.Combine(_root, "Projects", "winhance", "src", "main.rs"), "fn main() {}");
        File.WriteAllBytes(Path.Combine(_root, "Photos", "2024 Trip", "IMG_0001.jpg"), new byte[4096]);
        File.WriteAllBytes(Path.Combine(_root, "Photos", "2024 Trip", "IMG_0002.JPG"), new byte[10]);
        File.WriteAllText(Path.Combine(_root, "Photos", "Tax Return 2024.pdf"), "pdf");
        File.WriteAllText(Path.Combine(_root, "Photos", "Résumé.docx"), "cv");
    }

    private string IndexFile => Path.Combine(_root, "index", "search-index.bin");

    private NexusIndexerService NewService() => new(null, IndexFile);

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

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Build_Search_And_Reload_From_Disk()
    {
        var service = NewService();
        service.IsAvailable.Should().BeTrue(service.UnavailableReason);
        service.EngineVersion.Should().Be("1.5.0");

        var progress = new SyncProgress();
        var info = await service.BuildIndexAsync(new[] { _root }, progress);

        info.Files.Should().Be(6);
        info.Folders.Should().Be(5, "Projects, winhance, src, Photos and 2024 Trip");
        info.Roots.Should().ContainSingle().Which.Method.Should().Be("scan");
        File.Exists(IndexFile).Should().BeTrue("the index is saved for the next start");
        service.HasIndex.Should().BeTrue();

        var tax = await service.SearchAsync("tax 2024");
        tax.Total.Should().Be(1);
        var hit = tax.Results.Single();
        hit.Name.Should().Be("Tax Return 2024.pdf");
        hit.Folder.Should().Be(Path.Combine(_root, "Photos"));
        hit.Extension.Should().Be("pdf");
        hit.Modified.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(5));

        (await service.SearchAsync("*.jpg")).Results.Select(r => r.Name).Should().BeEquivalentTo("IMG_0001.jpg", "IMG_0002.JPG");
        (await service.SearchAsync("img ext:jpg")).Results.Single(r => r.Name == "IMG_0001.jpg").Size.Should().Be(4096);
        (await service.SearchAsync(@"winhance\ .rs")).Results.Single().Name.Should().Be("main.rs");
        (await service.SearchAsync("2024 folder:")).Results.Single().IsDirectory.Should().BeTrue();
        (await service.SearchAsync("résumé")).Results.Single().Name.Should().Be("Résumé.docx");
        (await service.SearchAsync("RESUME")).Total.Should().Be(0, "accents are not folded");
        (await service.SearchAsync("nothing-matches-this")).Results.Should().BeEmpty();
        (await service.SearchAsync("   ")).Total.Should().Be(0);

        var largest = await service.SearchAsync(string.Empty, 2, default, NexusSort.Size);
        largest.Total.Should().Be(11, "an empty query sorted by size lists every file and folder");
        largest.Results.Select(r => r.Name).Should().Equal("IMG_0001.jpg", "main.rs");
        (await service.SearchAsync("ext:jpg", 5, default, NexusSort.Size)).Results.First().Size.Should().Be(4096);

        // A second service (as after a restart) loads the saved file.
        var reloaded = NewService();
        var loaded = await reloaded.LoadSavedIndexAsync();
        loaded.Should().NotBeNull();
        loaded!.Entries.Should().Be(info.Entries);
        File.Delete(Path.Combine(_root, "Photos", "Tax Return 2024.pdf"));
        (await reloaded.SearchAsync("tax")).Results.Single().Missing.Should().BeTrue("deleted after indexing");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Build_Errors_Are_Reported_And_Cancellation_Throws()
    {
        var service = NewService();
        var missing = () => service.BuildIndexAsync(new[] { Path.Combine(_root, "no-such-folder") });
        (await missing.Should().ThrowAsync<InvalidOperationException>()).Which.Message.Should().Contain("no-such-folder");

        var none = () => service.BuildIndexAsync(Array.Empty<string>());
        await none.Should().ThrowAsync<ArgumentException>();

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var cancel = () => service.BuildIndexAsync(new[] { _root }, null, cancelled.Token);
        await cancel.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Corrupt_Saved_Index_Is_Ignored()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(IndexFile)!);
        File.WriteAllText(IndexFile, "not an index");
        var service = NewService();
        (await service.LoadSavedIndexAsync()).Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task SearchTab_Builds_From_Added_Folder_And_Lists_Results()
    {
        var vm = new FastSearchViewModel(NewService());
        vm.IsEngineAvailable.Should().BeTrue();
        foreach (var drive in vm.Drives)
        {
            drive.IsSelected = false;
        }

        vm.NewFolder = _root;
        vm.AddFolderCommand.Execute(null);
        vm.Folders.Should().ContainSingle();
        vm.SelectedRoots.Should().Equal(_root);

        await vm.BuildIndexCommand.ExecuteAsync(null);
        vm.HasIndex.Should().BeTrue(vm.StatusMessage);
        vm.StatusMessage.Should().StartWith("Indexed");
        vm.IndexSummary.Should().Contain("items").And.Contain(_root);

        vm.Query = "readme";
        await vm.SearchNowCommand.ExecuteAsync(null);
        vm.Results.Should().ContainSingle().Which.Name.Should().Be("README.md");
        vm.ResultSummary.Should().StartWith("1 match ");
        vm.SelectedResult.Should().NotBeNull();
        vm.Results[0].SizeText.Should().Be("6 B");

        vm.Query = "img";
        await vm.SearchNowCommand.ExecuteAsync(null);
        vm.Results.Should().HaveCount(2);
        vm.ResultSummary.Should().StartWith("2 matches");

        vm.Query = string.Empty;
        vm.SelectedSort = FastSearchViewModel.SortChoices.Single(s => s.Sort == NexusSort.Size);
        await vm.SearchNowCommand.ExecuteAsync(null);
        vm.Results.First().Name.Should().Be("IMG_0001.jpg", "largest first, even with an empty search box");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public void CompositionRoot_SearchTab_GetsTheNativeEngine()
    {
        using var host = CompositionRoot.CreateWinhanceHost().Build();
        var engine = host.Services.GetRequiredService<INexusIndexerService>();
        engine.Should().BeOfType<NexusIndexerService>();
        engine.Should().BeSameAs(host.Services.GetRequiredService<INexusIndexerService>(), "one engine per app");
        engine.IsAvailable.Should().BeTrue(engine.UnavailableReason);
        engine.IndexFilePath.Should().EndWith(Path.Combine("Winhance-FS", "search-index.bin"));

        var fileManager = host.Services.GetRequiredService<FileManagerViewModel>();
        fileManager.SelectSearchTabCommand.Execute(null);
        fileManager.CurrentView.Should().BeSameAs(fileManager.FastSearchViewModel);
        fileManager.IsSearchTabSelected.Should().BeTrue();
        fileManager.IsBrowserTabSelected.Should().BeFalse();
        fileManager.FastSearchViewModel!.IsEngineAvailable.Should().BeTrue();
    }

    private sealed class SyncProgress : IProgress<long>
    {
        public long Last { get; private set; }

        public void Report(long value) => Last = value;
    }
}
