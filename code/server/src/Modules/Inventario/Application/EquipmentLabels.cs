using System.Globalization;
using IMT_Reservas.Server.Application.Abstraction;
using Microsoft.Extensions.Options;

namespace IMT_Reservas.Server.Application.Features.Equipo;

public sealed class EquipmentLabels
{
    private const string Separator = " ";

    public EquipmentLabels(IOptions<BrandingSettings> branding)
    {
        InternalCodePrefix = branding.Value.InternalCodeLabel + Separator;
        DuplicateInternalCode = $"Código {branding.Value.InternalCodeLabel} ya registrado";
        DuplicateOrganizationCode = $"Código {branding.Value.OrganizationCodeLabel} ya registrado";
    }

    public string InternalCodePrefix { get; }
    public string DuplicateInternalCode { get; }
    public string DuplicateOrganizationCode { get; }

    public string InternalCode(int code) =>
        InternalCodePrefix + code.ToString(CultureInfo.InvariantCulture);
}
