using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class InstructionConverter
{
    public static Instruction ToDomain(this InstructionEntity entity)
    {
        return new Instruction
        {
            Id = entity.Id,
            ReportId = entity.ReportId,
            UserId = entity.UserId,
            CommentId = entity.CommentId,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt,
            DeletedAt = entity.DeletedAt,
        };
    }
}