using FluentAssertions;
using ReportChecker.Application.Services;
using ReportChecker.Models;

namespace Application.Tests.Services;

[TestFixture]
public class BenchmarkFixComparerTests
{
    private readonly BenchmarkFixComparer _comparer = new();
    private const string Chapter = "первая строка\nвторая строка\nтретья строка";

    private static PatchLine Modify(int number, string content)
    {
        return new PatchLine { Number = number, Content = content, Type = PatchLineType.Modify };
    }

    private static PatchLine Add(int number, string content)
    {
        return new PatchLine { Number = number, Content = content, Type = PatchLineType.Add };
    }

    private static PatchLine Delete(int number)
    {
        return new PatchLine { Number = number, Type = PatchLineType.Delete };
    }

    private static BenchmarkExpectedIssue Expected(int number, bool checkFix, PatchLine[]? patch)
    {
        return new BenchmarkExpectedIssue
        {
            Number = number,
            Chapter = "Chapter",
            Title = "Ошибка",
            Comment = "",
            CheckFix = checkFix,
            Patch = patch,
        };
    }

    private static BenchmarkFoundIssue Found(PatchLine[]? patch)
    {
        return new BenchmarkFoundIssue
        {
            Index = 0,
            Chapter = "Chapter",
            Title = "Ошибка",
            Comment = "",
            Patch = patch,
        };
    }

    [Test]
    public void ApplyPatch_WithModify_ShouldReplaceLineInPlace()
    {
        var result = _comparer.ApplyPatch(Chapter, [Modify(2, "изменённая строка")]);

        result.Should().Be("первая строка\nизменённая строка\nтретья строка");
    }

    [Test]
    public void ApplyPatch_WithAdd_ShouldKeepOriginalAndInsertAfterIt()
    {
        var result = _comparer.ApplyPatch(Chapter, [Add(1, "вставка")]);

        result.Should().Be("первая строка\nвставка\nвторая строка\nтретья строка");
    }

    [Test]
    public void ApplyPatch_WithDelete_ShouldRemoveLine()
    {
        var result = _comparer.ApplyPatch(Chapter, [Delete(2)]);

        result.Should().Be("первая строка\nтретья строка");
    }

    [Test]
    public void ApplyPatch_WithModifyAndAddOnSameLine_ShouldDoBoth()
    {
        var result = _comparer.ApplyPatch(Chapter, [Modify(2, "изменённая"), Add(2, "добавленная")]);

        result.Should().Be("первая строка\nизменённая\nдобавленная\nтретья строка");
    }

    [Test]
    public void ApplyPatch_WithOutOfOrderLines_ShouldApplyInSourceOrder()
    {
        var result = _comparer.ApplyPatch(Chapter, [Modify(3, "третья новая"), Modify(1, "первая новая")]);

        result.Should().Be("первая новая\nвторая строка\nтретья новая");
    }

    [Test]
    public void Compare_WithoutCheckFix_ShouldBeNotApplicable()
    {
        var status = _comparer.Compare(Expected(1, false, [Modify(2, "x")]), Chapter, Found([Modify(2, "y")]));

        status.Should().Be(BenchmarkFixMatchStatus.NotApplicable);
    }

    [Test]
    public void Compare_WithoutExpectedPatch_ShouldBeMissingExpectedPatch()
    {
        var status = _comparer.Compare(Expected(1, true, null), Chapter, Found([Modify(2, "y")]));

        status.Should().Be(BenchmarkFixMatchStatus.MissingExpectedPatch);
    }

    [Test]
    public void Compare_WhenExpectedButIssueNotFound_ShouldBeMissingFoundPatch()
    {
        var status = _comparer.Compare(Expected(1, true, [Modify(2, "x")]), Chapter, null);

        status.Should().Be(BenchmarkFixMatchStatus.MissingFoundPatch);
    }

    [Test]
    public void Compare_WhenModelGaveNoPatch_ShouldBeMissingFoundPatch()
    {
        var status = _comparer.Compare(Expected(1, true, [Modify(2, "x")]), Chapter, Found(null));

        status.Should().Be(BenchmarkFixMatchStatus.MissingFoundPatch);
    }

    [Test]
    public void Compare_WithIdenticalPatch_ShouldBeMatched()
    {
        var status = _comparer.Compare(Expected(1, true, [Modify(2, "исправлено")]), Chapter,
            Found([Modify(2, "исправлено")]));

        status.Should().Be(BenchmarkFixMatchStatus.Matched);
    }

    [Test]
    public void Compare_WithDifferentPatch_ShouldBeMismatched()
    {
        var status = _comparer.Compare(Expected(1, true, [Modify(2, "исправлено")]), Chapter,
            Found([Modify(2, "иначе исправлено")]));

        status.Should().Be(BenchmarkFixMatchStatus.Mismatched);
    }

    [Test]
    public void Compare_ShouldIgnoreTrailingSpacesAndLineEndings()
    {
        var status = _comparer.Compare(Expected(1, true, [Modify(2, "исправлено")]), "первая\r\nвторая\r\nтретья",
            Found([Modify(2, "исправлено   ")]));

        status.Should().Be(BenchmarkFixMatchStatus.Matched);
    }

    [Test]
    public void Compare_ShouldBeCaseSensitive()
    {
        var status = _comparer.Compare(Expected(1, true, [Modify(2, "Корова")]), Chapter,
            Found([Modify(2, "корова")]));

        status.Should().Be(BenchmarkFixMatchStatus.Mismatched);
    }

    [Test]
    public void AppliedExpectedFix_WithoutPatch_ShouldBeEmpty()
    {
        _comparer.AppliedExpectedFix(Expected(1, false, null), Chapter).Should().BeEmpty();
    }
}
