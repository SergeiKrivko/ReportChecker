using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaluxUI.Utils;
using ReactiveUI;
using ReportChecker.Shared.Abstractions;
using ReportChecker.Shared.Models;
using ReportChecker.Studio.Abstractions;
using ReportChecker.Studio.Models;

namespace ReportChecker.Studio.ViewModels;

public class AuthButtonViewModel(
    IAuthService authService,
    IHttpClientFactory httpClientFactory,
    ISubscriptionService subscriptionService,
    IWebLinksService webLinksService) : ViewModelBase
{
    public bool IsAuthenticated
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public AccountInfo? UserInfo
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public IImage? Avatar
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public CurrentSubscription? Subscription
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public int TokensPercent
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public SubscriptionPlan CurrentPlan
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = new()
    {
        Name = "Free",
    };

    public IReadOnlyList<AuthProvider> AuthProviders { get; } = authService.GetProviders();

    protected override async Task OnActivateAsync(CompositeDisposable disposable)
    {
        await base.OnActivateAsync(disposable);
        await Refresh();
    }

    private async Task Refresh()
    {
        IsAuthenticated = await authService.IsAuthenticatedAsync();
        UserInfo = (await authService.GetUserAsync()).Accounts.FirstOrDefault();
        if (UserInfo != null)
        {
            var client = httpClientFactory.CreateClient();
            using var pictureFile = new TempFile(".png");
            await using (var stream = await client.GetStreamAsync(UserInfo.AvatarUrl))
            await using (var file = pictureFile.OpenWrite())
            {
                await stream.CopyToAsync(file);
            }

            Avatar = new Bitmap(pictureFile.FilePath);

            Subscription = await subscriptionService.GetCurrentSubscriptionAsync();
            TokensPercent = Subscription == null
                ? 0
                : int.Min(100, Subscription.TokensLimit.Current * 100 / Subscription.TokensLimit.Maximum);
            if (Subscription?.Active?.PlanId != CurrentPlan?.Id)
                CurrentPlan = await subscriptionService.GetSubscriptionPlanAsync(Subscription?.Active?.Id);
        }
        else
        {
            Avatar = null;
            Subscription = null;
        }
    }

    [SupportedOSPlatform("Windows")]
    [SupportedOSPlatform("Linux")]
    [SupportedOSPlatform("Macos")]
    public async Task AuthenticateAsync(AuthProvider provider)
    {
        await authService.AuthenticateAsync(provider);
        await Refresh();
    }

    public async Task LogOutAsync()
    {
        await authService.LogOutAsync();
        IsAuthenticated = false;
        UserInfo = null;
        Avatar = null;
        Subscription = null;
    }

    public void GoToSubscriptions() => webLinksService.GoToSubscriptions();
    public void GoToAccounts() => webLinksService.GoToAccounts();
    public void GoToStatistics() => webLinksService.GoToStatistics();
}