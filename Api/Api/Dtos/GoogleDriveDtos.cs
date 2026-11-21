using ReportChecker.Models.GoogleDrive;

namespace ReportChecker.Api.Dtos;

public class GoogleDriveConnectionDto
{
    public bool HasAccount { get; init; }
    public bool Connected { get; init; }
    public string? Login { get; init; }
}

public class GoogleDriveResourceDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string MimeType { get; init; }
    public long? Size { get; init; }
    public DateTime? Modified { get; init; }

    public static GoogleDriveResourceDto FromDomain(GoogleDriveResource resource) => new()
    {
        Id = resource.Id,
        Name = resource.Name,
        Type = resource.Type,
        MimeType = resource.MimeType,
        Size = resource.Size,
        Modified = resource.Modified,
    };
}

public class GoogleDriveFolderDto
{
    public required string FolderId { get; init; }
    public int PageSize { get; init; }
    public string? NextPageToken { get; init; }
    public IReadOnlyList<GoogleDriveResourceDto> Items { get; init; } = [];

    public static GoogleDriveFolderDto FromDomain(GoogleDriveFolderListing listing) => new()
    {
        FolderId = listing.FolderId,
        PageSize = listing.PageSize,
        NextPageToken = listing.NextPageToken,
        Items = listing.Items.Select(GoogleDriveResourceDto.FromDomain).ToList(),
    };
}

public class GoogleDriveSyncResultDto
{
    public bool Created { get; init; }
    public Guid? CheckId { get; init; }
    public string? Reason { get; init; }
}
