using System.Text.RegularExpressions;

namespace UCB_Hold.Server.Shared.Security;

public static partial class AllowedImage
{
    public const int MaxSignatureBytes = 1024 * 1024;

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();

    public static bool IsValid(byte[]? content, int maxBytes) =>
        content == null
        || content.Length == 0
        || (content.Length <= maxBytes && HasImageSignature(content));

    public static bool IsValidDataUri(string? dataUri, int maxBytes)
    {
        if (string.IsNullOrEmpty(dataUri))
            return true;

        var match = DataUri().Match(dataUri);
        if (!match.Success)
            return false;

        var payload = match.Groups["payload"].Value;
        if (payload.Length > (maxBytes + 2) / 3 * 4)
            return false;

        var buffer = new byte[payload.Length * 3 / 4];
        return Convert.TryFromBase64String(payload, buffer, out var written)
            && IsValid(buffer[..written], maxBytes);
    }

    private static bool HasImageSignature(byte[] content)
    {
        var span = content.AsSpan();
        return span.StartsWith(Jpeg)
            || span.StartsWith(Png)
            || (span.Length > 12 && span.StartsWith(Riff) && span[8..12].SequenceEqual(Webp));
    }

    [GeneratedRegex("^data:image/(?:png|jpeg|webp);base64,(?<payload>[A-Za-z0-9+/]+={0,2})$")]
    private static partial Regex DataUri();
}
