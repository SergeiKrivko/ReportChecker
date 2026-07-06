using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ReportChecker.Studio.Views;

namespace ReportChecker.Studio;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewLocator = new ViewLocator(Program.ServiceProvider!);
            DataTemplates.Add(viewLocator);

            desktop.MainWindow = Program.ServiceProvider!.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}