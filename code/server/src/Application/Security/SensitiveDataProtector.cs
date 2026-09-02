using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace IMT_Reservas.Server.Application.Security;

public sealed class SensitiveDataProtector
{
    private static readonly byte[] Prefix = "UCBH1"u8.ToArray();
    private const string TextPrefix = "ucbhold:v1:";
    private readonly IDataProtector _protector;
    private readonly ILogger<SensitiveDataProtector> _logger;

    public SensitiveDataProtector(
        IDataProtectionProvider provider,
        ILogger<SensitiveDataProtector> logger
    )
    {
        _protector = provider.CreateProtector("UCBHold.SensitiveData.v1");
        _logger = logger;
    }

    public byte[]? Protect(byte[]? value)
    {
        if (value == null || value.Length == 0 || HasPrefix(value))
            return value;

        var protectedValue = _protector.Protect(value);
        var result = new byte[Prefix.Length + protectedValue.Length];
        Prefix.CopyTo(result, 0);
        protectedValue.CopyTo(result, Prefix.Length);
        return result;
    }

    public byte[]? Unprotect(byte[]? value)
    {
        if (value == null || value.Length == 0 || !HasPrefix(value))
            return value;

        try
        {
            return _protector.Unprotect(value.AsSpan(Prefix.Length).ToArray());
        }
        catch (CryptographicException exception)
        {
            _logger.LogError(exception, "No se pudo descifrar un dato sensible");
            return null;
        }
    }

    public string Protect(string value)
    {
        if (string.IsNullOrEmpty(value) || value.StartsWith(TextPrefix, StringComparison.Ordinal))
            return value;

        var protectedValue = _protector.Protect(Encoding.UTF8.GetBytes(value));
        return TextPrefix + Convert.ToBase64String(protectedValue);
    }

    public string Unprotect(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(TextPrefix, StringComparison.Ordinal))
            return value;

        try
        {
            var payload = Convert.FromBase64String(value[TextPrefix.Length..]);
            return Encoding.UTF8.GetString(_protector.Unprotect(payload));
        }
        catch (Exception exception) when (
            exception is CryptographicException or FormatException
        )
        {
            _logger.LogError(exception, "No se pudo descifrar un contrato");
            return string.Empty;
        }
    }

    private static bool HasPrefix(byte[] value) =>
        value.Length > Prefix.Length && value.AsSpan(0, Prefix.Length).SequenceEqual(Prefix);
}
