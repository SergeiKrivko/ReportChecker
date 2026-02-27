using ReportChecker.Abstractions;
using ReportChecker.FormatProviders.Latex;
using ReportChecker.FormatProviders.Pdf;
using ReportChecker.SourceProviders.File;
using ReportChecker.SourceProviders.GitHub;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.Application.Services;

public class ProviderService(
    IEnumerable<ISourceProvider> sourceProviders,
    LatexFormatProvider latexFormatProvider,
    PdfFormatProvider pdfFormatProvider) : IProviderService
{
    public ISourceProvider GetSourceProvider(string providerName)
    {
        return sourceProviders.First(e => e.Key == providerName);
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