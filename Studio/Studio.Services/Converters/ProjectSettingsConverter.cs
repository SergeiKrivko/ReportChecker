using ReportChecker.Shared.Models;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.Services.Dtos;

namespace ReportChecker.Studio.Services.Converters;

internal static class ProjectSettingsConverter
{
    public static Project ToDomain(this ProjectSettings dto, Report? report)
    {
        return new Project
        {
            Id = dto.Id,
            Name = dto.Name,
            Path = dto.Path,
            Format = dto.Format,
            Report = report?.Id == dto.ReportId ? report : null,
        };
    }

    public static ProjectSettings ToSettings(this Project dto)
    {
        return new ProjectSettings
        {
            Id = dto.Id,
            Name = dto.Name,
            Path = dto.Path,
            Format = dto.Format,
            ReportId = dto.Report?.Id,
        };
    }
}