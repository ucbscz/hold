using FluentAssertions;
using IMT_Reservas.Server.Application.Security;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
public class AllowedImageTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0];
    private static readonly byte[] Webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();

    [Test]
    public void AcceptsJpegPngAndWebp()
    {
        AllowedImage.IsValid(Png, 1024).Should().BeTrue();
        AllowedImage.IsValid(Jpeg, 1024).Should().BeTrue();
        AllowedImage.IsValid(Webp, 1024).Should().BeTrue();
    }

    [Test]
    public void RejectsContentWithoutImageSignature()
    {
        AllowedImage.IsValid("<script>alert(1)</script>"u8.ToArray(), 1024).Should().BeFalse();
        AllowedImage.IsValid("%PDF-1.7"u8.ToArray(), 1024).Should().BeFalse();
    }

    [Test]
    public void RejectsImagesAboveLimit()
    {
        AllowedImage.IsValid(Png, Png.Length - 1).Should().BeFalse();
    }

    [Test]
    public void AcceptsMissingContent()
    {
        AllowedImage.IsValid(null, 1024).Should().BeTrue();
        AllowedImage.IsValid([], 1024).Should().BeTrue();
        AllowedImage.IsValidDataUri(null, 1024).Should().BeTrue();
    }

    [Test]
    public void ValidatesDataUriTypeAndPayload()
    {
        var payload = Convert.ToBase64String(Png);

        AllowedImage.IsValidDataUri($"data:image/png;base64,{payload}", 1024).Should().BeTrue();
        AllowedImage.IsValidDataUri($"data:image/svg+xml;base64,{payload}", 1024).Should().BeFalse();
        AllowedImage.IsValidDataUri("data:image/png;base64,PHN2Zz4=", 1024).Should().BeFalse();
        AllowedImage.IsValidDataUri(payload, 1024).Should().BeFalse();
        AllowedImage.IsValidDataUri($"data:image/png;base64,{payload}", 4).Should().BeFalse();
    }
}
