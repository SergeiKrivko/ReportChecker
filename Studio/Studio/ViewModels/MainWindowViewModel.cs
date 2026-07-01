namespace ReportChecker.Studio.ViewModels;

public class MainWindowViewModel(
    ProjectSelectorViewModel projectSelectorViewModel,
    ViewModels.FileSystemViewModel fileSystemViewModel,
    ViewModels.EditorViewModel editorViewModel) : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";
    public ProjectSelectorViewModel ProjectSelectorViewModel => projectSelectorViewModel;
    public FileSystemViewModel FileSystemViewModel => fileSystemViewModel;
    public EditorViewModel EditorViewModel => editorViewModel;
}