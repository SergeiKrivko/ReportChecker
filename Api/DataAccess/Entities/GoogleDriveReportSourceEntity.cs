using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class GoogleDriveReportSourceEntity : BaseReportSourceEntity
{
    public Guid OwnerId { get; init; }
    [MaxLength(256)] public required string FileId { get; init; }
    [MaxLength(256)] public required string Name { get; init; }
    [MaxLength(256)] public string? EntryFilePath { get; init; }
}
