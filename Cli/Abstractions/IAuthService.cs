using ReportChecker.Cli.Models;

namespace ReportChecker.Cli.Abstractions;

public interface IAuthService
{
    public IReadOnlyList<AuthProvider> GetProviders();
}