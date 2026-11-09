using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

public class CarreraDto : IIdentifiedDto
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
    public string? Codigo { get; set; }
}
