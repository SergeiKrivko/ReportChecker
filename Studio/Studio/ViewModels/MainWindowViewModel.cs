namespace ReportChecker.Studio.ViewModels;

public class MainWindowViewModel(
    ProjectSelectorViewModel projectSelectorViewModel,
    EditorViewModel editorViewModel,
    RightPanelViewModel rightPanelViewModel,
    AuthButtonViewModel authButtonViewModel,
    ReportPanelViewModel reportPanelViewModel) : ViewModelBase
{
    public ProjectSelectorViewModel ProjectSelectorViewModel => projectSelectorViewModel;
    public AuthButtonViewModel AuthButtonViewModel => authButtonViewModel;
    public ReportPanelViewModel ReportPanelViewModel => reportPanelViewModel;
    public RightPanelViewModel RightPanelViewModel => rightPanelViewModel;
    public EditorViewModel EditorViewModel => editorViewModel;
}