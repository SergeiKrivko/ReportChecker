namespace ReportChecker.Studio.Abstractions;

public interface IWebLinksService
{
    public void GoToSubscriptions();
    public void GoToAccounts();
    public void GoToStatistics();
    public void GoToReport(Guid reportId);
    public void GoToReportSettings(Guid reportId);
    public void GoToIssue(Guid reportId, Guid issueId);
}