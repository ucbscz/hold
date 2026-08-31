using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Ambiente : Entity
{
    public string Nombre { get; set; } = string.Empty;
    public string? CarnetAdministrador { get; set; }
    public Usuario? Administrador { get; set; }
}
