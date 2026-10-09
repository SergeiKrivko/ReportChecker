using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace Application.Tests.Services;

[TestFixture]
public class BenchmarkServiceTests
{
    private Mock<IBenchmarkRepository> _repository = null!;
    private Mock<IBenchmarkCaseProvider> _caseProvider = null!;
    private Mock<IBenchmarkAiService> _aiService = null!;
    private Mock<ITaskCancellationService> _cancellationService = null!;
    private BenchmarkService _service = null!;

    private BenchmarkResult[]? _collectedResults;
    private BenchmarkRun? _collectedAggregates;
    private BenchmarkUsage? _collectedUsage;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IBenchmarkRepository>();
        _caseProvider = new Mock<IBenchmarkCaseProvider>();
        _aiService = new Mock<IBenchmarkAiService>();
        _cancellationService = new Mock<ITaskCancellationService>();

        var configuration = new Mock<IConfiguration>();
        configuration.Setup(e => e["Benchmarks.MaxParallelRuns"]).Returns("2");

        var scope = new Mock<IServiceScope>();
        scope.Setup(e => e.ServiceProvider).Returns(new Mock<IServiceProvider>().Object);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(e => e.GetService(typeof(IServiceScopeFactory)))
            .Returns(new Mock<IServiceScopeFactory>().Object);
        serviceProvider.Setup(e => e.GetService(typeof(IServiceScope))).Returns(scope.Object);

