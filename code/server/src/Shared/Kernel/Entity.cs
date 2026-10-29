namespace IMT_Reservas.Server.Core.Abstraction;

public abstract class Entity
{
    public int Id { get; set; }
    public bool EstadoEliminado { get; set; }
}
