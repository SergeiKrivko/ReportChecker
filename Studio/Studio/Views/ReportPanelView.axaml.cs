using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class ReportPanelView : ReactiveUserControl<ReportPanelViewModel>
{
    public ReportPanelView()
    {
        InitializeComponent();
    }
}