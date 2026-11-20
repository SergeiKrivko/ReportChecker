namespace ReportChecker.Models.YandexDisk;

/// <summary>Состояние ресурса на Диске на момент опроса.</summary>
public class YandexDiskState
{
    public required bool IsFolder { get; init; }
    public required string Path { get; init; }
    public required string Name { get; init; }

    /// <summary>md5 файла или sha256 списка файлов папки.</summary>
    public required string Hash { get; init; }

    public long Size { get; init; }
    public DateTime? Modified { get; init; }

    /// <summary>Файлы ресурса (для одиночного файла — один элемент).</summary>
    public IReadOnlyList<YandexDiskFileInfo> Files { get; init; } = [];

    /// <summary>Файл входа, если его удалось определить автоматически.</summary>
    public string? SuggestedEntryPath { get; init; }
}

public class YandexDiskFileInfo
{
    public required string Path { get; init; }

    /// <summary>Путь относительно выбранного ресурса (для папки).</summary>
    public required string RelativePath { get; init; }

    public required string Name { get; init; }
    public long Size { get; init; }
    public string? Md5 { get; init; }
    public DateTime? Modified { get; init; }
}
