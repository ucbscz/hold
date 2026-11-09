using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Mantenimientos.Application;

public class EmpresaMantenimientoDto : IIdentifiedDto
{
    public int? Id { get; set; }
    public string? NombreEmpresa { get; set; }
    public string? Nit { get; set; }
    public string? NombreResponsable { get; set; }
    public string? ApellidoResponsable { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
}
