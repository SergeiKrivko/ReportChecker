namespace ReportChecker.Cli.Services.Converters;

public static class PatchLineConverter
{
    public static Models.PatchLine ToDomain(this PatchLine dto)
    {
        return new Models.PatchLine
        {
            Number = dto.Number,
            Type = dto.Type.ToDomain(),
            Content = dto.Content,
            PreviousContent = dto.PreviousContent,
        };
    }

    public static Models.PatchLineType ToDomain(this PatchLineType dto)
    {
        return dto switch
        {
            PatchLineType.Add => Models.PatchLineType.Add,
            PatchLineType.Delete => Models.PatchLineType.Delete,
            PatchLineType.Modify => Models.PatchLineType.Modify,
            _ => throw new ArgumentException("Unknown patch line type")
        };
    }
}