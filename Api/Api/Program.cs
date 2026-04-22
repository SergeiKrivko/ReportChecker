using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using AiAgent;
using Avalux.Auth.ApiClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Octokit.Webhooks;
using Octokit.Webhooks.AspNetCore;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.DataAccess;
using ReportChecker.DataAccess.Repositories;
using ReportChecker.FormatProviders.Docx;
using ReportChecker.FormatProviders.Latex;
using ReportChecker.FormatProviders.Pdf;
using ReportChecker.Models.Sources;
using ReportChecker.S3;
using ReportChecker.SourceProviders.File;
using ReportChecker.SourceProviders.GitHub;
using ReportChecker.SourceProviders.Local;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddDbContext<ReportCheckerDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration["DB_CONNECTION_STRING"]);
});

builder.Services.AddScoped<ICheckRepository, CheckRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IInstructionRepository, InstructionRepository>();
builder.Services.AddScoped<IInstructionTaskRepository, InstructionTaskRepository>();
builder.Services.AddScoped<ICommentReadRepository, CommentReadRepository>();
builder.Services.AddScoped<IPatchRepository, PatchRepository>();
builder.Services.AddScoped<ILlmModelRepository, LlmModelRepository>();
builder.Services.AddScoped<ILlmUsageRepository, LlmUsageRepository>();
builder.Services.AddScoped<IReportSourceRepository<FileReportSource>, FileReportSourceRepository>();
builder.Services.AddScoped<ICheckSourceRepository<FileCheckSource>, FileCheckSourceRepository>();
builder.Services.AddScoped<IReportSourceRepository<GitHubReportSource>, GitHubReportSourceRepository>();
builder.Services.AddScoped<ICheckSourceRepository<GitHubCheckSource>, GitHubCheckSourceRepository>();
builder.Services.AddScoped<IReportSourceRepository<LocalReportSource>, LocalReportSourceRepository>();
builder.Services.AddScoped<ICheckSourceRepository<LocalCheckSource>, LocalCheckSourceRepository>();
builder.Services.AddSingleton<IFileRepository, S3Repository>();

builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICheckService, CheckService>();
builder.Services.AddScoped<IAiAgentFactory, AiAgentFactory>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddScoped<IPatchService, PatchService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<GithubService>();
builder.Services.AddScoped<WebhookEventProcessor, GithubWebhookProcessor>();
builder.Services.AddScoped<ILimitsService, LimitsService>();
builder.Services.AddScoped<IDifferenceService, DifferenceService>();
builder.Services.AddScoped<IChapterGroupService, ChapterGroupService>();
builder.Services.AddScoped<IInstructionTaskService, InstructionTaskService>();
builder.Services.AddAvaluxAuthApiClient(
    builder.Configuration["Security.AuthApiUrl"] ?? "",
    builder.Configuration["Security.ApiToken"] ?? "");
builder.Services.AddScoped<IUserRepository, AvaluxAuthUserRepository>();

builder.Services.AddScoped<ISourceProvider, FileSourceProvider>();
builder.Services.AddScoped<ISourceProvider, GitHubSourceProvider>();
builder.Services.AddScoped<ISourceProvider, LocalSourceProvider>();
builder.Services.AddSingleton<IFormatProvider, LatexFormatProvider>();
builder.Services.AddSingleton<IFormatProvider, PdfFormatProvider>();
builder.Services.AddSingleton<IFormatProvider, DocxFormatProvider>();

builder.Services.AddHttpClient("Auth",
    client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Security.AuthApiUrl"] ?? throw new Exception());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            builder.Configuration["Security.ApiToken"] ?? throw new Exception());
    });

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.MetadataAddress =
            $"{builder.Configuration["Security.AuthApiUrl"]}/api/v1/.well-known/openid-configuration";
        options.Authority = builder.Configuration["Security.AuthApiUrl"];
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Security.Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Security.Audience"],
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        };

        options.RefreshInterval = TimeSpan.FromMinutes(30);
        options.RefreshOnIssuerKeyNotFound = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(policy => policy
    .WithOrigins("http://localhost:4200", app.Configuration["Frontend.Url"] ?? "https://report-checker.vercel.app")
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.UseAuthorization();

app.MapControllers();
app.MapGitHubWebhooks("api/v1/github/webhooks");

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ReportCheckerDbContext>().Database.MigrateAsync();
}

app.Run();