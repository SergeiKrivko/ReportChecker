namespace ReportChecker.Api.Schemas;

public class MarkReadSchema
{
    public Guid[] CommentIds { get; init; } = [];
    public bool IsRead { get; init; } = true;
}