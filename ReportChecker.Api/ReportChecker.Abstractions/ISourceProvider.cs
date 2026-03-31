using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Abstractions;

public interface ISourceProvider
{
    public string Key { get; }

    public Task<IFileArchive> OpenAsync(Guid reportId, Guid checkId);
    public Task<IFileArchive> OpenAsync(ReportSourceUnion source);
    public Task<SourceSchema> GetFirstSourceAsync(Guid reportId);
    public Task<Guid> SaveAsync(Guid? checkId, CheckSourceUnion source);
    public Task<bool> AttachCheckAsync(Guid id, Guid checkId);
    public Task<Guid> SaveAsync(Guid reportId, ReportSourceUnion source);

    public Task WriteCheckStatusAsync(Report report, Check check, bool isCompleted) =>
        Task.CompletedTask;
}

public record SourceSchema(CheckSourceUnion Source, string? Name = null);