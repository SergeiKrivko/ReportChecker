using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class BuildProblemsView : ReactiveUserControl<BuildProblemsViewModel>
{
    public BuildProblemsView()
    {
        InitializeComponent();
    }
}