        _service = new BenchmarkService(
            _repository.Object,
            _caseProvider.Object,
            _aiService.Object,
            new BenchmarkRunLimiter(configuration.Object),
            serviceProvider.Object,
            _cancellationService.Object,
            configuration.Object,
            NullLogger<BenchmarkService>.Instance);
    }

    private static BenchmarkCase Case(params BenchmarkExpectedIssue[] expected)
    {
        return new BenchmarkCase
        {
            Id = "case",
            Name = "Case",
            Format = "Latex",
            EntryFile = "report.tex",
            Expected = expected,
        };
    }

    private static BenchmarkExpectedIssue Expected(int number, int? line, string title, bool checkFix = false,
        PatchLine[]? patch = null)
    {
        return new BenchmarkExpectedIssue
        {
            Number = number,
            Chapter = "Chapter",
            Line = line,
            Title = title,
            Comment = "Комментарий",
            Priority = 2,
            CheckFix = checkFix,
            Patch = patch,
        };
    }

    private static BenchmarkFoundIssue Found(int index, int? line, string title, PatchLine[]? patch = null)
    {
        return new BenchmarkFoundIssue
        {
            Index = index,
            Chapter = "Chapter",
            Line = line,
            Title = title,
            Comment = "Найдено",
            Priority = 2,
            Patch = patch,
        };
    }

    private static Chapter[] Chapters()
    {
        return
        [
            new Chapter { Name = "Chapter", Content = "первая строка\nвторая строка\nтретья строка" },
        ];
    }

    private void SetupCompleteCapture()
    {
        _repository
            .Setup(e => e.SetStatusAsync(It.IsAny<Guid>(), It.IsAny<ProgressStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(e => e.CompleteRunAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<BenchmarkResult>>(),
                It.IsAny<BenchmarkRun>(), It.IsAny<BenchmarkUsage>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, IReadOnlyCollection<BenchmarkResult>, BenchmarkRun, BenchmarkUsage, DateTime,
                CancellationToken>((_, results, aggregates, usage, _, _) =>
            {
                _collectedResults = results.ToArray();
                _collectedAggregates = aggregates;
                _collectedUsage = usage;
            })
            .ReturnsAsync(true);
    }

    [Test]
    public async Task RunAsync_ShouldCreateRowForEveryExpectedAndExtraIssue()
    {
        SetupCompleteCapture();
        var expected = new[]
        {
            Expected(1, 1, "Ошибка в первой строке"),
            Expected(2, 2, "Ошибка во второй строке"),
        };
        _caseProvider.Setup(e => e.GetCase("case")).Returns(Case(expected));
        _caseProvider.Setup(e => e.GetChaptersAsync(It.IsAny<BenchmarkCase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Chapters());
        _aiService.Setup(e => e.FindIssuesAsync(It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkFindResult
            {
                Issues =
                [
                    Found(0, 1, "Ошибка в первой строке"),
                    Found(1, 1, "Совершенно лишняя ошибка"),
                ],
                Usage = new BenchmarkUsage { TotalRequests = 1 },
            });
        _aiService.Setup(e => e.MatchIssuesAsync(It.IsAny<BenchmarkMatchRequest>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkMatchResult());

        await _service.RunAsync(Guid.NewGuid(), "case", Guid.NewGuid(), false);

        var results = _collectedResults!;
        results.Should().HaveCount(3);
        var first = results.Single(e => e.ExpectedNumber == 1);
        first.IsFound.Should().BeTrue();
        first.FoundIndex.Should().Be(0);
        first.MatchingMethod.Should().Be(BenchmarkMatchMethod.Deterministic);

        var second = results.Single(e => e.ExpectedNumber == 2);
        second.IsFound.Should().BeFalse();
        second.FoundIndex.Should().BeNull();
        second.MatchingMethod.Should().Be(BenchmarkMatchMethod.None);

        var extra = results.Single(e => e.ExpectedNumber == null);
        extra.IsFound.Should().BeTrue();
        extra.FoundIndex.Should().Be(1);
        extra.FixMatchStatus.Should().Be(BenchmarkFixMatchStatus.NotApplicable);

        _collectedAggregates!.ExpectedCount.Should().Be(2);
        _collectedAggregates.FoundCount.Should().Be(2);
        _collectedAggregates.MatchedCount.Should().Be(1);
        _collectedAggregates.TitleMatchCount.Should().Be(1);
        _collectedAggregates.PriorityMatchCount.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_ShouldMarkFixAsMatchedWhenPatchEqualsExpected()
    {
        SetupCompleteCapture();
        var patch = new[] { new PatchLine { Number = 2, Content = "исправлено", Type = PatchLineType.Modify } };
        _caseProvider.Setup(e => e.GetCase("case")).Returns(Case(Expected(1, 2, "Ошибка", true, patch)));
        _caseProvider.Setup(e => e.GetChaptersAsync(It.IsAny<BenchmarkCase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Chapters());
        _aiService.Setup(e => e.FindIssuesAsync(It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkFindResult { Issues = [Found(0, 2, "Ошибка", patch)] });

        await _service.RunAsync(Guid.NewGuid(), "case", Guid.NewGuid(), false);

        var row = _collectedResults!.Single();
        row.FixMatchStatus.Should().Be(BenchmarkFixMatchStatus.Matched);
        row.ExpectedFix.Should().Be("первая строка\nисправлено\nтретья строка");
        _collectedAggregates!.FixCheckedCount.Should().Be(1);
        _collectedAggregates.FixMatchCount.Should().Be(1);
    }

    [Test]
    public async Task RunAsync_ShouldNotCheckFixWhenIssueNotFound()
    {
        SetupCompleteCapture();
        var patch = new[] { new PatchLine { Number = 2, Content = "исправлено", Type = PatchLineType.Modify } };
        _caseProvider.Setup(e => e.GetCase("case")).Returns(Case(Expected(1, 2, "Ошибка", true, patch)));
        _caseProvider.Setup(e => e.GetChaptersAsync(It.IsAny<BenchmarkCase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Chapters());
        _aiService.Setup(e => e.FindIssuesAsync(It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkFindResult());

        await _service.RunAsync(Guid.NewGuid(), "case", Guid.NewGuid(), false);

        _collectedResults!.Single().FixMatchStatus.Should().Be(BenchmarkFixMatchStatus.MissingFoundPatch);
        _collectedAggregates!.FixCheckedCount.Should().Be(1);
        _collectedAggregates.FixMatchCount.Should().Be(0);
    }

    [Test]
    public async Task RunAsync_ShouldSumUsageOfFindAndMatchRequests()
    {
        SetupCompleteCapture();
        _caseProvider.Setup(e => e.GetCase("case")).Returns(Case(Expected(1, 1, "Ошибка в первой строке")));
        _caseProvider.Setup(e => e.GetChaptersAsync(It.IsAny<BenchmarkCase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Chapters());
        _aiService.Setup(e => e.FindIssuesAsync(It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkFindResult
            {
                Issues = [Found(0, 90, "Совсем другое")],
                Usage = new BenchmarkUsage { InputTokens = 10, TotalRequests = 1, TotalCost = 0.1m },
            });
        _aiService.Setup(e => e.MatchIssuesAsync(It.IsAny<BenchmarkMatchRequest>(), It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkMatchResult
            {
                Usage = new BenchmarkUsage { InputTokens = 5, TotalRequests = 1, TotalCost = 0.2m },
            });

        await _service.RunAsync(Guid.NewGuid(), "case", Guid.NewGuid(), true);

        _collectedUsage!.InputTokens.Should().Be(15);
        _collectedUsage.TotalRequests.Should().Be(2);
        _collectedUsage.TotalCost.Should().Be(0.3m);
    }

    [Test]
    public async Task RunAsync_ShouldNotCallLlmMatchingWhenDisabled()
    {
        SetupCompleteCapture();
        _caseProvider.Setup(e => e.GetCase("case")).Returns(Case(Expected(1, 1, "Ошибка в первой строке")));
        _caseProvider.Setup(e => e.GetChaptersAsync(It.IsAny<BenchmarkCase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Chapters());
        _aiService.Setup(e => e.FindIssuesAsync(It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkFindResult { Issues = [Found(0, 90, "Совсем другое")] });

        await _service.RunAsync(Guid.NewGuid(), "case", Guid.NewGuid(), false);

        _aiService.Verify(e => e.MatchIssuesAsync(It.IsAny<BenchmarkMatchRequest>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_WhenAiFails_ShouldMarkRunFailed()
    {
        _repository
            .Setup(e => e.SetStatusAsync(It.IsAny<Guid>(), It.IsAny<ProgressStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _caseProvider.Setup(e => e.GetCase("case")).Returns(Case(Expected(1, 1, "Ошибка")));
        _caseProvider.Setup(e => e.GetChaptersAsync(It.IsAny<BenchmarkCase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Chapters());
        _aiService.Setup(e => e.FindIssuesAsync(It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Модель недоступна"));

        var runId = Guid.NewGuid();
        await _service.RunAsync(runId, "case", Guid.NewGuid(), false);

        _repository.Verify(e => e.FailRunAsync(runId, It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(e => e.CompleteRunAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<BenchmarkResult>>(),
            It.IsAny<BenchmarkRun>(), It.IsAny<BenchmarkUsage>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RunAsync_WhenCancelled_ShouldMarkRunCancelled()
    {
        _repository
            .Setup(e => e.SetStatusAsync(It.IsAny<Guid>(), It.IsAny<ProgressStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _caseProvider.Setup(e => e.GetCase("case")).Returns(Case(Expected(1, 1, "Ошибка")));
        _caseProvider.Setup(e => e.GetChaptersAsync(It.IsAny<BenchmarkCase>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var runId = Guid.NewGuid();
        await _service.RunAsync(runId, "case", Guid.NewGuid(), false);

        _repository.Verify(e => e.SetStatusAsync(runId, ProgressStatus.Cancelled, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CreateRunsAsync_WithoutModels_ShouldThrowBadRequest()
    {
        var act = async () => await _service.CreateRunsAsync(new BenchmarkRunRequest { ModelIds = [] });

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateRunsAsync_WithInvalidCase_ShouldThrowBadRequest()
    {
        _caseProvider.Setup(e => e.GetCases()).Returns(
        [
            new BenchmarkCase
            {
                Id = "broken",
                Name = "Broken",
                Format = "Latex",
                ValidationError = "Не задан файл-точка входа",
            },
        ]);

        var act = async () => await _service.CreateRunsAsync(new BenchmarkRunRequest
        {
            CaseIds = ["broken"],
            ModelIds = [Guid.NewGuid()],
        });

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Test]
    public async Task CreateRunsAsync_WithUnknownCase_ShouldThrowNotFound()
    {
        _caseProvider.Setup(e => e.GetCases()).Returns([Case(Expected(1, 1, "Ошибка"))]);

        var act = async () => await _service.CreateRunsAsync(new BenchmarkRunRequest
        {
            CaseIds = ["unknown"],
            ModelIds = [Guid.NewGuid()],
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task CreateRunsAsync_WithoutCases_ShouldThrowNotFound()
    {
        _caseProvider.Setup(e => e.GetCases()).Returns([]);

        var act = async () => await _service.CreateRunsAsync(new BenchmarkRunRequest
        {
            ModelIds = [Guid.NewGuid()],
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task CreateRunsAsync_ShouldCreateRunForEachCaseAndModel()
    {
        var modelA = Guid.NewGuid();
        var modelB = Guid.NewGuid();
        _caseProvider.Setup(e => e.GetCases()).Returns(
        [
            new BenchmarkCase { Id = "case-a", Name = "A", Format = "Latex", Expected = [Expected(1, 1, "x")] },
            new BenchmarkCase
            {
                Id = "case-b",
                Name = "B",
                Format = "Latex",
                Expected = [Expected(1, 1, "y")],
                DisplayMode = BenchmarkCaseDisplayMode.Hidden,
            },
        ]);
        _repository.Setup(e => e.CreateRunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        var runIds = await _service.CreateRunsAsync(new BenchmarkRunRequest
        {
            CaseIds = ["case-a", "case-b"],
            ModelIds = [modelA, modelB],
        });

        runIds.Should().HaveCount(4);
        _repository.Verify(e => e.CreateRunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<LlmReasoningEffort>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Test]
    public async Task CreateRunsAsync_ShouldPassReasoningEffortToRepository()
    {
        _caseProvider.Setup(e => e.GetCases()).Returns([Case(Expected(1, 1, "x"))]);
        _repository.Setup(e => e.CreateRunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        await _service.CreateRunsAsync(new BenchmarkRunRequest
        {
            CaseIds = ["case"],
            ModelIds = [Guid.NewGuid()],
            ReasoningEffort = LlmReasoningEffort.High,
        });

        _repository.Verify(e => e.CreateRunAsync("case", It.IsAny<string>(), It.IsAny<Guid>(),
            LlmReasoningEffort.High, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateRunsAsync_WithoutReasoningEffort_ShouldStoreNone()
    {
        _caseProvider.Setup(e => e.GetCases()).Returns([Case(Expected(1, 1, "x"))]);
        _repository.Setup(e => e.CreateRunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        await _service.CreateRunsAsync(new BenchmarkRunRequest
        {
            CaseIds = ["case"],
            ModelIds = [Guid.NewGuid()],
        });

        _repository.Verify(e => e.CreateRunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(),
            LlmReasoningEffort.None, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RunAsync_ShouldForwardReasoningEffortToAiService()
    {
        SetupCompleteCapture();
        _caseProvider.Setup(e => e.GetCase("case")).Returns(Case(Expected(1, 1, "Ошибка")));
        _caseProvider.Setup(e => e.GetChaptersAsync(It.IsAny<BenchmarkCase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Chapters());
        _aiService.Setup(e => e.FindIssuesAsync(It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<Guid>(),
                It.IsAny<LlmReasoningEffort?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkFindResult { Issues = [Found(0, 1, "Ошибка")] });

        await _service.RunAsync(Guid.NewGuid(), "case", Guid.NewGuid(), false, LlmReasoningEffort.Max);

        _aiService.Verify(e => e.FindIssuesAsync(It.IsAny<IReadOnlyList<Chapter>>(), It.IsAny<Guid>(),
            LlmReasoningEffort.Max, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteRunAsync_WithActiveRun_ShouldCancelThenSoftDelete()
    {
        var runId = Guid.NewGuid();
        _repository.Setup(e => e.GetRunByIdAsync(runId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkRun
            {
                Id = runId,
                CaseId = "case",
                CaseName = "Case",
                ModelId = Guid.NewGuid(),
                Status = ProgressStatus.InProgress,
            });
        _repository.Setup(e => e.SoftDeleteAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        (await _service.DeleteRunAsync(runId)).Should().BeTrue();

        _cancellationService.Verify(e => e.CancelBenchmarkAsync(runId), Times.Once);
        _repository.Verify(e => e.SoftDeleteAsync(runId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteRunAsync_WithCompletedRun_ShouldNotCancel()
    {
        var runId = Guid.NewGuid();
        _repository.Setup(e => e.GetRunByIdAsync(runId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BenchmarkRun
            {
                Id = runId,
                CaseId = "case",
                CaseName = "Case",
                ModelId = Guid.NewGuid(),
                Status = ProgressStatus.Completed,
            });
        _repository.Setup(e => e.SoftDeleteAsync(runId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        (await _service.DeleteRunAsync(runId)).Should().BeTrue();

        _cancellationService.Verify(e => e.CancelBenchmarkAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Test]
    public async Task DeleteRunAsync_ForUnknownRun_ShouldReturnFalse()
    {
        _repository.Setup(e => e.GetRunByIdAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BenchmarkRun?)null);

        (await _service.DeleteRunAsync(Guid.NewGuid())).Should().BeFalse();
    }
}
