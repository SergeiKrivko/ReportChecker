using System.Text.Json.Serialization;

namespace YooMoney.Dtos;

public class HistoryOperationDto
{
    [JsonPropertyName("operation_id")] public required string Id { get; init; }
    [JsonPropertyName("status")] public required string Status { get; init; }
    [JsonPropertyName("datetime")] public DateTime? Time { get; init; }
    [JsonPropertyName("title")] public string? Title { get; init; }
    [JsonPropertyName("pattern_id")] public string? PatternId { get; init; }
    [JsonPropertyName("direction")] public string? Direction { get; init; }
    [JsonPropertyName("amount")] public decimal? Amount { get; init; }
    [JsonPropertyName("label")] public string? Label { get; init; }
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("amount_currency")] public string? Currency { get; init; }
    [JsonPropertyName("is_sbp_operation")] public bool IsSbpOperation { get; init; }
}