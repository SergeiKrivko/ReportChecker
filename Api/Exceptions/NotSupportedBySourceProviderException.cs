using System.Net;
using ReportChecker.Abstractions;

namespace ReportChecker.Exceptions;

public class NotSupportedBySourceProviderException : ReportCheckerBaseException
{
    public ISourceProvider SourceProvider { get; }

    public NotSupportedBySourceProviderException(ISourceProvider sourceProvider) : base(
        $"Operation is not supported by source provider '{sourceProvider.Key}'",
        HttpStatusCode.NotFound)
    {
        SourceProvider = sourceProvider;
    }

    public NotSupportedBySourceProviderException(ISourceProvider sourceProvider, Exception innerException) : base(
        $"Operation is not supported by source provider '{sourceProvider.Key}'",
        HttpStatusCode.NotFound,
        innerException)
    {
        SourceProvider = sourceProvider;
    }

    public NotSupportedBySourceProviderException(ISourceProvider sourceProvider, string message) : base(message,
        HttpStatusCode.NotFound)
    {
        SourceProvider = sourceProvider;
    }

    public NotSupportedBySourceProviderException(ISourceProvider sourceProvider, string message,
        Exception innerException) : base(message,
        HttpStatusCode.NotFound,
        innerException)
    {
        SourceProvider = sourceProvider;
    }
}