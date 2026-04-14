namespace ReportChecker.Cli.Services.Converters;

public static class PatchConverter
{
    public static Models.Patch ToDomain(this Patch dto, string chapter)
    {
        return new Models.Patch
        {
            Id = dto.Id,
            Status = dto.Status.ToDomain(),
            Chapter = chapter,
            Lines = dto.Lines.Select(e => e.ToDomain()).ToList()
        };
    }

    public static Models.PatchStatus ToDomain(this PatchStatus dto)
    {
        return dto switch
        {
            PatchStatus.Pending => Models.PatchStatus.Pending,
            PatchStatus.InProgress => Models.PatchStatus.InProgress,
            PatchStatus.Completed => Models.PatchStatus.Completed,
            PatchStatus.Failed => Models.PatchStatus.Failed,
            PatchStatus.Accepted => Models.PatchStatus.Accepted,
            PatchStatus.Rejected => Models.PatchStatus.Rejected,
            PatchStatus.Applied => Models.PatchStatus.Applied,
            _ => throw new ArgumentException("Unknown patch status")
        };
    }
}