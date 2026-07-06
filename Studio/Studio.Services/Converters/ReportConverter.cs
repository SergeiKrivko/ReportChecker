using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Services.Converters;

public static class ReportConverter
{
    public static Report ToDomain(this ReportChecker.Shared.ApiClient.Report dto)
    {
        return new Report
        {
            Id = dto.Id,
            Name = dto.Name,
        };
    }

    public static ProgressStatus ToDomain(this ReportChecker.Shared.ApiClient.ProgressStatus dtoStatus)
    {
        return dtoStatus switch
        {
            Shared.ApiClient.ProgressStatus.Queued => ProgressStatus.Pending,
            Shared.ApiClient.ProgressStatus.InProgress => ProgressStatus.InProgress,
            Shared.ApiClient.ProgressStatus.Completed => ProgressStatus.Completed,
            Shared.ApiClient.ProgressStatus.Failed => ProgressStatus.Failed,
            Shared.ApiClient.ProgressStatus.Cancelled => ProgressStatus.Cancelled,
            Shared.ApiClient.ProgressStatus.CancellationRequested => ProgressStatus.CancellationRequested,
            _ => ProgressStatus.Pending,
        };
    }
}