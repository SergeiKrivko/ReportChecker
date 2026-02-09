namespace ReportChecker.Abstractions;

public interface IProviderService
{
    public IAuthProvider GetAuthProvider(string providerName);
    public ISourceProvider GetSourceProvider(string providerName);
    public IFormatProvider GetFormatProvider(string providerName);
}