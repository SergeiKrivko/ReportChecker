using System.Reactive.Linq;
using System.Reactive.Subjects;
using AvaluxUI.Utils;
using ReportChecker.Shared.Abstractions;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using ReportChecker.Studio.Services.Converters;
using ReportChecker.Studio.Services.Dtos;
using IFormatProvider = ReportChecker.Shared.Abstractions.IFormatProvider;
using IReportService = ReportChecker.Studio.Abstractions.IReportService;

namespace ReportChecker.Studio.Services;

public class ProjectService(
    ISettingsSection settings,
    IEnumerable<IFormatProvider> formatProviders,
    IReportService reportService) : IProjectService
{
    private readonly BehaviorSubject<Project?> _currentProject = new(null);
    public IObservable<Project?> CurrentProject => _currentProject;

    private async Task<IReadOnlyList<Project>> GetAllProjects(CancellationToken ct = default)
    {
        var projects = await settings.Get<ProjectSettings[]>("projects");
        return projects?.Select(e => e.ToDomain()).ToList() ?? [];
    }

    public async Task<IReadOnlyList<Project>> GetRecentProjects(CancellationToken ct = default)
    {
        var allProjects = await GetAllProjects(ct);
        return allProjects.Take(10).ToList();
    }

    private async Task SelectProject(Project project, IEnumerable<Project> allProjects)
    {
        _currentProject.OnNext(project);
        await settings.Set("currentProject", project.Id);
        var newProjects = allProjects.Where(e => e.Id != project.Id).Prepend(project);
        await settings.Set("projects", newProjects);
        await reportService.SelectReport(project.ReportId);
    }

    public async Task OpenProject(Project project, CancellationToken ct = default)
    {
        var allProjects = await GetAllProjects(ct);
        await SelectProject(project, allProjects);
    }

    public async Task OpenProject(string path, CancellationToken ct = default)
    {
        var allProjects = await GetAllProjects(ct);
        var project = allProjects.FirstOrDefault(e => e.Path == path) ?? new Project
        {
            Id = Guid.NewGuid(),
            Path = path,
            Name = Path.GetFileName(Path.GetDirectoryName(path) ?? path),
            Format = "Latex",
        };
        await SelectProject(project, allProjects);
    }

    public async Task OpenLastProject(CancellationToken ct = default)
    {
        var id = await settings.Get<Guid>("currentProject");
        var allProjects = await GetAllProjects(ct);
        var project = allProjects.FirstOrDefault(e => e.Id == id);
        if (project == null)
            return;
        await SelectProject(project, allProjects);
    }

    public async Task SetReportId(Guid projectId, Guid reportId, CancellationToken ct = default)
    {
        var allProjects = await GetAllProjects(ct);
        var newProjects = allProjects.Select(e => e.Id == projectId
            ? new Project
            {
                Id = e.Id,
                Name = e.Name,
                Path = e.Path,
                Format = e.Format,
                ReportId = reportId
            }
            : e).ToList();
        await settings.Set("projects", newProjects);
    }

    public async Task<SourcePack> PackCurrentProjectAsync(CancellationToken ct = default)
    {
        var project = await CurrentProject.FirstAsync() ?? throw new Exception("Project not selected");
        var sourceProvider = formatProviders.First(e => e.Key == project.Format);
        return await sourceProvider.PackSourcesAsync(project.Path);
    }

    public async Task<IFormatProvider> GetFormatProviderAsync()
    {
        var project = await CurrentProject.FirstAsync() ?? throw new Exception("Project not selected");
        return formatProviders.First(e => e.Key == project.Format);
    }
}