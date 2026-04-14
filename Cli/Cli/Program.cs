using AvaluxUI.Utils;
using Cli.FormatProviders.Latex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReportChecker.Cli.Abstractions;
using ReportChecker.Cli.Models;
using ReportChecker.Cli.Services;
using Spectre.Console;
using IFormatProvider = ReportChecker.Cli.Abstractions.IFormatProvider;
using PatchLineType = ReportChecker.Cli.Models.PatchLineType;
using ProgressStatus = ReportChecker.Cli.Models.ProgressStatus;

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    Environment.Exit(0);
};

if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
    throw new NotSupportedException("Unsupported OS.");

var services = new ServiceCollection();

var configuration = new ConfigurationBuilder();
services.AddSingleton<IConfiguration>(configuration.Build());
services.AddLogging(builder => builder.AddConsole());

services.AddServices();
services.AddSingleton<ISettingsSection>(_ =>
    SettingsFile.Open(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SergeiKrivko", "ReportChecker", "settings.xml")));

services.AddSingleton<IFormatProvider, LatexFormatProvider>();

var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
try
{
    var scope = serviceProvider.CreateScope();

    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    if (!await authService.IsAuthenticatedAsync())
    {
        var authProvider = AnsiConsole.Prompt(new SelectionPrompt<AuthProvider>()
            .AddChoices(authService.GetProviders())
            .UseConverter(p => p.Name));
        await authService.AuthenticateAsync(authProvider);
    }

    var user = await authService.GetUserAsync();
    AnsiConsole.MarkupLine($"Здравствуйте, [bold green]{user.Accounts.First().Name}[/]!");

    var path = args[0];

    if (!Path.Exists(path))
    {
        AnsiConsole.MarkupLine($"[red]Файл '{path}' не существует[/]");
    }

    var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
    var report = await reportService.UploadAsync(path);

    AnsiConsole.MarkupLine($"Ваш отчет загружен в ReportChecker: " +
                           $"[blue]https://report-checker.vercel.app/reports/{report.Id}[/]");

    var formatProvider = await reportService.GetFormatProviderAsync(path);
    var latestCheck = await reportService.GetCheckAsync(report.Id);

    var delay = TimeSpan.FromSeconds(3);

    while (true)
    {
        var updateTime = await formatProvider.GetUpdateTimeAsync(path);
        if (updateTime > latestCheck.CreatedAt)
        {
            AnsiConsole.MarkupLine($"Обнаружено изменение файлов [yellow]{updateTime:hh:mm:ss}[/]");
            latestCheck = await reportService.GetCheckAsync(report.Id);
            if (latestCheck.Status != ProgressStatus.InProgress)
            {
                AnsiConsole.MarkupLine("Загрузка новой версии");
                await reportService.UploadVersionAsync(report.Id, path);
            }
        }

        var patches = await reportService.GetPatchesAsync(report.Id);
        if (patches.Count > 0)
        {
            AnsiConsole.MarkupLine($"Внесение исправлений ([blue]{patches.Count}[/])");
            foreach (var patch in patches)
            {
                await formatProvider.ApplyPatchAsync(path, patch.Chapter, patch.Lines);
                var added = patch.Lines.Count(e => e.Type != PatchLineType.Delete);
                var deleted = patch.Lines.Count(e => e.Type != PatchLineType.Add);
                AnsiConsole.MarkupLine($"Внесено исправление: [bold green]+{added}[/] [bold red]-{deleted}[/]");
            }
        }

        await Task.Delay(delay);
    }
}
catch (Exception e)
{
    logger.LogCritical(e, "Critical error");
    AnsiConsole.MarkupLine($"[red]Критическая ошибка: {e.Message}[/]");
    return -1;
}