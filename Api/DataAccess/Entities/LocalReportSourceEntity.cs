using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class LocalReportSourceEntity : BaseReportSourceEntity
{
    public Guid InitialFileId { get; init; }
    [MaxLength(256)] public string? EntryFilePath { get; init; }
    public Guid ClientId { get; init; }
    [MaxLength(256)] public string? ClientMachineName { get; init; }
}