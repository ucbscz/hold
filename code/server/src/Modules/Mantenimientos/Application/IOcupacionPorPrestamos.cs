namespace UCB_Hold.Server.Modules.Mantenimientos.Application;

public interface IOcupacionPorPrestamos
{
    Task<bool> HayPrestamoEnRango(IReadOnlyCollection<int> codigosImt, DateTime start, DateTime end);
}
