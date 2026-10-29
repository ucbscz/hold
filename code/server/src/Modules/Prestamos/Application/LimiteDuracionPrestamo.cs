namespace IMT_Reservas.Server.Modules.Prestamos.Application;

public static class LimiteDuracionPrestamo
{
    public static string? Validar(
        IEnumerable<(string Nombre, int MaximoDias)> limites,
        TimeSpan duracion
    )
    {
        var excedido = limites
            .Where(limite => duracion > TimeSpan.FromDays(limite.MaximoDias))
            .OrderBy(limite => limite.MaximoDias)
            .FirstOrDefault();

        return excedido == default
            ? null
            : $"El grupo '{excedido.Nombre}' permite préstamos de hasta {excedido.MaximoDias} día(s)";
    }
}
