using ReportChecker.Abstractions;
using ReportChecker.FormatProviders.Latex;
using ReportChecker.FormatProviders.Pdf;
using ReportChecker.SourceProviders.File;
using ReportChecker.SourceProviders.GitHub;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.Application.Services;

public class ProviderService(
    FileSourceProvider fileSourceProvider,
    GitHubSourceProvider gitHubSourceProvider,
    LatexFormatProvider latexFormatProvider,
    PdfFormatProvider pdfFormatProvider) : IProviderService
{
    private readonly Dictionary<string, ISourceProvider> _sourceProviders = new()
    {
        { fileSourceProvider.Key, fileSourceProvider },
        { gitHubSourceProvider.Key, gitHubSourceProvider },
    };

    public ISourceProvider GetSourceProvider(string providerName)
    {
        return _sourceProviders[providerName];
    }

    private readonly Dictionary<string, IFormatProvider> _formatProviders = new()
    {
        { latexFormatProvider.Key, latexFormatProvider },
        { pdfFormatProvider.Key, pdfFormatProvider },
    };

    public IFormatProvider GetFormatProvider(string providerName)
    {
        return _formatProviders[providerName];
    }
}