using System.Text.Json.Serialization;

namespace YooMoney.Dtos;

internal class HistoryRequestDto
{
    [JsonPropertyName("label")] public string? Label { get; init; }
    [JsonPropertyName("records")] public int RecordsCount { get; init; } = 1;
    [JsonPropertyName("details")] public bool IncludeDetails { get; init; }
}