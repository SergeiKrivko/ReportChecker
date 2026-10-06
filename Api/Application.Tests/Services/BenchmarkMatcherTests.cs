using FluentAssertions;
using ReportChecker.Application.Services;
using ReportChecker.Models;

namespace Application.Tests.Services;

[TestFixture]
public class BenchmarkMatcherTests
{
    private readonly BenchmarkMatcher _matcher = new();
    private readonly BenchmarkMatchOptions _options = new()
    {
        LineTolerance = 3,
        TitleSimilarityThreshold = 0.6,
        LlmMatching = true,
        LlmConfidenceThreshold = 60,
    };

    private static BenchmarkExpectedIssue Expected(int number, string chapter, int? line, string title)
    {
        return new BenchmarkExpectedIssue
        {
            Number = number,
            Chapter = chapter,
            Line = line,
            Title = title,
            Comment = "",
        };
    }

    private static BenchmarkFoundIssue Found(int index, string chapter, int? line, string title)
    {
        return new BenchmarkFoundIssue
        {
            Index = index,
            Chapter = chapter,
            Line = line,
            Title = title,
            Comment = "",
        };
    }

    [Test]
    public void Match_WithSameChapterAndLine_ShouldMatchDeterministically()
    {
        var outcome = _matcher.Match(
            [Expected(1, "Chapter", 10, "Орфографическая ошибка в слове карова")],
            [Found(0, "Chapter", 10, "Слово карова написано с ошибкой")],
            _options);

        outcome.Pairs.Should().HaveCount(1);
        outcome.Pairs[0].ExpectedNumber.Should().Be(1);
        outcome.Pairs[0].FoundIndex.Should().Be(0);
        outcome.Pairs[0].Method.Should().Be(BenchmarkMatchMethod.Deterministic);
        outcome.NotFoundExpected.Should().BeEmpty();
        outcome.ExtraFound.Should().BeEmpty();
        outcome.Pending.Should().BeNull();
    }

    [Test]
    public void Match_WithDifferentChapter_ShouldNotMatch()
    {
        var outcome = _matcher.Match(
            [Expected(1, "Chapter A", 10, "Ошибка")],
            [Found(0, "Chapter B", 10, "Ошибка")],
            _options);

        outcome.Pairs.Should().BeEmpty();
        outcome.NotFoundExpected.Should().HaveCount(1);
        outcome.ExtraFound.Should().HaveCount(1);
    }

    [Test]
    public void Match_WithLineOutsideTolerance_AndLowSimilarity_ShouldNotMatch()
    {
        var outcome = _matcher.Match(
            [Expected(1, "Chapter", 10, "Орфография")],
            [Found(0, "Chapter", 100, "Совершенно другая тема")],
            _options with { LineTolerance = 3 });

        outcome.Pairs.Should().BeEmpty();
    }

    [Test]
    public void Match_WithLineInsideTolerance_AndHighSimilarity_ShouldMatch()
    {
        var outcome = _matcher.Match(
            [Expected(1, "Chapter", 10, "Неверное согласование числа данных")],
            [Found(0, "Chapter", 12, "Неверное согласование числа данных в предложении")],
            _options);

        outcome.Pairs.Should().HaveCount(1);
        outcome.Pairs[0].FoundIndex.Should().Be(0);
    }

    [Test]
    public void Match_WithDifferentTagsAndCase_ShouldNormalizeBeforeComparing()
    {
        _matcher.Similarity("<b>Ошибка</b> в СЛОВЕ", "ошибка в слове").Should().BeGreaterThan(0.9);
    }

    [Test]
    public void Match_ShouldBeGreedyAndOneToOne()
    {
        // Обе известные ошибки подходят к found-0; он достаётся более похожей (E1),
        // а вторая известная сопоставляется с оставшейся найденной.
        var outcome = _matcher.Match(
        [
            Expected(1, "Chapter", 10, "Ошибка в слове карова"),
            Expected(2, "Chapter", 10, "Ошибка в слове карова молоко"),
        ],
        [
            Found(0, "Chapter", 10, "Ошибка в слове карова молоко"),
            Found(1, "Chapter", 11, "Ошибка в слове карова"),
        ],
        _options);

        outcome.Pairs.Should().HaveCount(2);
        outcome.Pairs.Select(e => e.ExpectedNumber).Should().OnlyHaveUniqueItems();
        outcome.Pairs.Select(e => e.FoundIndex).Should().OnlyHaveUniqueItems();
        outcome.Pairs.Single(e => e.ExpectedNumber == 2).FoundIndex.Should().Be(0);
        outcome.Pairs.Single(e => e.ExpectedNumber == 1).FoundIndex.Should().Be(1);
    }

