using ReportChecker.Models.YandexDisk;

namespace ReportChecker.Api.Dtos;

public class YandexDiskConnectionDto
{
    public bool Connected { get; init; }
    public string? Login { get; init; }
}

public class YandexDiskOAuthUrlDto
{
    public required string Url { get; init; }
}

public class YandexDiskResourceDto
{
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required string Type { get; init; }
    public long? Size { get; init; }
    public string? Md5 { get; init; }
    public DateTime? Modified { get; init; }

    public static YandexDiskResourceDto FromDomain(YandexDiskResource resource) => new()
    {
        Name = resource.Name,
        Path = resource.Path,
        Type = resource.Type,
        Size = resource.Size,
        Md5 = resource.Md5,
        Modified = resource.Modified,
    };
}

public class YandexDiskFolderDto
{
    public required string Path { get; init; }
    public int Limit { get; init; }
    public int Offset { get; init; }
    public int Total { get; init; }
    public IReadOnlyList<YandexDiskResourceDto> Items { get; init; } = [];

    public static YandexDiskFolderDto FromDomain(YandexDiskFolderListing listing) => new()
    {
        Path = listing.Path,
        Limit = listing.Limit,
        Offset = listing.Offset,
        Total = listing.Total,
        Items = listing.Items.Select(YandexDiskResourceDto.FromDomain).ToList(),
    };
}

public class YandexDiskSyncResultDto
{
    public bool Created { get; init; }
    public Guid? CheckId { get; init; }
    public string? Reason { get; init; }
}
