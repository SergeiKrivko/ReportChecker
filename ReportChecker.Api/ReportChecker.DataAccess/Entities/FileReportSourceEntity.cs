using System.ComponentModel.DataAnnotations;

namespace ReportChecker.DataAccess.Entities;

public class FileReportSourceEntity : BaseReportSourceEntity
{
    public Guid InitialFileId { get; init; }
    [MaxLength(256)] public string? EntryFilePath { get; init; }
}