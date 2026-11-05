namespace UCB_Hold.Server.Modules.Inventario.Application;

public interface IHistorialPrestamosEquipo
{
    Task<List<HistorialEquipoDto>> GetHistorial(int equipoId);
}
