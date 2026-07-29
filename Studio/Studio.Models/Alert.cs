namespace ReportChecker.Studio.Models;

public class Alert
{
    public AlertType Type { get; init; }
    public required string Text { get; init; }
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(10);
}

public enum AlertType
{
    Info,
    Success,
    Warning,
    Error,
}