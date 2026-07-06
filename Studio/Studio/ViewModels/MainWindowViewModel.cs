namespace ReportChecker.Studio.ViewModels;

public class MainWindowViewModel(
    ProjectSelectorViewModel projectSelectorViewModel,
    EditorViewModel editorViewModel,
    ViewModels.RightPanelViewModel rightPanelViewModel) : ViewModelBase
{
    public ProjectSelectorViewModel ProjectSelectorViewModel => projectSelectorViewModel;
    public RightPanelViewModel RightPanelViewModel => rightPanelViewModel;
    public EditorViewModel EditorViewModel => editorViewModel;
}