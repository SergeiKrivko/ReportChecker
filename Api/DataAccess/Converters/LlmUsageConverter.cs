using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class LlmUsageConverter
{
    public static LlmUsage ToDomain(this LlmUsageEntity entity)
    {
        return new LlmUsage
        {
            ModelId = entity.ModelId,
            ReportId = entity.ReportId,
            Type = entity.Type,
            FinishedAt = entity.FinishedAt,
            InputTokens = entity.InputTokens,
            OutputTokens = entity.OutputTokens,
            TotalTokens = entity.TotalTokens,
            TotalRequests = entity.TotalRequests,
        };
    }
}