namespace ReportChecker.Models.GoogleDrive;

/// <summary>Состояние ресурса на Google Диске на момент опроса.</summary>
public class GoogleDriveState
{
    public required bool IsFolder { get; init; }

    /// <summary>Идентификатор файла или папки в Google Drive.</summary>
    public required string FileId { get; init; }

    public required string Name { get; init; }

    /// <summary>md5 файла или sha256 отсортированного списка файлов папки.</summary>
    public required string Hash { get; init; }

    public long Size { get; init; }
    public DateTime? Modified { get; init; }

    /// <summary>Файлы ресурса (для одиночного файла — один элемент).</summary>
    public IReadOnlyList<GoogleDriveFileInfo> Files { get; init; } = [];

    /// <summary>Файл входа, если его удалось определить автоматически.</summary>
    public string? SuggestedEntryPath { get; init; }
}

public class GoogleDriveFileInfo
{
    public required string FileId { get; init; }

    /// <summary>Путь относительно выбранного ресурса (для папки).</summary>
    public required string RelativePath { get; init; }

    public required string Name { get; init; }
    public required string MimeType { get; init; }
    public long Size { get; init; }
    public string? Md5 { get; init; }
    public DateTime? Modified { get; init; }
}
