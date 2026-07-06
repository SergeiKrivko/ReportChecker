using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class CommentsView : ReactiveUserControl<CommentsViewModel>
{
    public CommentsView()
    {
        InitializeComponent();
    }
}