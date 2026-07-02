using Avalonia;
using System;
using System.IO;
using AvaluxUI.Utils;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.Avalonia;
using ReactiveUI.Avalonia.Splat;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.ViewModels;
using ReportChecker.Studio.Views;
using Studio.LanguageProviders.Latex;
using Studio.Services;

namespace ReportChecker.Studio;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    internal static IServiceProvider? ServiceProvider { get; set; }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUIWithMicrosoftDependencyResolver(
                services =>
                {
                    // Services
                    services.AddSingleton<IProjectService, ProjectService>();
                    services.AddSingleton<ILanguageService, LanguageService>();
                    services.AddSingleton<ILanguageProviderFactory, LatexLanguageProviderFactory>();
                    services.AddSingleton<ISettingsSection>(_ =>
                        SettingsFile.Open(Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "SergeiKrivko", "ReportChecker", "settings.xml")));

                    // ViewModels
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<ProjectSelectorViewModel>();
                    services.AddSingleton<FileSystemViewModel>();
                    services.AddSingleton<EditorViewModel>();

                    //Views
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<ProjectSelectorView>();
                    services.AddSingleton<FileSystemView>();
                    services.AddSingleton<EditorView>();
                    services.AddTransient<EditorTabView>();
                    services.AddTransient<EditorFileView>();
                },
                withResolver: sp => { ServiceProvider = sp; }
            );
}