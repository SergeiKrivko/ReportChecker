using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow(MainWindowViewModel vm)
    {
        ViewModel = vm;
        InitializeComponent();
    }
}