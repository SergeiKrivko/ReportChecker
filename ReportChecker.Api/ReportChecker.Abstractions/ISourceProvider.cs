using ReportChecker.Models;

namespace ReportChecker.Abstractions;

public interface ISourceProvider
{
    public string Key { get; }

    public Task<IFileArchive> OpenAsync(string source);
    public Task<SourceSchema> FindSourceAsync(string source);

    public Task WriteCheckStatusAsync(Report report, Check check, bool isCompleted) =>
        Task.CompletedTask;
}

public record SourceSchema(string Source, IFileArchive Archive, string? Name = null);