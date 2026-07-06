namespace ReportChecker.Studio.ViewModels;

public class MainWindowViewModel(
    ProjectSelectorViewModel projectSelectorViewModel,
    EditorViewModel editorViewModel,
    RightPanelViewModel rightPanelViewModel,
    AuthButtonViewModel authButtonViewModel) : ViewModelBase
{
    public ProjectSelectorViewModel ProjectSelectorViewModel => projectSelectorViewModel;
    public AuthButtonViewModel AuthButtonViewModel => authButtonViewModel;
    public RightPanelViewModel RightPanelViewModel => rightPanelViewModel;
    public EditorViewModel EditorViewModel => editorViewModel;
}