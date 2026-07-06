using ReportChecker.Shared.Models;

namespace ReportChecker.Studio.Abstractions;

public interface ICommentsService
{
    public IObservable<IReadOnlyList<Comment>> AllComments { get; }
    public IObservable<object> Load();
}