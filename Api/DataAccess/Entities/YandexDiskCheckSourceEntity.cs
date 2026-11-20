using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class YandexDiskCheckSourceEntity : BaseCheckSourceEntity
{
    [MaxLength(1024)] public required string Path { get; init; }
    [MaxLength(64)] public required string ContentHash { get; init; }
    [MaxLength(256)] public string? FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
