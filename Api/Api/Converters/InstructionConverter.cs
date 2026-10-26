using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class InstructionConverter
{
    public static InstructionDto ToDto(this Instruction instruction)
    {
        return new InstructionDto
        {
            Id = instruction.Id,
            ReportId = instruction.ReportId,
            UserId = instruction.UserId,
            CommentId = instruction.CommentId,
            Content = instruction.Content,
            CreatedAt = instruction.CreatedAt,
            DeletedAt = instruction.DeletedAt,
        };
    }
}