using System.Collections.ObjectModel;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Abstractions;

public interface IAlertService
{
    public ObservableCollection<Alert> Alerts { get; }
    public IObservable<object?> Initialize();
    public void SendAlert(Alert alert);
    public void SendAlert(string text);
    public void SendAlert(AlertType type, string text);
}