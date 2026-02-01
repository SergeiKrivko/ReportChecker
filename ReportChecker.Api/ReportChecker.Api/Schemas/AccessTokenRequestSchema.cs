namespace ReportChecker.Api.Schemas;

public class AccessTokenRequestSchema
{
    public string? RedirectUrl { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = [];
}