namespace IMT_Reservas.Server.Modules.Inventario.Domain;

public static class EstadoEquipoNombre
{
    public const string Operativo = "operativo";
    public const string ParcialmenteOperativo = "parcialmente_operativo";
    public const string Inoperativo = "inoperativo";
    public const string EnMantenimiento = "en_mantenimiento";

    public static readonly IReadOnlyList<string> Validos =
    [
        Operativo,
        ParcialmenteOperativo,
        Inoperativo,
        EnMantenimiento,
    ];

    public static string ToName(EstadoEquipo estado) =>
        estado switch
        {
            EstadoEquipo.ParcialmenteOperativo => ParcialmenteOperativo,
            EstadoEquipo.Inoperativo => Inoperativo,
            EstadoEquipo.EnMantenimiento => EnMantenimiento,
            _ => Operativo,
        };

    public static EstadoEquipo Parse(string? nombre) =>
        nombre switch
        {
            ParcialmenteOperativo => EstadoEquipo.ParcialmenteOperativo,
            Inoperativo => EstadoEquipo.Inoperativo,
            EnMantenimiento => EstadoEquipo.EnMantenimiento,
            _ => EstadoEquipo.Operativo,
        };
}
