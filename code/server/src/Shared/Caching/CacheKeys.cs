namespace IMT_Reservas.Server.Application.Features.Cache;

public static class CacheKeys
{
    public const string GrupoEquipoVersion = "grupo-equipo:version";

    public static string GrupoEquipoSearch(string nombre, string? categoria, long version) =>
        $"grupo-equipo:search:v{version}:{nombre}:{categoria}";

    public static string Usuario(string carnet) => $"usuario:{carnet}";
}
