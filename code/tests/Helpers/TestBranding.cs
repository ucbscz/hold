using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Equipo;
using IMT_Reservas.Server.Application.Security;
using Microsoft.Extensions.Options;

namespace IMT_Reservas.Tests.Helpers;

internal static class TestBranding
{
    public const string InternalCodeLabel = "IMT";
    public const string OrganizationCodeLabel = "UCB";

    public static readonly BrandingSettings Settings = new()
    {
        ProductName = "Plataforma",
        InternalCodeLabel = InternalCodeLabel,
        OrganizationCodeLabel = OrganizationCodeLabel,
    };

    public static readonly DataProtectionSettings DataProtection = new()
    {
        ApplicationName = "Plataforma",
        PayloadMarker = "PLT1",
        TextMarker = "plataforma:v1:",
    };

    public static EquipmentLabels Labels => new(Options.Create(Settings));

    public static IOptions<DataProtectionSettings> DataProtectionOptions =>
        Options.Create(DataProtection);
}
