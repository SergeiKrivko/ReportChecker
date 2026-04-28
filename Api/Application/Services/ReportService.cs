using ReportChecker.Abstractions;
using ReportChecker.Models;
using ReportChecker.Models.Sources;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.Application.Services;

public class ReportService(
    IReportRepository reportRepository,
    IProviderService providerService,
    ICheckService checkService,
    IEnumerable<IFormatProvider> formatProviders) : IReportService
{
    public async Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProviderKey,
        ReportSourceUnion source, Guid? llmModelId, ImageProcessingMode imageProcessingMode)
    {
        var reportId = await reportRepository.CreateReportAsync(ownerId, name, format, sourceProviderKey, llmModelId,
            imageProcessingMode);
        var provider = providerService.GetSourceProvider(sourceProviderKey);
        await provider.SaveAsync(reportId, source);

        var firstSource = await provider.GetFirstSourceAsync(reportId);

        await checkService.CreateCheckAsync(reportId, ownerId, firstSource.Source, firstSource.Name);

        return reportId;
    }

    public async Task<Guid> CreateCheckAsync(Report report)
    {
        var provider = providerService.GetSourceProvider(report.SourceProvider);
        var source = await provider.GetFirstSourceAsync(report.Id);
        return await checkService.CreateCheckAsync(report.Id, report.OwnerId, source.Source, source.Name);
    }

    public async Task<SourceInfo> GetSourceInfoAsync(string sourceProviderKey, ReportSourceUnion source)
    {
        var sourceProvider = providerService.GetSourceProvider(sourceProviderKey);
        try
        {
            var archive = await sourceProvider.OpenAsync(source);
            foreach (var formatProvider in formatProviders)
            {
                if (await formatProvider.TestSourceAsync(archive))
                    return new SourceInfo
                    {
                        Status = SourceStatus.Success,
                        Format = formatProvider.Key,
                    };
            }

            return new SourceInfo
            {
                Status = SourceStatus.WrongFormat,
            };
        }
        catch (Exception)
        {
            return new SourceInfo
            {
                Status = SourceStatus.NotFound,
            };
        }
    }
}