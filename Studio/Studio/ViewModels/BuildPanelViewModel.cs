using System;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class BuildPanelViewModel(ILanguageService languageService, IAlertService alertService, IFileService fileService) : ViewModelBase
{
    public bool IsProgress
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private CancellationTokenSource? _ctSource;

    public async Task RunBuild()
    {
        try
        {
            if (_ctSource != null)
                await _ctSource.CancelAsync();

            IsProgress = true;
            _ctSource = new CancellationTokenSource();
            var result = await languageService.BuildProjectAsync(_ctSource.Token);
            if (result.IsSuccess)
                alertService.SendAlert(AlertType.Success, "Сборка завершена успешно");
            else
                alertService.SendAlert(AlertType.Error, "Ошибка при сборке");

            foreach (var problem in result.Problems)
            {
                Console.WriteLine(
                    $"{problem.FilePath} -- {problem.LineNumber} ({problem.Type})\n    {problem.Message}\n{problem.Source}\n");
            }

            foreach (var artifact in result.Artifacts)
                await fileService.OpenFile(artifact);

            _ctSource = null;
            IsProgress = false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task CancelBuild()
    {
        if (_ctSource != null)
            await _ctSource.CancelAsync();
        IsProgress = false;
    }
}