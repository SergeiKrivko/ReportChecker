using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class BenchmarkCaseTextConverter
{
    public static BenchmarkCaseTextDto ToDto(this BenchmarkCaseText text)
    {
        return new BenchmarkCaseTextDto
        {
            CaseId = text.CaseId,
            CaseName = text.CaseName,
            Format = text.Format,
            Chapters = text.Chapters.Select(e => e.ToDto()).ToArray(),
        };
    }

    public static BenchmarkCaseChapterDto ToDto(this BenchmarkCaseChapter chapter)
    {
        return new BenchmarkCaseChapterDto
        {
            Name = chapter.Name,
            Lines = chapter.Lines.Select(e => e.ToDto()).ToArray(),
        };
    }

    public static BenchmarkCaseLineDto ToDto(this BenchmarkCaseLine line)
    {
        return new BenchmarkCaseLineDto
        {
            Number = line.Number,
            Content = line.Content,
        };
    }
}