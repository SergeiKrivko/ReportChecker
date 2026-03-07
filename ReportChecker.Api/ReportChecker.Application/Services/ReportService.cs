using ReportChecker.Abstractions;
using ReportChecker.Models;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.Application.Services;

public class ReportService(
    IReportRepository reportRepository,
    IProviderService providerService,
    ICheckService checkService,
    IEnumerable<IFormatProvider> formatProviders) : IReportService
{
    public async Task<Guid> CreateReportAsync(Guid ownerId, string name, string format, string sourceProviderKey,
        string source)
    {
        var reportId = await reportRepository.CreateReportAsync(ownerId, name, format, sourceProviderKey, source);
        var provider = providerService.GetSourceProvider(sourceProviderKey);
        var firstSource = await provider.FindSourceAsync(source);

        await checkService.CreateCheckAsync(reportId, ownerId, firstSource);

        return reportId;
    }

    public async Task<Guid> CreateCheckAsync(Report report)
    {
        var provider = providerService.GetSourceProvider(report.SourceProvider);
        var source = await provider.FindSourceAsync(report.Source ?? throw new Exception("Source not found"));
        return await checkService.CreateCheckAsync(report.Id, report.OwnerId, source);
    }

    public async Task<SourceInfo> GetSourceInfoAsync(string sourceProviderKey, string source)
    {
        var sourceProvider = providerService.GetSourceProvider(sourceProviderKey);
        try
        {
            var schema = await sourceProvider.FindSourceAsync(source);
            foreach (var formatProvider in formatProviders)
            {
                if (await formatProvider.TestSourceAsync(schema.Archive))
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