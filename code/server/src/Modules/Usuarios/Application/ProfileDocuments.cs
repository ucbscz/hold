namespace UCB_Hold.Server.Modules.Usuarios.Application;

public static class ProfileDocuments
{
    public static void Clear(UsuarioDto dto)
    {
        dto.ImagenPerfil = null;
        dto.ImagenFrenteCarnet = null;
        dto.ImagenAtrasCarnet = null;
        dto.ImagenFirma = null;
    }
}
