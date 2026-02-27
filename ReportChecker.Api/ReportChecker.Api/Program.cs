using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using AiAgent;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Octokit.Webhooks.AspNetCore;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.DataAccess;
using ReportChecker.DataAccess.Repositories;
using ReportChecker.FormatProviders.Latex;
using ReportChecker.FormatProviders.Pdf;
using ReportChecker.S3;
using ReportChecker.SourceProviders.File;
using ReportChecker.SourceProviders.GitHub;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ReportCheckerDbContext>(options =>
{
    options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"));
});

builder.Services.AddScoped<ICheckRepository, CheckRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IInstructionRepository, InstructionRepository>();
builder.Services.AddSingleton<IFileRepository, S3Repository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICheckService, CheckService>();
builder.Services.AddScoped<IAiAgentClient, AiAgent.AiAgent>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddSingleton<IProviderService, ProviderService>();
builder.Services.AddSingleton<IUserRepository, AuthApiClient>();
builder.Services.AddSingleton<GithubService>();
builder.Services.AddScoped<GithubWebhookProcessor>();

builder.Services.AddSingleton<FileSourceProvider>();
builder.Services.AddSingleton<GitHubSourceProvider>();
builder.Services.AddSingleton<LatexFormatProvider>();
builder.Services.AddSingleton<PdfFormatProvider>();

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
    .WithOrigins("http://localhost:4200", "https://report-checker.vercel.app")
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.UseAuthorization();

app.MapControllers();
app.MapGitHubWebhooks();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<ReportCheckerDbContext>().Database.MigrateAsync();
}

app.Run();