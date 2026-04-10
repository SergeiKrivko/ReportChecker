using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class FileCheckSourceEntity : BaseCheckSourceEntity
{
    [MaxLength(256)] public string? FileName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
}