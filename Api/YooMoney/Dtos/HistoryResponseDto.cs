using System.Text.Json.Serialization;

namespace YooMoney.Dtos;

public class HistoryResponseDto
{
    [JsonPropertyName("operations")] public IReadOnlyCollection<HistoryOperationDto> Operations { get; init; } = [];
}