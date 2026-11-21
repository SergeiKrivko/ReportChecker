using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class GoogleDriveCheckSourceEntity : BaseCheckSourceEntity
{
    [MaxLength(256)] public required string FileId { get; init; }
    [MaxLength(64)] public required string ContentHash { get; init; }
    [MaxLength(256)] public string? FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}
