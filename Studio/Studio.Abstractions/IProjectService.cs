using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IProjectService
{
    public IObservable<Project?> CurrentProject { get; }
    public Task<IReadOnlyList<Project>> GetRecentProjects(CancellationToken ct = default);
    public Task OpenProject(Project project, CancellationToken ct = default);
    public Task OpenProject(string path, CancellationToken ct = default);
    public Task OpenLastProject(CancellationToken ct = default);
    public Task SetReportId(Guid projectId, Guid reportId, CancellationToken ct = default);
}