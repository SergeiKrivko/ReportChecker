using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models.YandexDisk;

namespace ReportChecker.SourceProviders.YandexDisk;

/// <summary>
/// Клиент REST API Яндекс.Диска (cloud-api.yandex.net/v1). Документация:
/// https://yandex.ru/dev/disk-api/doc/ru/
/// </summary>
public class YandexDiskApiClient(HttpClient httpClient, ILogger<YandexDiskApiClient> logger) : IYandexDiskClient
{
    private const int MaxFolderFiles = 2000;
    private const int MaxFolderDepth = 12;
    private const int PageSize = 500;

    private const string ResourceFields =
        "name,path,type,size,md5,modified,_embedded.total,_embedded.limit,_embedded.offset," +
        "_embedded.items.name,_embedded.items.path,_embedded.items.type,_embedded.items.size," +
        "_embedded.items.md5,_embedded.items.modified";

    public async Task<YandexDiskState> GetStateAsync(string accessToken, string path, CancellationToken ct = default)
    {
        var resource = await GetResourceAsync(accessToken, path, ct: ct);
        if (string.Equals(resource.Type, "dir", StringComparison.OrdinalIgnoreCase))
        {
            var files = new List<YandexDiskFileInfo>();
            await CollectFilesAsync(accessToken, resource.Path ?? path, "", 0, files, ct);

            return new YandexDiskState
            {
                IsFolder = true,
                Path = resource.Path ?? path,
                Name = resource.Name ?? GetName(resource.Path ?? path),
                Hash = ComputeFolderHash(files),
                Size = files.Sum(e => e.Size),
                Modified = resource.Modified?.UtcDateTime,
                Files = files,
                SuggestedEntryPath = SuggestEntry(files),
            };
        }

        var file = new YandexDiskFileInfo
        {
            Path = resource.Path ?? path,
            RelativePath = resource.Name ?? GetName(resource.Path ?? path),
            Name = resource.Name ?? GetName(resource.Path ?? path),
            Size = resource.Size ?? 0,
            Md5 = resource.Md5,
            Modified = resource.Modified?.UtcDateTime,
        };

        return new YandexDiskState
        {
            IsFolder = false,
            Path = file.Path,
            Name = file.Name,
            Hash = file.Md5 ?? ComputeFileHash(file),
            Size = file.Size,
            Modified = file.Modified,
            Files = [file],
            SuggestedEntryPath = file.Name,
        };
    }

