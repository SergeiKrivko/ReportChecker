using ReportChecker.Models;
using ReportChecker.Models.Sources;

namespace ReportChecker.Abstractions;

public interface IReportService
{
    public Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProviderKey,
        ReportSourceUnion source, Guid? llmModelId, ImageProcessingMode imageProcessingMode);

    public Task<Guid> CreateCheckAsync(Report report);
    public Task<SourceInfo> GetSourceInfoAsync(string sourceProvider, ReportSourceUnion source);
}