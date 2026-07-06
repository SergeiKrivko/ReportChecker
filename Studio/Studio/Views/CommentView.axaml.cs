using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class CommentView : ReactiveUserControl<CommentViewModel>
{
    public CommentView()
    {
        InitializeComponent();
    }
}