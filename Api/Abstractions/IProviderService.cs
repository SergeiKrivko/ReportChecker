namespace ReportChecker.Abstractions;

public interface IProviderService
{
    public ISourceProvider GetSourceProvider(string providerName);
    public IFormatProvider GetFormatProvider(string providerName);
}