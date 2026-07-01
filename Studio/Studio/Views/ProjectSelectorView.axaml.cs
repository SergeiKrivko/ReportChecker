using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class ProjectSelectorView : ReactiveUserControl<ProjectSelectorViewModel>
{
    public ProjectSelectorView()
    {
        InitializeComponent();
    }
}