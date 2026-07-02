using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace Studio.Services;

public class LanguageService : ILanguageService
{
    private readonly IReadOnlyList<ILanguageProviderFactory> _factories;
    private ILanguageProvider? _currentProvider;

    public LanguageService(IProjectService projectService,
        IEnumerable<ILanguageProviderFactory> languageProviderFactories)
    {
        _factories = languageProviderFactories.ToList();
        projectService.CurrentProject.Subscribe(OnProjectChanged);
    }

    private async void OnProjectChanged(Project? project)
    {
        try
        {
            if (project == null)
            {
                _currentProvider = null;
                return;
            }

            var factory = _factories.Single(e => e.Key == project.Format);
            _currentProvider = await factory.CreateProviderAsync(project);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public LanguageCompletions GetCompletions(string triggerText, string fileText, int offset)
    {
        if (_currentProvider == null)
            return LanguageCompletions.Empty();
        return _currentProvider.GetCompletions(triggerText, fileText, offset);
    }
}