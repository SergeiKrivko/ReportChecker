using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Services.Converters;

public static class PatchConverter
{
    public static Patch ToDomain(this Shared.ApiClient.Patch dto)
    {
        return new Patch
        {
            Id = dto.Id,
            CommentId = dto.CommentId,
            Status = dto.Status.ToDomain(),
            Lines = dto.Lines?.Select(e => e.ToDomain()).ToList() ?? [],
        };
    }

    public static PatchStatus ToDomain(this Shared.ApiClient.PatchStatus dto)
    {
        return dto switch
        {
            Shared.ApiClient.PatchStatus.Pending => PatchStatus.Pending,
            Shared.ApiClient.PatchStatus.InProgress => PatchStatus.InProgress,
            Shared.ApiClient.PatchStatus.Failed => PatchStatus.Failed,
            Shared.ApiClient.PatchStatus.Completed => PatchStatus.Completed,
            Shared.ApiClient.PatchStatus.Accepted => PatchStatus.Accepted,
            Shared.ApiClient.PatchStatus.Rejected => PatchStatus.Rejected,
            Shared.ApiClient.PatchStatus.Applied => PatchStatus.Applied,
            _ => PatchStatus.Failed
        };
    }

    public static Shared.ApiClient.PatchStatus ToDto(this PatchStatus dto)
    {
        return dto switch
        {
            PatchStatus.Pending => Shared.ApiClient.PatchStatus.Pending,
            PatchStatus.InProgress => Shared.ApiClient.PatchStatus.InProgress,
            PatchStatus.Failed => Shared.ApiClient.PatchStatus.Failed,
            PatchStatus.Completed => Shared.ApiClient.PatchStatus.Completed,
            PatchStatus.Accepted => Shared.ApiClient.PatchStatus.Accepted,
            PatchStatus.Rejected => Shared.ApiClient.PatchStatus.Rejected,
            PatchStatus.Applied => Shared.ApiClient.PatchStatus.Applied,
            _ => Shared.ApiClient.PatchStatus.Failed
        };
    }

    public static PatchLine ToDomain(this Shared.ApiClient.PatchLine dto)
    {
        return new PatchLine
        {
            Number = dto.Number,
            Content = dto.Content,
            PreviousContent = dto.PreviousContent,
            Type = dto.Type?.ToDomain() ?? PatchLineType.Modify,
        };
    }

    public static PatchLineType ToDomain(this Shared.ApiClient.PatchLineType dto)
    {
        return dto switch
        {
            Shared.ApiClient.PatchLineType.Add => PatchLineType.Add,
            Shared.ApiClient.PatchLineType.Delete => PatchLineType.Delete,
            _ => PatchLineType.Modify
        };
    }
}