using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using ReportChecker.Abstractions;

namespace ReportChecker.SourceProviders.YandexDisk;

/// <summary>
/// AES-GCM для OAuth-токенов. Ключ берётся из <c>YandexDisk:TokenEncryptionKey</c>:
/// base64 из 32 байт либо произвольная строка (из неё выводится SHA-256).
/// </summary>
public class YandexDiskTokenProtector : IYandexDiskTokenProtector
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[]? _key;

    public YandexDiskTokenProtector(IConfiguration configuration)
    {
        var value = configuration["YandexDisk:TokenEncryptionKey"];
        if (string.IsNullOrWhiteSpace(value))
            return;

        var buffer = new byte[32];
        if (!Convert.TryFromBase64String(value, buffer, out var written) || written != buffer.Length)
            buffer = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        _key = buffer;
    }

    public string Protect(string value)
    {
        var key = _key ?? throw NotConfigured();
        var plain = Encoding.UTF8.GetBytes(value);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        var result = new byte[NonceSize + TagSize + cipher.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSize);
        cipher.CopyTo(result, NonceSize + TagSize);
        return Prefix + Convert.ToBase64String(result);
    }

    public string Unprotect(string value)
    {
        var key = _key ?? throw NotConfigured();
        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
            throw new CryptographicException("Неизвестный формат защищённого токена");

        var data = Convert.FromBase64String(value[Prefix.Length..]);
        if (data.Length < NonceSize + TagSize)
            throw new CryptographicException("Повреждённый защищённый токен");

        var nonce = data.AsSpan(0, NonceSize);
        var tag = data.AsSpan(NonceSize, TagSize);
        var cipher = data.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    private static InvalidOperationException NotConfigured()
    {
        return new InvalidOperationException(
            "YandexDisk:TokenEncryptionKey не настроен: без ключа нельзя безопасно хранить OAuth-токены Диска");
    }
}
