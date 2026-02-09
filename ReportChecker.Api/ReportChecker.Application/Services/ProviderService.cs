using ReportChecker.Abstractions;
using ReportChecker.AuthProviders.Yandex;
using ReportChecker.FormatProviders.Latex;
using ReportChecker.FormatProviders.Pdf;
using ReportChecker.SourceProviders.File;
using IFormatProvider = ReportChecker.Abstractions.IFormatProvider;

namespace ReportChecker.Application.Services;

public class ProviderService(
    YandexAuthProvider yandexAuthProvider,
    FileSourceProvider fileSourceProvider,
    LatexFormatProvider latexFormatProvider,
    PdfFormatProvider pdfFormatProvider) : IProviderService
{
    private readonly Dictionary<string, IAuthProvider> _authProviders = new()
    {
        { yandexAuthProvider.Key, yandexAuthProvider },
    };

    public IAuthProvider GetAuthProvider(string providerName)
    {
        return _authProviders[providerName];
    }

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
        { pdfFormatProvider.Key, pdfFormatProvider },
    };

    public IFormatProvider GetFormatProvider(string providerName)
    {
        return _formatProviders[providerName];
    }
}