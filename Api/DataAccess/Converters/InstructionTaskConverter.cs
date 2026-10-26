using ReportChecker.DataAccess.Entities;
using ReportChecker.Models;

namespace ReportChecker.DataAccess.Converters;

public static class InstructionTaskConverter
{
    public static InstructionTask ToDomain(this InstructionTaskEntity entity)
    {
        return new InstructionTask
        {
            Id = entity.Id,
            ReportId = entity.ReportId,
            Status = entity.Status,
            Instruction = entity.Instruction,
            Mode = entity.Mode,
        };
    }
}