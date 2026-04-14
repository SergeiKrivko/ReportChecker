namespace ReportChecker.Cli.Services.Converters;

public static class ReportConverter
{
    public static Models.Report ToDomain(this Report dto)
    {
        return new Models.Report
        {
            Id = dto.Id,
            Name = dto.Name,
        };
    }
}