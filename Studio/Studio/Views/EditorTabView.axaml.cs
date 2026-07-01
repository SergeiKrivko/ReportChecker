using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class EditorTabView : ReactiveUserControl<EditorTabViewModel>
{
    public EditorTabView()
    {
        InitializeComponent();
    }
}