using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using IMT_Reservas.Server.Application.Abstraction;
using Microsoft.Extensions.Options;

namespace IMT_Reservas.Server.Application.Security;

public sealed class SensitiveDataProtector
{
    private readonly byte[] _payloadMarker;
    private readonly string _textMarker;
    private readonly IDataProtector _protector;
    private readonly ILogger<SensitiveDataProtector> _logger;

    public SensitiveDataProtector(
        IDataProtectionProvider provider,
        IOptions<DataProtectionSettings> settings,
        ILogger<SensitiveDataProtector> logger
    )
    {
        _protector = provider.CreateProtector(settings.Value.Purpose);
        _payloadMarker = Encoding.UTF8.GetBytes(settings.Value.PayloadMarker);
        _textMarker = settings.Value.TextMarker;
        _logger = logger;
    }

    public byte[]? Protect(byte[]? value)
    {
        if (value == null || value.Length == 0 || HasPrefix(value))
            return value;

        var protectedValue = _protector.Protect(value);
        var result = new byte[_payloadMarker.Length + protectedValue.Length];
        _payloadMarker.CopyTo(result, 0);
        protectedValue.CopyTo(result, _payloadMarker.Length);
        return result;
    }

    public byte[]? Unprotect(byte[]? value)
    {
        if (value == null || value.Length == 0 || !HasPrefix(value))
            return value;

        try
        {
            return _protector.Unprotect(value.AsSpan(_payloadMarker.Length).ToArray());
        }
        catch (CryptographicException exception)
        {
            _logger.SensitiveDataDecryptionFailed(exception);
            return null;
        }
    }

    public string Protect(string value)
    {
        if (string.IsNullOrEmpty(value) || value.StartsWith(_textMarker, StringComparison.Ordinal))
            return value;

        var protectedValue = _protector.Protect(Encoding.UTF8.GetBytes(value));
        return _textMarker + Convert.ToBase64String(protectedValue);
    }

    public string Unprotect(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(_textMarker, StringComparison.Ordinal))
            return value;

        try
        {
            var payload = Convert.FromBase64String(value[_textMarker.Length..]);
            return Encoding.UTF8.GetString(_protector.Unprotect(payload));
        }
        catch (Exception exception) when (
            exception is CryptographicException or FormatException
        )
        {
            _logger.ContractDecryptionFailed(exception);
            return string.Empty;
        }
    }

    private bool HasPrefix(byte[] value) =>
        value.Length > _payloadMarker.Length
        && value.AsSpan(0, _payloadMarker.Length).SequenceEqual(_payloadMarker);
}
