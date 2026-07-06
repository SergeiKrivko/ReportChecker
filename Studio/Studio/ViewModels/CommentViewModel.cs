using System;
using System.Collections.Generic;
using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.ViewModels;

public class CommentViewModel(Comment comment) : ViewModelBase
{
    public string? Content => comment.Content;
    public IssueStatus? Status => comment.Status;
    public bool IsFirstComment { get; init; }
    public bool IsReopen => comment.Status == IssueStatus.Open && !IsFirstComment;
    public bool IsClose => comment.Status == IssueStatus.Closed;
    public bool IsFixed => comment.Status == IssueStatus.Fixed;
    public bool IsBot => comment.UserId == Guid.Empty;

    public bool HasPatch => comment.Patch != null;
    public bool IsPatchApplied => comment.Patch?.Status == PatchStatus.Applied;
    public bool IsPatchRejected => comment.Patch?.Status == PatchStatus.Rejected;
    public bool IsPatchReadyToApply => comment.Patch?.Status == PatchStatus.Completed;
    public IReadOnlyCollection<PatchLine> PatchLines => comment.Patch?.Lines ?? [];
}