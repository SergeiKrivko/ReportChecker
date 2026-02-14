namespace ReportChecker.Models;

public class UserInfo
{
    public required Guid Id { get; init; }
    public required AccountInfo[] Accounts { get; init; }
}

public class AccountInfo
{
    public required string Provider { get; init; }
    public required string Id { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
}