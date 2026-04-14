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

if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
    throw new NotSupportedException("Unsupported OS.");

var services = new ServiceCollection();

var configuration = new ConfigurationBuilder();
services.AddSingleton<IConfiguration>(configuration.Build());
// services.AddLogging(builder => builder.AddConsole());

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

    if (!Path.Exists(args[0]))
    {
        AnsiConsole.MarkupLine($"[red]Файл '{args[0]}' не существует[/]");
    }

    var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
    await reportService.UploadAsync(args[0]);
}
catch (Exception e)
{
    logger.LogCritical(e, "Critical error");
    AnsiConsole.MarkupLine($"[red]Критическая ошибка: {e.Message}[/]");
    return -1;
}

return 0;
