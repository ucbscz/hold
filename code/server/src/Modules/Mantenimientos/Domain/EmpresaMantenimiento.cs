using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Mantenimientos.Domain;

public class EmpresaMantenimiento : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Nit { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? NombreResponsable { get; set; }
    public string? ApellidoResponsable { get; set; }
}
