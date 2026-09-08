using System.Net.Http.Json;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace AiAgent;

public class AiHelperService(IHttpClientFactory httpClientFactory) : IAiHelperService
{
    public async Task<IReadOnlyList<ModelPrice>> GetModelsPricingAsync(CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("polzaAi");
        var resp = await client.GetFromJsonAsync<PricingResponseSchema>("api/v1/models", ct) ??
                   throw new Exception("Empty response");
        return resp.Data.Select(e => new ModelPrice
        {
            ModelId = e.Id,
            InputRubPerMillion = Convert.ToDecimal(e.TopProvider.PromptPerMillion),
            OutputRubPerMillion = Convert.ToDecimal(e.TopProvider.CompletionPerMillion),
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
        public required PricingResponseProviderSchema TopProvider { get; init; }
    }

    private class PricingResponseProviderSchema
    {
        public required string PromptPerMillion { get; init; }
        public required string CompletionPerMillion { get; init; }
    }
}