namespace ReportChecker.Studio.Abstractions;

public interface IAuthService : Shared.Abstractions.IAuthService
{
    public IObservable<bool> IsAuthorized { get; }
}