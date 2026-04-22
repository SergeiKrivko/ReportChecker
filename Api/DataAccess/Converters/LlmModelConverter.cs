using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class LlmModelConverter
{
    public static LlmModel ToDomain(this LlmModelEntity entity, Guid defaultModelId = default)
    {
        return new LlmModel
        {
            Id = entity.Id,
            DisplayName = entity.DisplayName,
            ModelKey = entity.ModelKey,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
            IsDefault = entity.Id == defaultModelId,
        };
    }
}