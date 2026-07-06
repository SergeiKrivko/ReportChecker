using System.Diagnostics;
using ReportChecker.Studio.Abstractions;

namespace ReportChecker.Studio.Services;

public class WebLinksService : IWebLinksService
{
    private const string CiteUrl = "https://reportchecker.ru";

    public void GoToSubscriptions() => OpenUrl($"{CiteUrl}/subscriptions");
    public void GoToAccounts() => OpenUrl($"{CiteUrl}/auth");
    public void GoToStatistics() => OpenUrl($"{CiteUrl}/statistics");

    private static void OpenUrl(string url)
    {
        if (OperatingSystem.IsWindows())
            Process.Start(new ProcessStartInfo
                { FileName = "cmd", Arguments = $"/c start {url[0]}\"{url.AsSpan(1)}\"", CreateNoWindow = true });
        else if (OperatingSystem.IsLinux())
            Process.Start(new ProcessStartInfo
                { FileName = "xdg-open", Arguments = $"\"{url}\"", CreateNoWindow = true });
        else if (OperatingSystem.IsMacOS())
            Process.Start(new ProcessStartInfo { FileName = "open", Arguments = $"\"{url}\"", CreateNoWindow = true });
    }
}