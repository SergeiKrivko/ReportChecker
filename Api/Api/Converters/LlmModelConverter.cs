using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class LlmModelConverter
{
    public static LlmModelDto ToDto(this LlmModel model, LLmModelPrice? price = null)
    {
        return new LlmModelDto
        {
            Id = model.Id,
            DisplayName = model.DisplayName,
            ModelKey = model.ModelKey,
            InputCoefficient = price == null ? model.InputCoefficient : (double)price.InputCoefficient,
            OutputCoefficient = price == null ? model.OutputCoefficient : (double)price.OutputCoefficient,
            DefaultReasoningLevel = model.DefaultReasoningLevel,
            IsDefault = model.IsDefault,
            IsRecommended = model.IsRecommended,
            CreatedAt = model.CreatedAt,
            DeletedAt = model.DeletedAt,
        };
    }
}