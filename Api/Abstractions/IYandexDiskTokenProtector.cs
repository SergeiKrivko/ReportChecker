namespace ReportChecker.Abstractions;

/// <summary>Шифрование OAuth-токенов перед сохранением в БД.</summary>
public interface IYandexDiskTokenProtector
{
    public string Protect(string value);
    public string Unprotect(string value);
}
