namespace ReportChecker.Api.Dtos;

public class MarkReadDto
{
    public Guid[] CommentIds { get; init; } = [];
    public bool IsRead { get; init; } = true;
}