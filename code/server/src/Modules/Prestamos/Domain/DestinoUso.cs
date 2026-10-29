namespace IMT_Reservas.Server.Core.Entities;

public static class DestinoUso
{
    public const string Universidad = "Universidad";
    public const string Clase = "Clase";
    public const string Casa = "Casa";

    public static bool EsValido(string? destino) => destino is Universidad or Clase or Casa;
}
