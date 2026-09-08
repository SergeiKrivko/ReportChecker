using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiHelperService(IHttpClientFactory httpClientFactory) : IAiHelperService
{
    public async Task<IReadOnlyList<ModelPrice>> GetModelsPricingAsync(CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("PolzaAi");
        var resp = await client.GetFromJsonAsync<PricingResponseSchema>("https://polza.ai/api/v1/models?type=chat", ct) ??
                   throw new Exception("Empty response");
        return resp.Data.Select(e => new ModelPrice
        {
            ModelId = e.Id,
            InputRubPerMillion = Convert.ToDecimal(e.TopProvider?.Pricing.PromptPerMillion),
            OutputRubPerMillion = Convert.ToDecimal(e.TopProvider?.Pricing.CompletionPerMillion),
        }).ToList();
    }

    private class PricingResponseSchema
    {
        public required IReadOnlyList<PricingResponseModelSchema> Data { get; init; }
    }

    private class PricingResponseModelSchema
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        [JsonPropertyName("top_provider")] public PricingResponseProviderSchema? TopProvider { get; init; }
    }

    private class PricingResponseProviderSchema
    {
        public required PricingResponseProviderPricingSchema Pricing { get; init; }
    }

    private class PricingResponseProviderPricingSchema
    {
        [JsonPropertyName("prompt_per_million")] public string? PromptPerMillion { get; init; }
        [JsonPropertyName("completion_per_million")] public string? CompletionPerMillion { get; init; }
    }
}