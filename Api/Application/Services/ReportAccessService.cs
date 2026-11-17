using ReportChecker.Abstractions;
using ReportChecker.Exceptions;
using ReportChecker.Models;

namespace ReportChecker.Application.Services;

public class ReportAccessService(IReportRepository reportRepository) : IReportAccessService
{
    public async Task<ReportAccess> RequireAccessAsync(Guid reportId, Guid userId, ReportAccessLevel required,
        CancellationToken ct = default)
    {
        var access = await reportRepository.GetReportAccessAsync(reportId, userId, ct);
        if (access == null || !access.Level.Satisfies(required))
            throw new NotFoundException($"Отчет '{reportId}' не найден либо доступ заблокирован");
        return access;
    }

    public Task<ReportAccess> RequireOwnerAsync(Guid reportId, Guid userId, CancellationToken ct = default)
    {
        return RequireAccessAsync(reportId, userId, ReportAccessLevel.Owner, ct);
    }

    public async Task<Report> RequireSharedReportAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new NotFoundException("Ссылка недействительна либо отозвана");

        var report = await reportRepository.GetReportByShareTokenAsync(token, ct);
        if (report == null)
            throw new NotFoundException("Ссылка недействительна либо отозвана");
        return report;
    }
}
