namespace ReportChecker.Models;

public class User
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}