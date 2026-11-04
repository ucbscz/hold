using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

public class CategoriaDto : IIdentifiedDto
{
    public int? Id { get; set; }
    public string? Nombre { get; set; }
}
