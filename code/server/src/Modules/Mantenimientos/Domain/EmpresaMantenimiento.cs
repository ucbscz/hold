using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Mantenimientos.Domain;

public class EmpresaMantenimiento : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Nit { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? NombreResponsable { get; set; }
    public string? ApellidoResponsable { get; set; }
}
