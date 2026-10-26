using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class LlmUsageConverter
{
    public static LlmUsageGroupDto ToDto(this LlmUsageGroup group)
    {
        return new LlmUsageGroupDto
        {
            ReportId = group.ReportId,
            ModelId = group.ModelId,
            TotalTokens = group.TotalTokens,
            TotalRequests = group.TotalRequests,
        };
    }

    public static LlmUsageIntervalDto ToDto(this LlmUsageInterval interval)
    {
        return new LlmUsageIntervalDto
        {
            StartTime = interval.StartTime,
            EndTime = interval.EndTime,
            TotalTokens = interval.TotalTokens,
            TotalRequests = interval.TotalRequests,
        };
    }
}