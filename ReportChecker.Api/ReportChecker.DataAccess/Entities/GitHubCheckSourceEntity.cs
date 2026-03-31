using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class GitHubCheckSourceEntity : BaseCheckSourceEntity
{
    [MaxLength(64)] public required string CommitHash { get; init; }
}