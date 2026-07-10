namespace ReportChecker.Shared.Models;

public record SourcePack(string Format, Stream Stream, string FileName, string? EntryFilePath);