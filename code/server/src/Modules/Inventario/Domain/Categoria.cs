using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Categoria : Entity
{
    public string Nombre { get; set; } = string.Empty;
}
