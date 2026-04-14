namespace ReportChecker.Cli.Abstractions;

public interface IReportService
{
    public Task<Models.Report> UploadAsync(string path);
}