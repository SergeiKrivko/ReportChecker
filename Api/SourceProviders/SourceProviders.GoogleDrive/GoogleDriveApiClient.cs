using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models.GoogleDrive;

namespace ReportChecker.SourceProviders.GoogleDrive;

/// <summary>
/// Клиент REST API Google Drive (www.googleapis.com/drive/v3). Документация:
/// https://developers.google.com/workspace/drive/api/reference/rest/v3
/// </summary>
public partial class GoogleDriveApiClient(HttpClient httpClient, ILogger<GoogleDriveApiClient> logger)
    : IGoogleDriveClient
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";
    private const string GoogleAppsMimeTypePrefix = "application/vnd.google-apps.";
    private const int MaxFolderFiles = 2000;
    private const int MaxFolderDepth = 12;
    private const int MaxPageSize = 1000;

    private const string FileFields = "id,name,mimeType,size,modifiedTime,md5Checksum";

    public async Task<GoogleDriveFolderListing> GetFolderAsync(string accessToken, string folderId,
        string? pageToken, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        folderId = NormalizeFileId(folderId);

        var parameters = new List<(string, string?)>
        {
            ("q", $"'{folderId}' in parents and trashed = false"),
            ("fields", $"nextPageToken,files({FileFields})"),
            ("pageSize", pageSize.ToString()),
            ("orderBy", "folder,name"),
            ("supportsAllDrives", "true"),
            ("includeItemsFromAllDrives", "true"),
        };
        if (!string.IsNullOrEmpty(pageToken))
            parameters.Add(("pageToken", pageToken));

        var url = BuildUrl("/drive/v3/files", parameters.ToArray());
        var response = await SendAsync(accessToken, url, HttpMethod.Get, ct);
        var dto = JsonSerializer.Deserialize<FileListDto>(response) ?? new FileListDto();

        return new GoogleDriveFolderListing
        {
            FolderId = folderId,
            PageSize = pageSize,
            NextPageToken = string.IsNullOrEmpty(dto.NextPageToken) ? null : dto.NextPageToken,
            Items = (dto.Files ?? [])
                .Where(e => !string.IsNullOrEmpty(e.Id))
                .Select(e => new GoogleDriveResource
                {
                    Id = e.Id!,
                    Name = e.Name ?? e.Id!,
                    Type = IsFolder(e.MimeType) ? "dir" : "file",
                    MimeType = e.MimeType ?? "",
                    Size = e.Size,
                    Md5 = e.Md5Checksum,
                    Modified = e.ModifiedTime?.UtcDateTime,
                })
                .ToList(),
        };
    }

    public async Task<GoogleDriveState> GetStateAsync(string accessToken, string fileId,
        CancellationToken ct = default)
    {
        fileId = NormalizeFileId(fileId);
        var resource = await GetFileAsync(accessToken, fileId, ct);

        if (IsFolder(resource.MimeType))
        {
            var files = new List<GoogleDriveFileInfo>();
            await CollectFilesAsync(accessToken, resource.Id ?? fileId, "", 0, files, ct);

            return new GoogleDriveState
            {
                IsFolder = true,
                FileId = resource.Id ?? fileId,
                Name = resource.Name ?? fileId,
                Hash = ComputeFolderHash(files),
                Size = files.Sum(e => e.Size),
                Modified = resource.ModifiedTime?.UtcDateTime,
                Files = files,
                SuggestedEntryPath = SuggestEntry(files),
            };
        }

        if (IsGoogleAppsDocument(resource.MimeType))
            throw new BadRequestException(
                $"'{resource.Name}' — это документ Google (Docs/Sheets/Slides), его нельзя скачать как файл. " +
                "Загрузите .tex, .docx, .pdf или .zip на Диск и выберите его");

        var file = new GoogleDriveFileInfo
        {
            FileId = resource.Id ?? fileId,
            RelativePath = resource.Name ?? fileId,
            Name = resource.Name ?? fileId,
            MimeType = resource.MimeType ?? "",
            Size = resource.Size ?? 0,
            Md5 = resource.Md5Checksum,
            Modified = resource.ModifiedTime?.UtcDateTime,
        };

        return new GoogleDriveState
        {
            IsFolder = false,
            FileId = file.FileId,
            Name = file.Name,
            Hash = file.Md5 ?? ComputeFileHash(file),
            Size = file.Size,
            Modified = file.Modified,
            Files = [file],
            SuggestedEntryPath = file.Name,
        };
    }

    public async Task DownloadAsync(string accessToken, GoogleDriveState state, Stream target,
        CancellationToken ct = default)
    {
        if (state.IsFolder)
        {
            using var zip = new ZipArchive(target, ZipArchiveMode.Create, leaveOpen: true);
            foreach (var file in state.Files)
            {
                var entry = zip.CreateEntry(file.RelativePath, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await DownloadFileAsync(accessToken, file.FileId, entryStream, ct);
            }

            return;
        }

        await DownloadFileAsync(accessToken, state.FileId, target, ct);
    }

    private async Task DownloadFileAsync(string accessToken, string fileId, Stream target, CancellationToken ct)
    {
        var url = BuildUrl($"/drive/v3/files/{Uri.EscapeDataString(fileId)}", ("alt", "media"),
            ("supportsAllDrives", "true"));

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var (description, reason) = TryGetError(body);
            description ??= response.ReasonPhrase ?? "неизвестная ошибка";
            logger.LogWarning("Google Drive download {status} for {fileId}: {description}",
                (int)response.StatusCode, fileId, description);
            throw CreateException(response.StatusCode, description, reason);
        }

        await using var content = await response.Content.ReadAsStreamAsync(ct);
        await content.CopyToAsync(target, ct);
    }

    private async Task<FileDto> GetFileAsync(string accessToken, string fileId, CancellationToken ct)
    {
        var url = BuildUrl($"/drive/v3/files/{Uri.EscapeDataString(fileId)}", ("fields", FileFields),
            ("supportsAllDrives", "true"));
        var response = await SendAsync(accessToken, url, HttpMethod.Get, ct);
        return JsonSerializer.Deserialize<FileDto>(response) ??
               throw new InternalErrorException($"Google Диск вернул пустой ответ для '{fileId}'");
    }

    private async Task CollectFilesAsync(string accessToken, string folderId, string prefix, int depth,
        List<GoogleDriveFileInfo> result, CancellationToken ct)
    {
        if (depth > MaxFolderDepth)
            throw new BadRequestException("Слишком глубокая вложенность папок в Google Диске");
        if (result.Count > MaxFolderFiles)
            throw new BadRequestException($"В папке больше {MaxFolderFiles} файлов — выберите папку поменьше");

        string? pageToken = null;
        do
        {
            var listing = await GetFolderAsync(accessToken, folderId, pageToken, MaxPageSize, ct);
            foreach (var item in listing.Items)
            {
                ct.ThrowIfCancellationRequested();
                if (item.Type == "dir")
                {
                    await CollectFilesAsync(accessToken, item.Id, $"{prefix}{item.Name}/", depth + 1, result, ct);
                }
                else if (IsGoogleAppsDocument(item.MimeType))
                {
                    // Google Docs/Sheets/Slides не скачиваются как файлы — в архив они не попадают.
                    logger.LogDebug("Skipped Google document '{name}' ({mimeType}) in '{folderId}'", item.Name,
                        item.MimeType, folderId);
                }
                else
                {
                    var file = new GoogleDriveFileInfo
                    {
                        FileId = item.Id,
                        RelativePath = $"{prefix}{item.Name}",
                        Name = item.Name,
                        MimeType = item.MimeType,
                        Size = item.Size ?? 0,
                        Md5 = item.Md5,
                        Modified = item.Modified,
                    };
                    result.Add(file);
                }
            }

            pageToken = listing.NextPageToken;
        } while (!string.IsNullOrEmpty(pageToken));
    }

    private async Task<string> SendAsync(string accessToken, string url, HttpMethod method, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.IsSuccessStatusCode)
            return body;

        var (description, reason) = TryGetError(body);
        description ??= response.ReasonPhrase ?? "неизвестная ошибка";
        logger.LogWarning("Google Drive API {status} for {url}: {description}", (int)response.StatusCode, url,
            description);

        throw CreateException(response.StatusCode, description, reason);
    }

    private static Exception CreateException(HttpStatusCode status, string description, string? reason = null)
    {
        return status switch
        {
            HttpStatusCode.Unauthorized => new UnauthorizedException(
                "Google Диск отклонил токен. Перепривяжите Google-аккаунт на странице «Аккаунт»"),
            HttpStatusCode.NotFound => new NotFoundException($"На Google Диске не найдено: {description}"),
            HttpStatusCode.Forbidden when reason is "rateLimitExceeded" or "userRateLimitExceeded"
                or "dailyLimitExceeded" => new InternalErrorException(
                "Google Диск ограничил частоту запросов, попробуйте позже"),
            HttpStatusCode.Forbidden => new BadRequestException(
                "У приложения нет доступа к этому файлу на Google Диске. Проверьте, что при входе через Google " +
                $"выдано разрешение на чтение Диска: {description}"),
            HttpStatusCode.TooManyRequests => new InternalErrorException(
                "Google Диск ограничил частоту запросов, попробуйте позже"),
            HttpStatusCode.ServiceUnavailable => new InternalErrorException(
                "Google Диск временно недоступен"),
            _ => new BadRequestException($"Ошибка Google Диска: {description}"),
        };
    }

    private static (string? Description, string? Reason) TryGetError(string body)
    {
        try
        {
            var error = JsonSerializer.Deserialize<ErrorRootDto>(body)?.Error;
            if (error == null)
                return (null, null);
            return (error.Message, error.Errors?.FirstOrDefault()?.Reason);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static string BuildUrl(string path, params (string Key, string? Value)[] query)
    {
        var builder = new StringBuilder(path);
        var separator = '?';
        foreach (var (key, value) in query)
        {
            if (value == null)
                continue;
            builder.Append(separator).Append(key).Append('=').Append(Uri.EscapeDataString(value));
            separator = '&';
        }

        return builder.ToString();
    }

    private static bool IsFolder(string? mimeType)
    {
        return string.Equals(mimeType, FolderMimeType, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGoogleAppsDocument(string? mimeType)
    {
        return !string.IsNullOrEmpty(mimeType) &&
               mimeType.StartsWith(GoogleAppsMimeTypePrefix, StringComparison.OrdinalIgnoreCase) &&
               !IsFolder(mimeType);
    }

    private static string NormalizeFileId(string? fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return "root";

        var normalized = fileId.Trim();
        if (!FileIdRegex().IsMatch(normalized))
            throw new BadRequestException("Некорректный идентификатор файла Google Диска");

        return normalized;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex FileIdRegex();

    private static string ComputeFolderHash(IEnumerable<GoogleDriveFileInfo> files)
    {
        var payload = string.Join('\n', files
            .OrderBy(e => e.RelativePath, StringComparer.Ordinal)
            // md5 не меняется при "касании" файла, поэтому изменённое время учитывается
            // только как запасной вариант: поллинг не должен запускать проверку зря.
            .Select(e => e.Md5 != null
                ? $"{e.RelativePath.ToLowerInvariant()}|{e.Md5}"
                : $"{e.RelativePath.ToLowerInvariant()}|{e.Size}|{e.Modified?.Ticks ?? 0}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string ComputeFileHash(GoogleDriveFileInfo file)
    {
        var payload = $"{file.Size}|{file.Modified?.Ticks ?? 0}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string? SuggestEntry(IReadOnlyList<GoogleDriveFileInfo> files)
    {
        var tex = files.Where(e => e.RelativePath.EndsWith(".tex", StringComparison.OrdinalIgnoreCase)).ToList();
        if (tex.Count == 1)
            return tex[0].RelativePath;

        var main = tex.FirstOrDefault(e => string.Equals(e.Name, "main.tex", StringComparison.OrdinalIgnoreCase));
        if (main != null)
            return main.RelativePath;

        var supported = files
            .Where(e => e.RelativePath.EndsWith(".tex", StringComparison.OrdinalIgnoreCase) ||
                        e.RelativePath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                        e.RelativePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .ToList();
        return supported.Count == 1 ? supported[0].RelativePath : null;
    }

    private sealed class FileDto
    {
        [JsonPropertyName("id")] public string? Id { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("mimeType")] public string? MimeType { get; init; }
        // Drive v3 отдаёт size строкой (int64), а не числом.
        [JsonPropertyName("size")]
        [JsonConverter(typeof(LongFromStringConverter))]
        public long? Size { get; init; }
        [JsonPropertyName("modifiedTime")] public DateTimeOffset? ModifiedTime { get; init; }
        [JsonPropertyName("md5Checksum")] public string? Md5Checksum { get; init; }
    }

    private sealed class LongFromStringConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return long.TryParse(reader.GetString(), out var value) ? value : null;
                case JsonTokenType.Number:
                    return reader.GetInt64();
                case JsonTokenType.Null:
                    return null;
                default:
                    reader.Skip();
                    return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value == null)
                writer.WriteNullValue();
            else
                writer.WriteStringValue(value.Value.ToString());
        }
    }

    private sealed class FileListDto
    {
        [JsonPropertyName("nextPageToken")] public string? NextPageToken { get; init; }
        [JsonPropertyName("files")] public List<FileDto>? Files { get; init; }
    }

    private sealed class ErrorRootDto
    {
        [JsonPropertyName("error")] public ErrorDto? Error { get; init; }
    }

    private sealed class ErrorDto
    {
        [JsonPropertyName("code")] public int? Code { get; init; }
        [JsonPropertyName("message")] public string? Message { get; init; }
        [JsonPropertyName("errors")] public List<ErrorItemDto>? Errors { get; init; }
    }

    private sealed class ErrorItemDto
    {
        [JsonPropertyName("message")] public string? Message { get; init; }
        [JsonPropertyName("reason")] public string? Reason { get; init; }
    }
}
