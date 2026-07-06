using ReactiveUI.Avalonia;
using ReportChecker.Studio.ViewModels;

namespace ReportChecker.Studio.Views;

public partial class AuthButtonView : ReactiveUserControl<AuthButtonViewModel>
{
    public AuthButtonView()
    {
        InitializeComponent();
    }
}