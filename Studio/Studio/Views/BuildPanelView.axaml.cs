using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class BuildPanelView : ReactiveUserControl<BuildPanelViewModel>
{
    public BuildPanelView()
    {
        InitializeComponent();
    }
}