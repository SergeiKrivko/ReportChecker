using Microsoft.Extensions.DependencyInjection;
using ReportChecker.Shared.Abstractions;
using ReportChecker.Shared.ApiClient;
using ReportChecker.Studio.Abstractions;
using IAuthService = ReportChecker.Studio.Abstractions.IAuthService;
using IReportService = ReportChecker.Studio.Abstractions.IReportService;

namespace ReportChecker.Studio.Services;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddServices()
        {
            services.AddApiClient();

            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IIssueService, IssueService>();
            services.AddScoped<ICommentsService, CommentsService>();
            services.AddSingleton<ILanguageService, LanguageService>();
            services.AddSingleton<ISubscriptionService, SubscriptionService>();
            services.AddSingleton<IWebLinksService, WebLinksService>();
            services.AddSingleton<IAlertService, AlertService>();

            return services;
        }
    }
}