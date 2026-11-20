namespace ReportChecker.Models.YandexDisk;

/// <summary>Элемент папки Диска, отдаваемый в UI для выбора источника.</summary>
public class YandexDiskResource
{
    public required string Name { get; init; }
    public required string Path { get; init; }

    /// <summary><c>dir</c> или <c>file</c>.</summary>
    public required string Type { get; init; }

    public long? Size { get; init; }
    public string? Md5 { get; init; }
    public DateTime? Modified { get; init; }
}

public class YandexDiskFolderListing
{
    public required string Path { get; init; }
    public int Limit { get; init; }
    public int Offset { get; init; }
    public int Total { get; init; }
    public IReadOnlyList<YandexDiskResource> Items { get; init; } = [];
}
