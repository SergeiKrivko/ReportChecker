using ReportChecker.Cli.Models;

namespace ReportChecker.Cli.Abstractions;

public interface IReportService
{
    public Task<Report> UploadAsync(string path);
    public Task UploadVersionAsync(Guid reportId, string path);
    public Task<Check> GetCheckAsync(Guid reportId);
    public Task<IFormatProvider> GetFormatProviderAsync(string path);
    public Task<IReadOnlyList<Patch>> GetPatchesAsync(Guid reportId);
    public Task SetPatchStatusAsync(Guid reportId, Guid issueId, Guid commentId, PatchStatus status);
}