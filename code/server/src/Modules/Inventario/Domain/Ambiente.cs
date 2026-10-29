using IMT_Reservas.Server.Modules.Usuarios.Domain;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Inventario.Domain;

public class Ambiente : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? CarnetAdministrador { get; set; }
    public Usuario? Administrador { get; set; }
}
