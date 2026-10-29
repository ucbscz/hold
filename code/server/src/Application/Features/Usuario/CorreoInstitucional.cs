namespace IMT_Reservas.Server.Application.Features.Usuario;

public static class CorreoInstitucional
{
    public const string Dominio = "@ucb.edu.bo";

    public static bool Es(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.EndsWith(Dominio, StringComparison.OrdinalIgnoreCase);
}
