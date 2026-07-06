using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class IssueView : ReactiveUserControl<IssueViewModel>
{
    public IssueView()
    {
        InitializeComponent();
    }
}