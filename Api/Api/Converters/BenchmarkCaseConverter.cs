using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class BenchmarkCaseConverter
{
    public static BenchmarkCaseDto ToDto(this BenchmarkCase benchmarkCase)
    {
        return new BenchmarkCaseDto
        {
            Id = benchmarkCase.Id,
            Name = benchmarkCase.Name,
            Format = benchmarkCase.Format,
            EntryFile = benchmarkCase.EntryFile,
            Description = benchmarkCase.Description,
            DisplayMode = benchmarkCase.DisplayMode,
            Expected = benchmarkCase.Expected.Select(e => e.ToDto()).ToArray(),
            ValidationError = benchmarkCase.ValidationError,
        };
    }

    public static BenchmarkExpectedIssueDto ToDto(this BenchmarkExpectedIssue issue)
    {
        return new BenchmarkExpectedIssueDto
        {
            Number = issue.Number,
            Chapter = issue.Chapter,
            Line = issue.Line,
            Title = issue.Title,
            Comment = issue.Comment,
            Priority = issue.Priority,
            ErrorClass = issue.ErrorClass,
            CheckFix = issue.CheckFix,
            Patch = issue.Patch?.Select(e => e.ToDto()).ToArray(),
        };
    }
}