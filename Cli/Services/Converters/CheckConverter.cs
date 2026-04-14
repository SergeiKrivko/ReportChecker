namespace ReportChecker.Cli.Services.Converters;

public static class CheckConverter
{
    public static Models.ProgressStatus ToDomain(this ProgressStatus status)
    {
        return status switch
        {
            ProgressStatus.Queued => Models.ProgressStatus.Pending,
            ProgressStatus.InProgress => Models.ProgressStatus.InProgress,
            ProgressStatus.Completed => Models.ProgressStatus.Completed,
            ProgressStatus.Failed => Models.ProgressStatus.Failed,
            _ => throw new ArgumentException("Unknown progress status")
        };
    }

    public static Models.Check ToDomain(this Check dto)
    {
        return new Models.Check
        {
            Id = dto.Id,
            ReportId = dto.ReportId,
            Status = dto.Status?.ToDomain() ?? Models.ProgressStatus.Pending,
            CreatedAt = dto.CreatedAt,
        };
    }
}