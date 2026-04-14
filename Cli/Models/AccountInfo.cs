namespace ReportChecker.Cli.Models;

public class AccountInfo
{
    public required string Provider { get; init; }

    public required string Id { get; init; }

    public string? Name { get; init; }

    public string? Login { get; init; }

    public string? Email { get; init; }

    public string? AvatarUrl { get; init; }
}