using System.Reactive.Subjects;
using AvaluxUI.Utils;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;
using Studio.Services.Converters;
using Studio.Services.Dtos;

namespace Studio.Services;

public class ProjectService(ISettingsSection settings) : IProjectService
{
    private readonly Subject<Project?> _currentProject = new();
    public IObservable<Project?> CurrentProject => _currentProject;

    private async Task<IReadOnlyList<Project>> GetAllProjects(CancellationToken ct = default)
    {
        var projects = await settings.Get<ProjectSettings[]>("projects");
        return projects?.Select(e => e.ToDomain(null)).ToList() ?? [];
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
            Name = Path.GetFileName(path),
            Format = "Latex",
            Report = null,
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
}