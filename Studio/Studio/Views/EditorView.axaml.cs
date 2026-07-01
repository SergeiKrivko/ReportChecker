using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class EditorView : ReactiveUserControl<EditorViewModel>
{
    public EditorView()
    {
        InitializeComponent();
    }
}