    [Test]
    public void Match_WhenCandidateRejected_ShouldNotConsumeExpected()
    {
        // E1 лучше всего подходит к found-0, но found-0 достаётся E2 (оценка выше).
        // E1 не должен «сгореть»: он обязан сопоставиться с found-1.
        var outcome = _matcher.Match(
        [
            Expected(1, "Chapter", 10, "Ошибка в слове карова"),
            Expected(2, "Chapter", 10, "Ошибка в слове карова молоко"),
        ],
        [
            Found(0, "Chapter", 10, "Ошибка в слове карова молоко"),
            Found(1, "Chapter", 10, "Ошибка в слове карова"),
        ],
        _options);

        outcome.Pairs.Should().HaveCount(2);
        outcome.Pairs.Single(e => e.ExpectedNumber == 1).FoundIndex.Should().Be(1);
        outcome.Pairs.Single(e => e.ExpectedNumber == 2).FoundIndex.Should().Be(0);
        outcome.NotFoundExpected.Should().BeEmpty();
        outcome.ExtraFound.Should().BeEmpty();
    }

    [Test]
    public void Match_ShouldReturnPendingWhenLlmMatchingEnabled()
    {
        var outcome = _matcher.Match(
            [Expected(1, "Chapter", 10, "Орфография")],
            [Found(0, "Chapter", 50, "Совсем другая формулировка темы")],
            _options);

        outcome.Pairs.Should().BeEmpty();
        outcome.Pending.Should().NotBeNull();
        outcome.Pending!.Expected.Should().HaveCount(1);
        outcome.Pending.Found.Should().HaveCount(1);
    }

    [Test]
    public void Match_ShouldNotReturnPendingWhenLlmMatchingDisabled()
    {
        var outcome = _matcher.Match(
            [Expected(1, "Chapter", 10, "Орфография")],
            [Found(0, "Chapter", 50, "Совсем другая формулировка темы")],
            _options with { LlmMatching = false });

        outcome.Pending.Should().BeNull();
    }

    [Test]
    public void ApplyLlmMatches_ShouldAcceptOnlyConfidentAndUnusedPairs()
    {
        var baseOutcome = _matcher.Match(
        [
            Expected(1, "Chapter", 10, "Первая ошибка"),
            Expected(2, "Chapter", 10, "Вторая ошибка"),
        ],
        [
            Found(0, "Chapter", 90, "Что-то другое"),
            Found(1, "Chapter", 91, "И это другое"),
        ],
        _options);

        var result = _matcher.ApplyLlmMatches(baseOutcome,
        [
            new BenchmarkMatch { ExpectedNumber = 1, FoundIndex = 0, ConfidencePercent = 90, Reason = "ok" },
            new BenchmarkMatch { ExpectedNumber = 2, FoundIndex = 1, ConfidencePercent = 30, Reason = "weak" },
        ], _options);

        result.Pairs.Should().HaveCount(1);
        result.Pairs[0].ExpectedNumber.Should().Be(1);
        result.Pairs[0].Method.Should().Be(BenchmarkMatchMethod.Llm);

        // Отклонённая пара остаётся в остатке как ненайденная и «лишняя».
        result.NotFoundExpected.Select(e => e.Number).Should().Equal(2);
        result.ExtraFound.Select(e => e.Index).Should().Equal(1);
    }

    [Test]
    public void ApplyLlmMatches_WithOutOfRangeIndexes_ShouldIgnoreThem()
    {
        var baseOutcome = _matcher.Match(
            [Expected(1, "Chapter", 10, "Ошибка")],
            [Found(0, "Chapter", 90, "Другое")],
            _options);

        var result = _matcher.ApplyLlmMatches(baseOutcome,
        [
            new BenchmarkMatch { ExpectedNumber = 99, FoundIndex = 0, ConfidencePercent = 100 },
            new BenchmarkMatch { ExpectedNumber = 1, FoundIndex = 99, ConfidencePercent = 100 },
        ], _options);

        result.Pairs.Should().BeEmpty();
    }

    [Test]
    public void Similarity_WithEmptyAndNonEmpty_ShouldBeZero()
    {
        _matcher.Similarity("", "текст").Should().Be(0);
        _matcher.Similarity(null, "текст").Should().Be(0);
        _matcher.Similarity("", "").Should().Be(1);
    }
}
