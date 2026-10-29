using Microsoft.Extensions.Options;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Modules.Prestamos.Application;
using IMT_Reservas.Server.Shared.Configuration;
using IMT_Reservas.Server.Shared.Security;

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

    public static readonly RegionSettings Region = new()
    {
        Name = "Bolivia",
        TimeZoneId = "America/La_Paz",
        UtcOffsetHours = -4,
    };

    public static HorarioReserva Horario => new(new RegionClock(Options.Create(Region)));

    public static EquipmentLabels Labels => new(Options.Create(Settings));

    public static IOptions<DataProtectionSettings> DataProtectionOptions =>
        Options.Create(DataProtection);
}
