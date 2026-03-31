using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class GitHubReportSourceEntity : BaseReportSourceEntity
{
    public long RepositoryId { get; init; }
    [MaxLength(64)] public required string Branch { get; init; }
    [MaxLength(256)] public required string Path { get; init; }
}