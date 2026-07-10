using Avalonia;
using System;
using System.IO;
using AvaluxUI.Utils;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.Avalonia;
using ReactiveUI.Avalonia.Splat;
using ReportChecker.Shared.FormatProviders.Latex;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.ViewModels;
using ReportChecker.Studio.Views;
using Studio.LanguageProviders.Latex;
using ReportChecker.Studio.Services;
using IFormatProvider = ReportChecker.Shared.Abstractions.IFormatProvider;

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
                    services.AddServices();
                    services.AddSingleton<IFormatProvider, LatexFormatProvider>();
                    services.AddSingleton<ILanguageProviderFactory, LatexLanguageProviderFactory>();
                    services.AddSingleton<ISettingsSection>(_ =>
                        SettingsFile.Open(Path.Join(Config.DataPath, "settings.xml")));

                    // ViewModels
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<RightPanelViewModel>();
                    services.AddSingleton<ProjectSelectorViewModel>();
                    services.AddSingleton<AuthButtonViewModel>();
                    services.AddSingleton<FileSystemViewModel>();
                    services.AddSingleton<EditorViewModel>();
                    services.AddSingleton<IssueListViewModel>();
                    services.AddSingleton<CommentsViewModel>();
                    services.AddSingleton<ReportPanelViewModel>();

                    //Views
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<RightPanelView>();
                    services.AddSingleton<ProjectSelectorView>();
                    services.AddSingleton<AuthButtonView>();
                    services.AddSingleton<FileSystemView>();
                    services.AddSingleton<EditorView>();
                    services.AddTransient<EditorTabView>();
                    services.AddTransient<EditorFileView>();
                    services.AddSingleton<IssueListView>();
                    services.AddTransient<IssueView>();
                    services.AddSingleton<CommentsView>();
                    services.AddSingleton<ReportPanelView>();
                },
                withResolver: sp => { ServiceProvider = sp; }
            );
}