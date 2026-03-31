using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportChecker.Abstractions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class InstructionTaskService(
    IInstructionTaskRepository instructionTaskRepository,
    IInstructionRepository instructionRepository,
    IServiceProvider serviceProvider,
    IReportRepository reportRepository,
    ICheckRepository checkRepository,
    IProviderService providerService,
    ILogger<InstructionTaskService> logger) : IInstructionTaskService
{
    public async Task<IReadOnlyList<InstructionTask>> GetActiveInstructionTasksAsync(Guid reportId,
        CancellationToken ct = default)
    {
        return await instructionTaskRepository.GetAllForReportAsync(reportId);
    }

    public async Task<Guid> CreateInstructionTaskAsync(Guid reportId, Guid instructionId, InstructionTaskMode mode,
        CancellationToken ct = default)
    {
        var instruction = await instructionRepository.GetInstructionByIdAsync(instructionId, ct);
        if (instruction == null)
            throw new NullReferenceException("Instruction not found");
        return await CreateInstructionTaskAsync(reportId, instruction.Content, mode, ct);
    }

    public async Task<Guid> CreateInstructionTaskAsync(Guid reportId, string instruction, InstructionTaskMode mode,
        CancellationToken ct = default)
    {
        var id = await instructionTaskRepository.CreateAsync(reportId, instruction, mode);

        var report = await reportRepository.GetReportByIdAsync(reportId);
        if (report == null)
            throw new ArgumentException($"Report with id {reportId} does not exist");

        var check = await checkRepository.GetLatestCheckOfReportAsync(reportId);
        if (check == null)
            throw new ArgumentException($"Report {reportId} have no checks");

        var sourceProvider = providerService.GetSourceProvider(report.SourceProvider);
        var sourceStream = await sourceProvider.OpenAsync(reportId, check.Id);

        var formatProvider = providerService.GetFormatProvider(report.Format);
        var chapters = await formatProvider.GetChaptersAsync(sourceStream);

        RunInstructionTask(id, report, check, chapters.ToList(), instruction, mode);
        return id;
    }

    private async void RunInstructionTask(Guid taskId, Report report, Check check, List<Chapter> chapters, string instruction,
        InstructionTaskMode mode)
    {
        try
        {
            var service = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IAiService>();
            switch (mode)
            {
                case InstructionTaskMode.Apply:
                    await service.ProcessInstructionApplyAsync(taskId, report.Id, check.Id, chapters, instruction);
                    break;
                case InstructionTaskMode.Search:
                    await service.ProcessInstructionSearchAsync(taskId, report.Id, check.Id, chapters, instruction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Error during comment processing: {e}", e);
        }
    }
}