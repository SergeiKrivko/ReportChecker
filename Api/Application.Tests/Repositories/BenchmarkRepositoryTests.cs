using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ReportChecker.DataAccess;
using ReportChecker.DataAccess.Entities;
using ReportChecker.DataAccess.Repositories;
using ReportChecker.Models;

namespace Application.Tests.Repositories;

[TestFixture]
public class BenchmarkRepositoryTests
{
    private SqliteConnection _connection = null!;
    private ReportCheckerDbContext _context = null!;
    private BenchmarkRepository _repository = null!;
    private readonly Guid _modelId = Guid.NewGuid();

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ReportCheckerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ReportCheckerDbContext(options);
        await _context.Database.MigrateAsync("20261012150454_BenchmarkRunReasoningEffort");

        await _context.LlmModels.AddAsync(new LlmModelEntity
        {
            Id = _modelId,
            DisplayName = "Test model",
            ModelKey = "test-model",
            CreatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        _repository = new BenchmarkRepository(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static BenchmarkUsage Usage(int requests = 1)
    {
        return new BenchmarkUsage
        {
            InputTokens = 100,
            OutputTokens = 50,
            TotalTokens = 150,
            TotalRequests = requests,
            TotalCost = 0.25m,
        };
    }

    private static BenchmarkRun Aggregates(Guid runId)
    {
        return new BenchmarkRun
        {
            Id = runId,
            CaseId = "case",
            CaseName = "Case",
            ModelId = Guid.Empty,
            ExpectedCount = 4,
            FoundCount = 3,
            MatchedCount = 2,
            TitleMatchCount = 2,
            PriorityMatchCount = 1,
            FixCheckedCount = 2,
            FixMatchCount = 1,
        };
    }

    private static BenchmarkResult Result(Guid runId, int? expectedNumber)
    {
        return new BenchmarkResult
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            CreatedAt = DateTime.UtcNow,
            ExpectedNumber = expectedNumber,
            Chapter = "Chapter",
            IsFound = expectedNumber != null,
            FixMatchStatus = BenchmarkFixMatchStatus.NotApplicable,
        };
    }

    [Test]
    public async Task CreateRunAsync_ShouldCreateQueuedRun()
    {
        var runId = await _repository.CreateRunAsync("case", "Case", _modelId);

        var run = await _repository.GetRunByIdAsync(runId);
        run.Should().NotBeNull();
        run!.Status.Should().Be(ProgressStatus.Queued);
        run.StartedAt.Should().BeNull();
        run.FinishedAt.Should().BeNull();
        run.DeletedAt.Should().BeNull();
    }

    [Test]
    public async Task CreateRunAsync_ShouldStoreReasoningEffort()
    {
        var runId = await _repository.CreateRunAsync("case", "Case", _modelId, LlmReasoningEffort.High);

        var run = await _repository.GetRunByIdAsync(runId);
        run!.ReasoningEffort.Should().Be(LlmReasoningEffort.High);

        var listed = (await _repository.GetRunsAsync()).Single(e => e.Id == runId);
        listed.ReasoningEffort.Should().Be(LlmReasoningEffort.High);
    }

    [Test]
    public async Task CreateRunAsync_WithoutReasoningEffort_ShouldDefaultToNone()
    {
        var runId = await _repository.CreateRunAsync("case", "Case", _modelId);

        var run = await _repository.GetRunByIdAsync(runId);
        run!.ReasoningEffort.Should().Be(LlmReasoningEffort.None);
    }

    [Test]
    public async Task SetStatusAsync_InProgress_ShouldSetStartedAtOnlyOnce()
    {
        var runId = await _repository.CreateRunAsync("case", "Case", _modelId);

        (await _repository.SetStatusAsync(runId, ProgressStatus.InProgress)).Should().BeTrue();
        var first = await _repository.GetRunByIdAsync(runId);
        first!.StartedAt.Should().NotBeNull();
        first.FinishedAt.Should().BeNull();

        await Task.Delay(20);
        await _repository.SetStatusAsync(runId, ProgressStatus.InProgress);
        var second = await _repository.GetRunByIdAsync(runId);
        second!.StartedAt.Should().Be(first.StartedAt);
    }

    [Test]
    public async Task SetStatusAsync_TerminalStatus_ShouldSetFinishedAt()
    {
        var runId = await _repository.CreateRunAsync("case", "Case", _modelId);

        await _repository.SetStatusAsync(runId, ProgressStatus.Cancelled);

        var run = await _repository.GetRunByIdAsync(runId);
        run!.Status.Should().Be(ProgressStatus.Cancelled);
        run.FinishedAt.Should().NotBeNull();
    }

    [Test]
    public async Task SetStatusAsync_ForUnknownRun_ShouldReturnFalse()
    {
        (await _repository.SetStatusAsync(Guid.NewGuid(), ProgressStatus.InProgress)).Should().BeFalse();
    }

    [Test]
    public async Task CompleteRunAsync_ShouldStoreResultsAndAggregates()
    {
        var runId = await _repository.CreateRunAsync("case", "Case", _modelId);
        await _repository.SetStatusAsync(runId, ProgressStatus.InProgress);

        var results = new[] { Result(runId, 1), Result(runId, 2), Result(runId, null) };
        var completed = await _repository.CompleteRunAsync(runId, results, Aggregates(runId), Usage(), DateTime.UtcNow);
        completed.Should().BeTrue();

        var run = await _repository.GetRunByIdAsync(runId);
        run!.Status.Should().Be(ProgressStatus.Completed);
        run.FinishedAt.Should().NotBeNull();
        run.DurationMs.Should().NotBeNull();
        run.ExpectedCount.Should().Be(4);
        run.MatchedCount.Should().Be(2);
        run.TotalCost.Should().Be(0.25m);
        run.Results.Should().HaveCount(3);
        run.Results.Select(e => e.ExpectedNumber).Should().BeEquivalentTo(new int?[] { 1, 2, null });
    }

    [Test]
    public async Task CompleteRunAsync_ForUnknownRun_ShouldReturnFalse()
    {
        var completed = await _repository.CompleteRunAsync(Guid.NewGuid(), [], Aggregates(Guid.NewGuid()),
            Usage(), DateTime.UtcNow);
        completed.Should().BeFalse();
    }

    [Test]
    public async Task FailRunAsync_ShouldStoreReasonAndFinishedAt()
    {
        var runId = await _repository.CreateRunAsync("case", "Case", _modelId);

        (await _repository.FailRunAsync(runId, "boom", DateTime.UtcNow)).Should().BeTrue();

        var run = await _repository.GetRunByIdAsync(runId);
        run!.Status.Should().Be(ProgressStatus.Failed);
        run.FailureReason.Should().Be("boom");
        run.FinishedAt.Should().NotBeNull();
    }

    [Test]
    public async Task SoftDeleteAsync_ShouldHideRunFromQueries()
    {
        var runId = await _repository.CreateRunAsync("case", "Case", _modelId);

        (await _repository.SoftDeleteAsync(runId)).Should().BeTrue();
        (await _repository.SoftDeleteAsync(runId)).Should().BeFalse();

        (await _repository.GetRunByIdAsync(runId)).Should().BeNull();
        var all = await _repository.GetRunsAsync();
        all.Should().BeEmpty();

        var withDeleted = await _repository.GetRunByIdAsync(runId, true);
        withDeleted.Should().NotBeNull();
        withDeleted!.DeletedAt.Should().NotBeNull();
    }

    [Test]
    public async Task GetRunsAsync_ShouldOrderByCreatedAtDescAndFilter()
    {
        var older = await _repository.CreateRunAsync("case-a", "Case A", _modelId);
        await Task.Delay(20);
        var newer = await _repository.CreateRunAsync("case-b", "Case B", _modelId);
        await _repository.SetStatusAsync(older, ProgressStatus.Completed);

        var all = await _repository.GetRunsAsync();
        all.Should().HaveCount(2);
        all[0].Id.Should().Be(newer);

        var byCase = await _repository.GetRunsAsync(caseId: "case-a");
        byCase.Should().HaveCount(1);
        byCase[0].Id.Should().Be(older);

        var byStatus = await _repository.GetRunsAsync(status: ProgressStatus.Completed);
        byStatus.Should().HaveCount(1);

        var byModel = await _repository.GetRunsAsync(modelId: Guid.NewGuid());
        byModel.Should().BeEmpty();
    }

    [Test]
    public async Task GetSummaryAsync_ShouldAggregateByCaseAndModel()
    {
        var first = await _repository.CreateRunAsync("case-a", "Case A", _modelId);
        var second = await _repository.CreateRunAsync("case-a", "Case A", _modelId);
        var otherCase = await _repository.CreateRunAsync("case-b", "Case B", _modelId);

        foreach (var runId in new[] { first, second, otherCase })
        {
            await _repository.SetStatusAsync(runId, ProgressStatus.InProgress);
            await Task.Delay(10);
            await _repository.CompleteRunAsync(runId, [Result(runId, 1)], Aggregates(runId), Usage(), DateTime.UtcNow);
        }

        // Удалённый прогон не должен попадать в сводку.
        var deleted = await _repository.CreateRunAsync("case-a", "Case A", _modelId);
        await _repository.CompleteRunAsync(deleted, [], Aggregates(deleted), Usage(), DateTime.UtcNow);
        await _repository.SoftDeleteAsync(deleted);

        var summary = await _repository.GetSummaryAsync();

        summary.Should().HaveCount(2);
        var group = summary.Single(e => e.CaseId == "case-a");
        group.RunCount.Should().Be(2);
        group.CompletedRunCount.Should().Be(2);
        group.TotalExpected.Should().Be(8);
        group.TotalFound.Should().Be(6);
        group.TotalMatched.Should().Be(4);
        group.TotalExtra.Should().Be(2);
        group.TotalRequests.Should().Be(2);
        group.TotalCost.Should().Be(0.5m);
        group.AvgDurationMs.Should().NotBeNull();
        group.MinDurationMs.Should().NotBeNull();
        group.MaxDurationMs.Should().NotBeNull();
        group.MinDurationMs.Should().BeLessThanOrEqualTo(group.MaxDurationMs!.Value);
        group.LastRunAt.Should().NotBeNull();
        group.ModelDisplayName.Should().Be("Test model");
        group.MatchedShare.Should().BeApproximately(0.5, 0.0001);
        group.FixMatchShare.Should().BeApproximately(0.5, 0.0001);
    }

    [Test]
    public async Task GetSummaryAsync_WithTwoModels_ShouldSplitGroupsAndResolveNames()
    {
        var otherModelId = Guid.NewGuid();
        await _context.LlmModels.AddAsync(new LlmModelEntity
        {
            Id = otherModelId,
            DisplayName = "Other model",
            ModelKey = "other-model",
            CreatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        var first = await _repository.CreateRunAsync("case", "Case", _modelId);
        var second = await _repository.CreateRunAsync("case", "Case", otherModelId);
        await _repository.CompleteRunAsync(first, [], Aggregates(first), Usage(), DateTime.UtcNow);
        await _repository.CompleteRunAsync(second, [], Aggregates(second), Usage(), DateTime.UtcNow);

        var summary = await _repository.GetSummaryAsync();

        summary.Should().HaveCount(2);
        summary.Single(e => e.ModelId == _modelId).ModelDisplayName.Should().Be("Test model");
        summary.Single(e => e.ModelId == otherModelId).ModelDisplayName.Should().Be("Other model");
    }

    [Test]
    public async Task GetSummaryAsync_WithFilter_ShouldRestrictGroups()
    {
        await _repository.CreateRunAsync("case-a", "Case A", _modelId);
        await _repository.CreateRunAsync("case-b", "Case B", _modelId);

        var byCase = await _repository.GetSummaryAsync(caseId: "case-a");
        byCase.Should().HaveCount(1);
        byCase[0].CaseId.Should().Be("case-a");

        var byOtherModel = await _repository.GetSummaryAsync(modelId: Guid.NewGuid());
        byOtherModel.Should().BeEmpty();
    }

    [Test]
    public async Task GetSummaryAsync_WithDifferentReasoning_ShouldSplitGroups()
    {
        var none = await _repository.CreateRunAsync("case", "Case", _modelId, LlmReasoningEffort.None);
        var high = await _repository.CreateRunAsync("case", "Case", _modelId, LlmReasoningEffort.High);
        var highAgain = await _repository.CreateRunAsync("case", "Case", _modelId, LlmReasoningEffort.High);

        foreach (var runId in new[] { none, high, highAgain })
            await _repository.CompleteRunAsync(runId, [], Aggregates(runId), Usage(), DateTime.UtcNow);

        var summary = await _repository.GetSummaryAsync();

        summary.Should().HaveCount(2);
        summary.Should().OnlyContain(e => e.CaseId == "case" && e.ModelId == _modelId);
        summary.Single(e => e.ReasoningEffort == LlmReasoningEffort.None).RunCount.Should().Be(1);
        summary.Single(e => e.ReasoningEffort == LlmReasoningEffort.High).RunCount.Should().Be(2);
    }

    [Test]
    public async Task GetRunsAsync_WithReasoningFilter_ShouldRestrictRuns()
    {
        var none = await _repository.CreateRunAsync("case", "Case", _modelId, LlmReasoningEffort.None);
        var low = await _repository.CreateRunAsync("case", "Case", _modelId, LlmReasoningEffort.Low);

        var all = await _repository.GetRunsAsync();
        all.Should().HaveCount(2);

        var byLow = await _repository.GetRunsAsync(reasoning: LlmReasoningEffort.Low);
        byLow.Should().ContainSingle().Which.Id.Should().Be(low);

        var byNone = await _repository.GetRunsAsync(reasoning: LlmReasoningEffort.None);
        byNone.Should().ContainSingle().Which.Id.Should().Be(none);

        var byMax = await _repository.GetRunsAsync(reasoning: LlmReasoningEffort.Max);
        byMax.Should().BeEmpty();
    }

    [Test]
    public async Task GetRunsAsync_WithReasoningFilter_ShouldCombineWithOtherFilters()
    {
        var matching = await _repository.CreateRunAsync("case-a", "Case A", _modelId, LlmReasoningEffort.High);
        await _repository.CreateRunAsync("case-b", "Case B", _modelId, LlmReasoningEffort.High);
        await _repository.CreateRunAsync("case-a", "Case A", _modelId, LlmReasoningEffort.Low);

        var runs = await _repository.GetRunsAsync(caseId: "case-a", modelId: _modelId,
            reasoning: LlmReasoningEffort.High);

        runs.Should().ContainSingle().Which.Id.Should().Be(matching);
    }
}