    public async Task<YandexDiskFolderListing> GetFolderAsync(string accessToken, string path, int offset, int limit,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, PageSize);
        var resource = await GetResourceAsync(accessToken, path, limit, offset, ct);
        if (!string.Equals(resource.Type, "dir", StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException($"'{(resource.Path ?? path)}' — не папка");

        var items = resource.Embedded?.Items ?? [];
        return new YandexDiskFolderListing
        {
            Path = resource.Path ?? path,
            Limit = resource.Embedded?.Limit ?? limit,
            Offset = resource.Embedded?.Offset ?? offset,
            Total = resource.Embedded?.Total ?? items.Count,
            Items = items
                .Select(e => new YandexDiskResource
                {
                    Name = e.Name ?? GetName(e.Path ?? ""),
                    Path = e.Path ?? "",
                    Type = e.Type ?? "file",
                    Size = e.Size,
                    Md5 = e.Md5,
                    Modified = e.Modified?.UtcDateTime,
                })
                .Where(e => !string.IsNullOrEmpty(e.Path))
                .ToList(),
        };
    }

    public async Task DownloadAsync(string accessToken, YandexDiskState state, Stream target,
        CancellationToken ct = default)
    {
        if (state.IsFolder)
        {
            using var zip = new ZipArchive(target, ZipArchiveMode.Create, leaveOpen: true);
            foreach (var file in state.Files)
            {
                var entry = zip.CreateEntry(file.RelativePath, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await DownloadFileAsync(accessToken, file.Path, entryStream, ct);
            }

            return;
        }

        await DownloadFileAsync(accessToken, state.Path, target, ct);
    }

    private async Task DownloadFileAsync(string accessToken, string path, Stream target, CancellationToken ct)
    {
        var href = await GetDownloadHrefAsync(accessToken, path, ct);
        using var request = new HttpRequestMessage(HttpMethod.Get, href);
        request.Headers.TryAddWithoutValidation("Authorization", $"OAuth {accessToken}");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
            throw new InternalErrorException(
                $"Не удалось скачать '{path}' с Яндекс.Диска: {(int)response.StatusCode} {response.ReasonPhrase}");

        await using var content = await response.Content.ReadAsStreamAsync(ct);
        await content.CopyToAsync(target, ct);
    }

    private async Task<string> GetDownloadHrefAsync(string accessToken, string path, CancellationToken ct)
    {
        var url = BuildUrl("/v1/disk/resources/download", ("path", path));
        var response = await SendAsync(accessToken, url, HttpMethod.Get, ct);
        var link = JsonSerializer.Deserialize<LinkDto>(response);
        if (string.IsNullOrEmpty(link?.Href))
            throw new InternalErrorException($"Яндекс.Диск не вернул ссылку на скачивание '{path}'");
        return link.Href;
    }

    private async Task<ResourceDto> GetResourceAsync(string accessToken, string path, int? limit = null,
        int? offset = null, CancellationToken ct = default)
    {
        var parameters = new List<(string, string?)> { ("path", path), ("fields", ResourceFields) };
        if (limit != null)
            parameters.Add(("limit", limit.Value.ToString()));
        if (offset != null)
            parameters.Add(("offset", offset.Value.ToString()));

        var url = BuildUrl("/v1/disk/resources", parameters.ToArray());
        var response = await SendAsync(accessToken, url, HttpMethod.Get, ct);
        return JsonSerializer.Deserialize<ResourceDto>(response) ??
               throw new InternalErrorException($"Яндекс.Диск вернул пустой ответ для '{path}'");
    }

    private async Task CollectFilesAsync(string accessToken, string folderPath, string prefix, int depth,
        List<YandexDiskFileInfo> result, CancellationToken ct)
    {
        if (depth > MaxFolderDepth)
            throw new BadRequestException($"Слишком глубокая вложенность папок в '{folderPath}'");
        if (result.Count > MaxFolderFiles)
            throw new BadRequestException($"В папке больше {MaxFolderFiles} файлов — выберите папку поменьше");

        var offset = 0;
        while (true)
        {
            var resource = await GetResourceAsync(accessToken, folderPath, PageSize, offset, ct);
            var embedded = resource.Embedded;
            var items = embedded?.Items ?? [];
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();
                if (string.Equals(item.Type, "dir", StringComparison.OrdinalIgnoreCase))
                {
                    var name = item.Name ?? GetName(item.Path ?? "");
                    await CollectFilesAsync(accessToken, item.Path ?? $"{folderPath}/{name}", $"{prefix}{name}/",
                        depth + 1, result, ct);
                }
                else if (item.Type == "file")
                {
                    var name = item.Name ?? GetName(item.Path ?? "");
                    result.Add(new YandexDiskFileInfo
                    {
                        Path = item.Path ?? $"{folderPath}/{name}",
                        RelativePath = $"{prefix}{name}",
                        Name = name,
                        Size = item.Size ?? 0,
                        Md5 = item.Md5,
                        Modified = item.Modified?.UtcDateTime,
                    });
                }
            }

            offset += items.Count;
            if (items.Count == 0 || embedded?.Total is null || offset >= embedded.Total)
                break;
        }
    }

    private async Task<string> SendAsync(string accessToken, string url, HttpMethod method, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"OAuth {accessToken}");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (response.IsSuccessStatusCode)
            return body;

        var description = TryGetError(body) ?? response.ReasonPhrase ?? "неизвестная ошибка";
        logger.LogWarning("Yandex.Disk API {status} for {url}: {description}", (int)response.StatusCode, url,
            description);

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new UnauthorizedException(
                "Яндекс.Диск отклонил токен. Подключите Диск заново"),
            HttpStatusCode.NotFound => new NotFoundException($"На Яндекс.Диске не найдено: {description}"),
            HttpStatusCode.Forbidden => new BadRequestException(
                $"У приложения нет доступа к ресурсу Яндекс.Диска: {description}"),
            HttpStatusCode.TooManyRequests => new InternalErrorException(
                "Яндекс.Диск ограничил частоту запросов, попробуйте позже"),
            HttpStatusCode.ServiceUnavailable => new InternalErrorException(
                "Яндекс.Диск временно недоступен"),
            _ => new BadRequestException($"Ошибка Яндекс.Диска: {description}"),
        };
    }

    private static string? TryGetError(string body)
    {
        try
        {
            var error = JsonSerializer.Deserialize<ErrorDto>(body);
            return error?.Description ?? error?.Message ?? error?.Error;
        }
        catch (JsonException)
        {
            return null;
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

    private static string ComputeFolderHash(IEnumerable<YandexDiskFileInfo> files)
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

    private static string ComputeFileHash(YandexDiskFileInfo file)
    {
        var payload = $"{file.Size}|{file.Modified?.Ticks ?? 0}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static string? SuggestEntry(IReadOnlyList<YandexDiskFileInfo> files)
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

    private static string GetName(string path)
    {
        var index = path.TrimEnd('/').LastIndexOf('/');
        return index < 0 ? path : path[(index + 1)..];
    }

    private sealed class ResourceDto
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("path")] public string? Path { get; init; }
        [JsonPropertyName("type")] public string? Type { get; init; }
        [JsonPropertyName("size")] public long? Size { get; init; }
        [JsonPropertyName("md5")] public string? Md5 { get; init; }
        [JsonPropertyName("modified")] public DateTimeOffset? Modified { get; init; }
        [JsonPropertyName("_embedded")] public ResourceListDto? Embedded { get; init; }
    }

    private sealed class ResourceListDto
    {
        [JsonPropertyName("items")] public List<ResourceDto>? Items { get; init; }
        [JsonPropertyName("limit")] public int? Limit { get; init; }
        [JsonPropertyName("offset")] public int? Offset { get; init; }
        [JsonPropertyName("total")] public int? Total { get; init; }
    }

    private sealed class LinkDto
    {
        [JsonPropertyName("href")] public string? Href { get; init; }
        [JsonPropertyName("method")] public string? Method { get; init; }
        [JsonPropertyName("templated")] public bool? Templated { get; init; }
    }

    private sealed class ErrorDto
    {
        [JsonPropertyName("error")] public string? Error { get; init; }
        [JsonPropertyName("description")] public string? Description { get; init; }
        [JsonPropertyName("message")] public string? Message { get; init; }
    }
}
