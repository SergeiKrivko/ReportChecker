using ReportChecker.Abstractions;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.Application.Services;

public class ProviderService(
    IEnumerable<ISourceProvider> sourceProviders,
    IEnumerable<IFormatProvider> formatProviders) : IProviderService
{
    public ISourceProvider GetSourceProvider(string providerName)
    {
        return sourceProviders.First(e => e.Key == providerName);
    }

    public IFormatProvider GetFormatProvider(string providerName)
    {
        return formatProviders.First(e => e.Key == providerName);
    }
}