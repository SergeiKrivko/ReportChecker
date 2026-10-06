using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ReportChecker.Application.Services;
using ReportChecker.FormatProviders.Latex;
using ReportChecker.Models;

namespace Application.Tests.Services;

/// <summary>
/// Проверяет, что тестовые данные бенчмарка согласованы с тем, что реально видит модель:
/// имена глав и номера строк в <c>case.json</c> должны совпадать с разбором исходника.
/// Такие тесты ловят сдвиг нумерации строк (например, из-за лишних переводов строк).
/// </summary>
[TestFixture]
public class BenchmarkCaseDataTests
{
    private static IConfiguration Configuration()
    {
        var configuration = new Mock<IConfiguration>();
        configuration.Setup(e => e["Reports.ChapterSeparator"]).Returns("//");
        configuration.Setup(e => e["Reports.MinChapterSize"]).Returns("50");
        configuration.Setup(e => e["Reports.MaxRequestSize"]).Returns("5000");
        return configuration.Object;
    }

    private static string BenchmarksPath => Path.Combine(AppContext.BaseDirectory, "Benchmarks");

    private static IEnumerable<string> CaseIds()
    {
        return Directory.EnumerateDirectories(BenchmarksPath).Select(Path.GetFileName)!;
    }

    /// <summary>
    /// Возвращает главы теста так, как их пронумерует агент: через тот же
    /// <see cref="DifferenceService"/>, чей результат уходит модели.
    /// </summary>
    private static async Task<Dictionary<string, List<ChapterLine>>> NumberedChaptersAsync(string caseId,
        string entryFile)
    {
        var directory = Path.Combine(BenchmarksPath, caseId);
        var archive = new BenchmarkDirectoryArchive(directory, entryFile);
        var provider = new LatexFormatProvider(Configuration());
        var differenceService = new DifferenceService();

        var result = new Dictionary<string, List<ChapterLine>>();
        foreach (var chapter in await provider.GetChaptersAsync(archive))
            result[chapter.Name] = differenceService.GetDifference(chapter, null).Difference.ToList();
        return result;
    }

    [Test]
    public void CaseData_ShouldBeCopiedToOutput()
    {
        Directory.Exists(BenchmarksPath).Should().BeTrue($"'{BenchmarksPath}' должен существовать");
        CaseIds().Should().NotBeEmpty();
    }

    [Test]
    public async Task ExpectedIssues_ShouldPointAtRealLinesOfRealChapters()
    {
        var checkedCases = 0;
        foreach (var caseId in CaseIds())
        {
            var caseFile = Path.Combine(BenchmarksPath, caseId, "case.json");
            if (!File.Exists(caseFile))
                continue;
            checkedCases++;

            using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(caseFile));
            var root = document.RootElement;
            var chapters = await NumberedChaptersAsync(caseId, root.GetProperty("entryFile").GetString()!);

            foreach (var expected in root.GetProperty("expected").EnumerateArray())
            {
                var number = expected.GetProperty("number").GetInt32();
                var chapterName = expected.GetProperty("chapter").GetString()!;

                chapters.Should().ContainKey(chapterName,
                    $"тест '{caseId}', ошибка {number}: глава '{chapterName}' должна существовать; " +
                    $"доступны: {string.Join(" | ", chapters.Keys)}");

                if (!expected.TryGetProperty("line", out var lineProperty) ||
                    lineProperty.ValueKind != System.Text.Json.JsonValueKind.Number)
                    continue;

                var line = lineProperty.GetInt32();
                var lines = chapters[chapterName];
                line.Should().BeInRange(1, lines.Count,
                    $"тест '{caseId}', ошибка {number}: строка {line} выходит за границы главы ({lines.Count} строк)");
                lines[line - 1].Content.Should().NotBeNullOrWhiteSpace(
                    $"тест '{caseId}', ошибка {number}: строка {line} в главе '{chapterName}' пустая — " +
                    "нумерация строк сдвинута (лишние переводы строк при разборе)");
            }
        }

        checkedCases.Should().BeGreaterThan(0, "должен быть проверен хотя бы один тест бенчмарка");
    }

    /// <summary>
    /// Нумерация строк не должна зависеть от переводов строк в исходном файле:
    /// раньше CRLF давал удвоенный '\r' и лишние пустые строки.
    /// </summary>
    [Test]
    public async Task LineNumbering_ShouldNotDependOnLineEndings()
    {
        foreach (var caseId in CaseIds())
        {
            var caseFile = Path.Combine(BenchmarksPath, caseId, "case.json");
            if (!File.Exists(caseFile))
                continue;

            using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(caseFile));
            var entryFile = document.RootElement.GetProperty("entryFile").GetString()!;
            var directory = Path.Combine(BenchmarksPath, caseId);
            var provider = new LatexFormatProvider(Configuration());

            var original = await provider.GetChaptersAsync(new BenchmarkDirectoryArchive(directory, entryFile));

            // Тот же документ, но с LF вместо CRLF: нумерация обязана совпасть.
            var text = File.ReadAllText(Path.Combine(directory, entryFile)).Replace("\r\n", "\n");
            var lfDirectory = Path.Combine(Path.GetTempPath(), $"benchmark-lf-{Guid.NewGuid()}");
            Directory.CreateDirectory(lfDirectory);
            try
            {
                await File.WriteAllTextAsync(Path.Combine(lfDirectory, entryFile), text);
                var rewritten =
                    await provider.GetChaptersAsync(new BenchmarkDirectoryArchive(lfDirectory, entryFile));

                rewritten.Select(e => (e.Name, Content: e.Content.Replace("\r\n", "\n")))
                    .Should().Equal(original.Select(e => (e.Name, Content: e.Content.Replace("\r\n", "\n"))),
                        $"тест '{caseId}': переводы строк не должны менять разбор и нумерацию");
            }
            finally
            {
                Directory.Delete(lfDirectory, true);
            }
        }
    }
}
