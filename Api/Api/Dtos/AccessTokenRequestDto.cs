namespace ReportChecker.Api.Dtos;

public class AccessTokenRequestDto
{
    public string? RedirectUrl { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = [];
}