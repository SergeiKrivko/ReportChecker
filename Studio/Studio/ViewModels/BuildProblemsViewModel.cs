using System;
using System.Collections.Generic;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class BuildProblemsViewModel(ILanguageService languageService, IFileService fileService) : ViewModelBase
{
    public IObservable<IReadOnlyList<BuildProblem>> Problems => languageService.BuildProblems;

    public void JumpToCode(BuildProblem problem)
    {
        if (problem.FilePath == null)
            return;
        fileService.JumpToFile(problem.FilePath, problem.LineNumber ?? 1);
    }
}