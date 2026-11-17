namespace ReportChecker.Models;

public class ReportAccess
{
    public required Report Report { get; init; }
    public required ReportAccessLevel Level { get; init; }
}
