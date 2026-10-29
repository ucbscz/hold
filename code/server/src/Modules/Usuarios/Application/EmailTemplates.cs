namespace IMT_Reservas.Server.Modules.Usuarios.Application;

public static class EmailTemplates
{
    public const string VerificationSubject = "Verifica tu cuenta de {0}";
    public const string VerificationBody =
        "Confirma tu correo institucional desde este enlace, válido durante {0} horas:\n\n{1}";
    public const string PasswordResetSubject = "Restablece tu contraseña de {0}";
    public const string PasswordResetBody =
        "Tu código para restablecer la contraseña es:\n\n{0}\n\nEl código es válido durante {1} minutos y solo puede utilizarse una vez. Si no realizaste esta solicitud, puedes ignorar este correo.";
}
