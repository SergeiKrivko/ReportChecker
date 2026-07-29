using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.Services;

public class AlertService : IAlertService
{
    public ObservableCollection<Alert> Alerts { get; } = [];
    private readonly Subject<Alert> _newAlerts = new();

    public IObservable<object?> Initialize()
    {
        return _newAlerts
            .Do(alert => Alerts.Add(alert))
            .SelectMany<Alert, object?>(async alert =>
            {
                await Task.Delay(alert.Duration);
                Alerts.Remove(alert);
                return null;
            });
    }

    public void SendAlert(Alert alert)
    {
        _newAlerts.OnNext(alert);
    }

    public void SendAlert(string text)
    {
        SendAlert(new Alert { Text = text, Type = AlertType.Info });
    }

    public void SendAlert(AlertType type, string text)
    {
        SendAlert(new Alert { Text = text, Type = type });
    }
}