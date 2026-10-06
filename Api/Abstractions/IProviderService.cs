namespace ReportChecker.Abstractions;

public interface IProviderService
{
    public ISourceProvider GetSourceProvider(string providerName);
    public IFormatProvider GetFormatProvider(string providerName);

    /// <summary>Возвращает провайдер формата или <c>null</c>, если формат неизвестен.</summary>
    public IFormatProvider? GetFormatProviderOrDefault(string providerName);
}
