using ReportChecker.Shared.Models;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.Services.Dtos;

namespace ReportChecker.Studio.Services.Converters;

internal static class ProjectSettingsConverter
{
    public static Project ToDomain(this ProjectSettings dto)
    {
        return new Project
        {
            Id = dto.Id,
            Name = dto.Name,
            Path = dto.Path,
            Format = dto.Format,
            ReportId = dto.ReportId,
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
            ReportId = dto.ReportId,
        };
    }
}