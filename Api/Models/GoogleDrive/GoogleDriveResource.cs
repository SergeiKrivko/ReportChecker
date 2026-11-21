namespace ReportChecker.Models.GoogleDrive;

/// <summary>Элемент папки Диска, отдаваемый в UI для выбора источника.</summary>
public class GoogleDriveResource
{
    /// <summary>Идентификатор файла или папки в Google Drive.</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary><c>dir</c> или <c>file</c>.</summary>
    public required string Type { get; init; }

    public required string MimeType { get; init; }
    public long? Size { get; init; }
    public string? Md5 { get; init; }
    public DateTime? Modified { get; init; }
}

public class GoogleDriveFolderListing
{
    public required string FolderId { get; init; }
    public int PageSize { get; init; }
    public string? NextPageToken { get; init; }
    public IReadOnlyList<GoogleDriveResource> Items { get; init; } = [];
}
