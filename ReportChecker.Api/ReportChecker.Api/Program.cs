using System.Text.Json.Serialization;
using AiAgent;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ReportChecker.Abstractions;
using ReportChecker.Application.Services;
using ReportChecker.AuthProviders.Yandex;
using ReportChecker.DataAccess;
using ReportChecker.DataAccess.Repositories;
using ReportChecker.FormatProviders.Latex;
using ReportChecker.S3;
using ReportChecker.SourceProviders.File;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ReportCheckerDbContext>(options =>
{
    options.UseNpgsql(Environment.GetEnvironmentVariable("DB_CONNECTION_STRING"));
});

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ICheckRepository, CheckRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IIssueRepository, IssueRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IFileRepository, S3Repository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<ICheckService, CheckService>();
builder.Services.AddScoped<IAiAgentClient, AiAgent.AiAgent>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddSingleton<IProviderService, ProviderService>();

builder.Services.AddSingleton<YandexAuthProvider>();
builder.Services.AddSingleton<FileSourceProvider>();
builder.Services.AddSingleton<LatexFormatProvider>();

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            RequireSignedTokens = false,
            RequireExpirationTime = false,
            RequireAudience = false,
            SignatureValidator = (token, _) => new JsonWebToken(token),
        };

        // Отключаем события, которые могут вызывать исключения
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = _ => Task.CompletedTask,
            OnTokenValidated = _ => Task.CompletedTask,
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
);

app.UseAuthorization();

app.MapControllers();

app.Run();