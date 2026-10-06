using ReportChecker.Models;

namespace ReportChecker.Abstractions;

/// <summary>
/// Читает тесты бенчмарка из каталога данных (внутри контейнера).
/// </summary>
public interface IBenchmarkCaseProvider
{
    /// <summary>Все тесты, включая невалидные (у них заполнен <see cref="BenchmarkCase.ValidationError"/>).</summary>
    public IReadOnlyList<BenchmarkCase> GetCases();

    public BenchmarkCase? GetCase(string id);

    /// <summary>Разбирает исходник теста на главы провайдером формата из <see cref="BenchmarkCase.Format"/>.</summary>
    public Task<IReadOnlyList<Chapter>> GetChaptersAsync(BenchmarkCase benchmarkCase,
        CancellationToken ct = default);
}
