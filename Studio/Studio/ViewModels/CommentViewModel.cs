using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.ViewModels;

public class CommentViewModel(Comment comment, IProjectService projectService, IFileService fileService, ICommentsService commentsService, IIssueService issueService) : ViewModelBase
{
    public string? Content => comment.Content;
    public IssueStatus? Status => comment.Status;
    public bool IsFirstComment { get; init; }
    public bool IsReopen => comment.Status == IssueStatus.Open && !IsFirstComment;
    public bool IsClose => comment.Status == IssueStatus.Closed;
    public bool IsFixed => comment.Status == IssueStatus.Fixed;
    public bool IsBot => comment.UserId == Guid.Empty;
    public bool IsProgress => comment.ProgressStatus == ProgressStatus.InProgress;

    public bool HasPatch => comment.Patch != null;
    public bool IsPatchApplied => comment.Patch?.Status == PatchStatus.Applied;
    public bool IsPatchRejected => comment.Patch?.Status == PatchStatus.Rejected;
    public bool IsPatchReadyToApply => comment.Patch?.Status == PatchStatus.Completed;
    public IReadOnlyCollection<PatchLine> PatchLines => comment.Patch?.Lines ?? [];

    public async Task ApplyPatchAsync()
    {
        if (comment.Patch == null)
            return;
        var project = await projectService.CurrentProject.FirstAsync();
        var issue = await issueService.SelectedIssue.FirstAsync();
        if (project == null || issue?.Issue.Chapter == null)
            return;
        var formatProvider = await projectService.GetFormatProviderAsync();
        var filePatch = await formatProvider.PatchToFilePatchAsync(project.Path, issue.Issue.Chapter, comment.Patch.Lines);
        if (filePatch == null)
            return;
        await fileService.ApplyPatch(filePatch);
        await commentsService.SetPatchStatusAsync(comment.Id, PatchStatus.Applied);
    }

    public async Task RejectPatchAsync()
    {
        await commentsService.SetPatchStatusAsync(comment.Id, PatchStatus.Rejected);
    }
}