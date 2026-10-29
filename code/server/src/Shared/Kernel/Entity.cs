namespace IMT_Reservas.Server.Shared.Kernel;

public abstract class Entity
{
    public int Id { get; set; }
    public bool EstadoEliminado { get; set; }
}
