using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class YandexDiskReportSourceEntity : BaseReportSourceEntity
{
    public Guid OwnerId { get; init; }
    [MaxLength(1024)] public required string Path { get; init; }
    [MaxLength(256)] public string? EntryFilePath { get; init; }
}
