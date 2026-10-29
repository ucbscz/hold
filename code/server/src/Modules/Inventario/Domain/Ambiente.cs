using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Domain;

public class Ambiente : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? CarnetAdministrador { get; set; }
    public Usuario? Administrador { get; set; }
}
