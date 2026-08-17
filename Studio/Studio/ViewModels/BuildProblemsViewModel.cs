using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class BuildProblemsViewModel(ILanguageService languageService, IFileService fileService) : ViewModelBase
{
    public IObservable<IReadOnlyList<BuildProblem>> Problems => languageService.BuildProblems;

    public IObservable<bool> HasProblems => Problems.Select(e => e.Count > 0);

    public void JumpToCode(BuildProblem problem)
    {
        if (problem.FilePath == null)
            return;
        fileService.JumpToFile(problem.FilePath, problem.LineNumber ?? 1);
    }
}