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
        var file = await apiClient.FilesPOSTAsync(new FileParameter(sourcePack.Stream, sourcePack.FileName));
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