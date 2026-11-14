using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Winhance.Core.Features.Common.Interfaces;
using Winhance.Core.Features.FileManager.Interfaces;
using Winhance.Infrastructure.Features.FileManager.Services;
using Winhance.WPF.Features.FileManager.ViewModels;
using Xunit;

namespace Winhance.Tests.FileManager;

/// <summary>
/// Regression tests for File Manager bugs found during end-to-end testing.
/// </summary>
public class FileManagerRegressionTests : IDisposable
{
    private readonly string _dir;

    public FileManagerRegressionTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "winhance-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string MakeFile(string name, DateTime? modified = null)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, name);
        if (modified.HasValue)
        {
            File.SetLastWriteTime(path, modified.Value);
        }

        return path;
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task BatchRename_DateRule_UsesFileModifiedDate_NotToday()
    {
        var file = MakeFile("photo.jpg", new DateTime(2019, 7, 4, 12, 0, 0));
        var service = new BatchRenameService(Mock.Of<ILogService>());
        var rule = new RenameRule { Type = RenameRuleType.DateTime, DateFormat = "yyyy-MM-dd", DateSource = DateSource.FileModified, Position = TextPosition.Prefix };

        var preview = (await service.PreviewRenameAsync(new[] { file }, new[] { rule })).Single();

        preview.NewName.Should().Be("2019-07-04_photo.jpg");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task BatchRename_RuleViewModel_SendsEverySetting_ToTheRenamer()
    {
        var file = MakeFile("IMG_0001.jpeg");
        var service = new BatchRenameService(Mock.Of<ILogService>());
        var rules = new[]
        {
            new RenameRuleViewModel { RuleType = RenameRuleType.ChangeExtension, NewExtension = "jpg", Order = 1 },
            new RenameRuleViewModel { RuleType = RenameRuleType.Counter, CounterStart = 7, CounterPadding = 2, CounterPrefix = "-", Position = TextPosition.Suffix, Order = 2 },
        }.Select(r => r.ToRule());

        var preview = (await service.PreviewRenameAsync(new[] { file }, rules)).Single();

        preview.NewName.Should().Be("IMG_0001-07.jpg");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task SpaceRecovery_ReportsTotals_AndNoDuplicateLocations()
    {
        var service = new OrganizerService(Mock.Of<ILogService>());
        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory)!.TrimEnd('\\');

        var analysis = await service.AnalyzeSpaceRecoveryAsync(systemDrive);

        analysis.RecoverableSpace.Should().Be(analysis.TotalRecoverableSize);
        analysis.TotalRecoverableSize.Should().Be(analysis.Opportunities.Sum(o => o.Size));
        analysis.Opportunities.Select(o => o.Path).Should().OnlyHaveUniqueItems(o => o.ToUpperInvariant());
        analysis.Opportunities.All(o => o.RecommendedAction == o.Action).Should().BeTrue();
        analysis.Opportunities.Where(o => o.Action == RecoveryAction.Relocate).All(o => !o.IsSafeToClean).Should().BeTrue("relocatable folders must never be auto-cleaned");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task SpaceRecovery_OnlyReportsFoldersOnTheRequestedDrive()
    {
        var service = new OrganizerService(Mock.Of<ILogService>());
        var otherDrive = Enumerable.Range('D', 23).Select(c => $"{(char)c}:")
            .First(d => !string.Equals(d + "\\", Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)), StringComparison.OrdinalIgnoreCase));

        var analysis = await service.AnalyzeSpaceRecoveryAsync(otherDrive);

        analysis.Opportunities.All(o => o.Path.StartsWith(otherDrive, StringComparison.OrdinalIgnoreCase)).Should().BeTrue("folders on other drives must not be reported");
    }

    [Fact]
    [Trait("Category", "Backend")]
    public async Task Organizer_Apply_SendsTheAnalyzedFiles_ToTheService()
    {
        var source = MakeFile("report.pdf");
        var analyzed = new OrganizationPlan
        {
            SourcePath = _dir,
            Strategy = OrganizationStrategy.ByType,
            Categories = new[]
            {
                new OrganizationCategory
                {
                    Name = "Documents",
                    DestinationFolder = Path.Combine(_dir, "Documents"),
                    FileCount = 1,
                    Items = new[] { new OrganizationItem { SourcePath = source, FileName = "report.pdf", DestinationPath = Path.Combine(_dir, "Documents", "report.pdf") } },
                },
            },
            TotalFiles = 1,
        };
        OrganizationPlan? executed = null;
        var organizer = new Mock<IOrganizerService>();
        organizer.Setup(o => o.AnalyzeAsync(It.IsAny<string>(), It.IsAny<OrganizationStrategy>(), It.IsAny<CancellationToken>())).ReturnsAsync(analyzed);
        organizer.Setup(o => o.ExecuteAsync(It.IsAny<OrganizationPlan>(), It.IsAny<CancellationToken>()))
            .Callback<OrganizationPlan, CancellationToken>((p, _) => executed = p)
            .ReturnsAsync(new OrganizationResult { Success = true, FilesOrganized = 1, TransactionId = "t1" });
        var vm = new OrganizerViewModel(organizer.Object) { SourcePath = _dir, SelectedStrategy = OrganizationStrategy.ByType };

        await vm.AnalyzeAsync();
        await vm.ApplyOrganizationCommand.ExecuteAsync(null);

        executed.Should().NotBeNull();
        executed!.Categories.SelectMany(c => c.Items).Select(i => i.SourcePath).Should().Equal(source);
    }
}
