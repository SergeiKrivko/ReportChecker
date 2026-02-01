using ReportChecker.Abstractions;
using ReportChecker.FormatProviders.Latex;
using ReportChecker.SourceProviders.File;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.Application.Services;

public class ProviderService(FileSourceProvider fileSourceProvider, LatexFormatProvider latexFormatProvider) : IProviderService
{
    private readonly Dictionary<string, ISourceProvider> _sourceProviders = new()
    {
        { fileSourceProvider.Key, fileSourceProvider },
    };

    public ISourceProvider GetSourceProvider(string providerName)
    {
        return _sourceProviders[providerName];
    }

    private readonly Dictionary<string, IFormatProvider> _formatProviders = new()
    {
        { latexFormatProvider.Key, latexFormatProvider },
    };

    public IFormatProvider GetFormatProvider(string providerName)
    {
        return _formatProviders[providerName];
    }
}