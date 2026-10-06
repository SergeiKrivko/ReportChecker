using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.Application.Services;

/// <summary>
/// Загружает тесты бенчмарка из каталога данных (по умолчанию <c>Benchmarks</c> рядом с приложением).
/// Каждый тест — подкаталог с <c>case.json</c> и исходником.
/// </summary>
public class BenchmarkCaseProvider : IBenchmarkCaseProvider
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly IProviderService _providerService;
    private readonly ILogger<BenchmarkCaseProvider> _logger;
    private readonly Lazy<IReadOnlyList<BenchmarkCase>> _cases;

    public BenchmarkCaseProvider(IConfiguration configuration, IProviderService providerService,
        ILogger<BenchmarkCaseProvider> logger)
    {
        _providerService = providerService;
        _logger = logger;
        DataPath = Path.IsPathRooted(configuration["Benchmarks.DataPath"])
            ? configuration["Benchmarks.DataPath"]!
            : Path.Combine(AppContext.BaseDirectory, configuration["Benchmarks.DataPath"] ?? "Benchmarks");
        _cases = new Lazy<IReadOnlyList<BenchmarkCase>>(LoadCases);
    }

    public string DataPath { get; }

    public IReadOnlyList<BenchmarkCase> GetCases()
    {
        return _cases.Value;
    }

    public BenchmarkCase? GetCase(string id)
    {
        return GetCases().FirstOrDefault(e => e.Id == id);
    }

    public async Task<IReadOnlyList<Chapter>> GetChaptersAsync(BenchmarkCase benchmarkCase,
        CancellationToken ct = default)
    {
        if (benchmarkCase.ValidationError != null)
            throw new InvalidOperationException($"Тест '{benchmarkCase.Id}' некорректен: {benchmarkCase.ValidationError}");

        var directory = Path.Combine(DataPath, benchmarkCase.Id);
        var archive = new BenchmarkDirectoryArchive(directory, benchmarkCase.EntryFile);
        var formatProvider = _providerService.GetFormatProvider(benchmarkCase.Format);
        return await formatProvider.GetChaptersAsync(archive);
    }

    private IReadOnlyList<BenchmarkCase> LoadCases()
    {
        if (!Directory.Exists(DataPath))
        {
            _logger.LogWarning("Каталог тестов бенчмарка '{path}' не найден", DataPath);
            return [];
        }

        var result = new List<BenchmarkCase>();
        foreach (var directory in Directory.EnumerateDirectories(DataPath).OrderBy(e => e))
        {
            var caseFile = Path.Combine(directory, "case.json");
            if (!File.Exists(caseFile))
                continue;
            result.Add(LoadCase(directory, caseFile));
        }

        if (result.Count == 0)
            _logger.LogWarning("В каталоге тестов бенчмарка '{path}' не найдено ни одного теста", DataPath);
        return result;
    }

    private BenchmarkCase LoadCase(string directory, string caseFile)
    {
        var directoryId = Path.GetFileName(directory);
        CaseSchema? schema;
        try
        {
            using var stream = File.OpenRead(caseFile);
            schema = JsonSerializer.Deserialize<CaseSchema>(stream, JsonSerializerOptions);
            if (schema == null)
                return Invalid(directoryId, "Файл case.json пуст");
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "Не удалось разобрать тест бенчмарка '{file}'", caseFile);
            return Invalid(directoryId, $"Некорректный JSON: {e.Message}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось прочитать тест бенчмарка '{file}'", caseFile);
            return Invalid(directoryId, $"Не удалось прочитать файл: {e.Message}");
        }

        var id = string.IsNullOrWhiteSpace(schema.Id) ? directoryId : schema.Id!;
        var expected = (schema.Expected ?? []).Select(ToExpected).ToArray();
        var validationError = Validate(id, schema, expected, directory);
        return new BenchmarkCase
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(schema.Name) ? id : schema.Name!,
            Format = schema.Format ?? "",
            EntryFile = string.IsNullOrWhiteSpace(schema.EntryFile) ? null : schema.EntryFile,
            Description = schema.Description,
            DisplayMode = schema.DisplayMode ?? BenchmarkCaseDisplayMode.Collapsed,
            Expected = expected,
            ValidationError = validationError,
        };
    }

    private string? Validate(string id, CaseSchema schema, IReadOnlyCollection<BenchmarkExpectedIssue> expected,
        string directory)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "Не задан идентификатор теста";
        if (string.IsNullOrWhiteSpace(schema.Format))
            return "Не задан формат (format)";
        if (_providerService.GetFormatProviderOrDefault(schema.Format) == null)
            return $"Неизвестный формат '{schema.Format}'";
        if (string.IsNullOrWhiteSpace(schema.EntryFile))
            return "Не задан файл-точка входа (entryFile)";
        if (!File.Exists(Path.Combine(directory, schema.EntryFile)))
            return $"Файл '{schema.EntryFile}' не найден в каталоге теста";
        if (expected.Count == 0)
            return "Не задано ни одной известной ошибки (expected)";

        var duplicate = expected.GroupBy(e => e.Number).FirstOrDefault(e => e.Count() > 1);
        if (duplicate != null)
            return $"Номер известной ошибки '{duplicate.Key}' повторяется";

        var emptyTitle = expected.FirstOrDefault(e => string.IsNullOrWhiteSpace(e.Title));
        if (emptyTitle != null)
            return $"У известной ошибки '{emptyTitle.Number}' не задан заголовок";
        var emptyChapter = expected.FirstOrDefault(e => string.IsNullOrWhiteSpace(e.Chapter));
        if (emptyChapter != null)
            return $"У известной ошибки '{emptyChapter.Number}' не задана глава";

        return null;
    }

    private static BenchmarkCase Invalid(string id, string error)
    {
        return new BenchmarkCase
        {
            Id = id,
            Name = id,
            Format = "",
            ValidationError = error,
        };
    }

    private static BenchmarkExpectedIssue ToExpected(ExpectedSchema schema)
    {
        return new BenchmarkExpectedIssue
        {
            Number = schema.Number,
            Chapter = schema.Chapter ?? "",
            Line = schema.Line,
            Title = schema.Title ?? "",
            Comment = schema.Comment ?? "",
            Priority = schema.Priority ?? 1,
            ErrorClass = schema.Class,
            CheckFix = schema.CheckFix ?? false,
            Patch = schema.Patch?.Select(e => new PatchLine
            {
                Number = e.Number,
                Content = e.Content,
                Type = e.Type,
            }).ToArray(),
        };
    }

    private sealed class CaseSchema
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Format { get; init; }
        public string? EntryFile { get; init; }
        public BenchmarkCaseDisplayMode? DisplayMode { get; init; }
        public ExpectedSchema[]? Expected { get; init; }
    }

    private sealed class ExpectedSchema
    {
        public int Number { get; init; }
        public string? Chapter { get; init; }
        public int? Line { get; init; }
        public string? Title { get; init; }
        public string? Comment { get; init; }
        public int? Priority { get; init; }
        public string? Class { get; init; }
        public bool? CheckFix { get; init; }
        public PatchSchema[]? Patch { get; init; }
    }

    private sealed class PatchSchema
    {
        public int Number { get; init; }
        public string? Content { get; init; }
        public PatchLineType Type { get; init; }
    }
}
