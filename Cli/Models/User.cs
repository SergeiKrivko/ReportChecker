namespace ReportChecker.Cli.Models;

public class User
{
    public required Guid Id { get; init; }
    public IReadOnlyCollection<AccountInfo> Accounts { get; init; } = [];
}