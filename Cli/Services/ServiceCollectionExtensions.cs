using Avalux.Auth.UserClient;
using AvaluxUI.Utils;
using Microsoft.Extensions.DependencyInjection;
using ReportChecker.Cli.Abstractions;
using ReportChecker.Cli.Services.Services;

namespace ReportChecker.Cli.Services;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddServices()
        {
            services.AddHttpClient("AvaluxAuth",
                client => { client.BaseAddress = new Uri("https://auth.nachert.art"); });
            services.AddScoped<IAuthClient, AuthClient>(provider =>
            {
                var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var settings = provider.GetRequiredService<ISettingsSection>();
                return new AuthClient(clientFactory.CreateClient("AvaluxAuth"),
                    "7c4a1272396979451d2a2d311f087050",
                    "64305c89201a3fce41287675d2c9b0a5",
                    new CredentialsStore(settings));
            });

            services.AddTransient<ApiHttpMessageHandler>();
            services.AddHttpClient("ReportCheckerApi")
                .AddHttpMessageHandler<ApiHttpMessageHandler>()
                .ConfigureHttpClient(client => { client.BaseAddress = new Uri("https://reportchecker.nachert.art"); });
            services.AddScoped<IApiClient, ApiClient>(provider =>
            {
                var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
                return new ApiClient(clientFactory.CreateClient("ReportCheckerApi"));
            });

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IReportService, ReportService>();

            return services;
        }
    }
}