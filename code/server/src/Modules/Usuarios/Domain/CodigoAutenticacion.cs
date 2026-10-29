namespace IMT_Reservas.Server.Core.Entities;

public sealed class CodigoAutenticacion
{
    public string Hash { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string GoogleId { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public bool Usado { get; set; }
}
