namespace ReportChecker.Models;

public class ReportShareInfo
{
    public required ReportShare Share { get; init; }
    public UserInfo? User { get; init; }
}
