using ReportChecker.Models;

namespace ReportChecker.Api.Dtos;

public class IssueDto
{
    public required Guid Id { get; init; }
    public required Guid CheckId { get; init; }
    public required string Title { get; init; }
    public IssueStatus Status { get; init; }
    public int Priority { get; init; } = 1;
    public required string Chapter { get; init; }
    public int? Line { get; init; }

    public CommentDto[] Comments { get; init; } = [];
}