namespace IMT_Reservas.Server.Application.Features.Contrato;

public class ContratoDto
{
    public int? Id { get; set; }
    public int? PrestamoId { get; set; }
    public string? ContratoHtml { get; set; }
}

public sealed class FirmanteContratoDto
{
    public string Nombre { get; init; } = string.Empty;
    public string Carnet { get; init; } = string.Empty;
    public string FirmaBase64 { get; init; } = string.Empty;
}
