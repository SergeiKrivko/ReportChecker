using AvaluxUI.Utils;
using ReportChecker.Cli.Abstractions;
using ReportChecker.Cli.Services.Converters;
using IFormatProvider = ReportChecker.Cli.Abstractions.IFormatProvider;

namespace ReportChecker.Cli.Services.Services;

public class ReportService(
    IApiClient apiClient,
    ISettingsSection globalSettings,
    IEnumerable<IFormatProvider> formatProviders) : IReportService
{
    private const string SettingsReportsKey = "reports";
    private const string SettingsClientIdKey = "clientId";

    public async Task<Models.Report> UploadAsync(string path)
    {
        path = Path.GetFullPath(path);
        var reports = await globalSettings.Get<ReportRecord[]>(SettingsReportsKey, []);
        var clientId = await GetClientIdAsync();
        var existing = reports.FirstOrDefault(e => e.Path == path);
        if (existing != null)
        {
            var report = await apiClient.ReportsGETAsync(existing.ReportId);
            return report.ToDomain();
        }

        var provider = await formatProviders
            .ToAsyncEnumerable()
            .FirstAsync(async (e, _) => await e.TestSourceAsync(path));
        var sourcePack = await provider.PackSourcesAsync(path);
        var file = await apiClient.FilesPOSTAsync(FileBucketDto.Local,
            new FileParameter(sourcePack.Stream, sourcePack.FileName));
        var reportId = await apiClient.ReportsPOSTAsync(new CreateReportSchema
        {
            Format = provider.Key,
            Name = Path.GetFileName(path),
            SourceProvider = "Local",
            Source = new ReportSourceUnion
            {
                Local = new LocalReportSource
                {
                    ClientId = clientId,
                    ClientMachineName = Environment.MachineName,
                    InitialFileId = file.Id,
                    EntryFilePath = sourcePack.EntryFilePath,
                }
            }
        });

        await globalSettings.Set(SettingsReportsKey, reports.Concat([new ReportRecord(path, reportId)]));

        return new Models.Report
        {
            Id = reportId,
            Name = Path.GetFileName(path),
        };
    }

    public async Task UploadVersionAsync(Guid reportId, string path)
    {
        path = Path.GetFullPath(path);
        var provider = await formatProviders
            .ToAsyncEnumerable()
            .FirstAsync(async (e, _) => await e.TestSourceAsync(path));
        var sourcePack = await provider.PackSourcesAsync(path);
        var file = await apiClient.FilesPOSTAsync(FileBucketDto.Local,
            new FileParameter(sourcePack.Stream, sourcePack.FileName));
        await apiClient.ChecksAsync(reportId, new CreateCheckSchema
        {
            Name = $"Version {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            Source = new CheckSourceUnion
            {
                Id = file.Id,
            }
        });
    }

    public async Task<Models.Check> GetCheckAsync(Guid reportId)
    {
        var check = await apiClient.LatestAsync(reportId);
        return check.ToDomain();
    }

    public async Task<IFormatProvider> GetFormatProviderAsync(string path)
    {
        return await formatProviders
            .ToAsyncEnumerable()
            .FirstAsync(async (e, _) => await e.TestSourceAsync(path));
    }

    public async Task<IReadOnlyList<Models.Patch>> GetPatchesAsync(Guid reportId)
    {
        var issues = await apiClient.IssuesAllAsync(reportId);
        return issues
            .SelectMany(e => e.Comments
                .Where(c => c.Patch?.Status == PatchStatus.Accepted)
                .Select(c => c.Patch.ToDomain(e.Chapter)))
            .ToList();
    }

    private async Task<Guid> GetClientIdAsync()
    {
        var id = await globalSettings.Get<Guid>(SettingsClientIdKey);
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
            await globalSettings.Set(SettingsClientIdKey, id);
        }

        return id;
    }

    private record ReportRecord(string Path, Guid ReportId);
}