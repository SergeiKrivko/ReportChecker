using ReportChecker.Api.Dtos;
using ReportChecker.Models;

namespace ReportChecker.Api.Converters;

public static class InstructionTaskConverter
{
    public static InstructionTaskDto ToDto(this InstructionTask task)
    {
        return new InstructionTaskDto
        {
            Id = task.Id,
            ReportId = task.ReportId,
            Status = task.Status,
            Mode = task.Mode,
            Instruction = task.Instruction,
        };
    }
}