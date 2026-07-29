using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class AlertsView : ReactiveUserControl<AlertsViewModel>
{
    public AlertsView()
    {
        InitializeComponent();
    }
}