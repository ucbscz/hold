using IMT_Reservas.Server.Core.Abstraction;

namespace IMT_Reservas.Server.Core.Entities;

public class Ambiente : Entity
{
    public string Nombre { get; set; } = string.Empty;
}
