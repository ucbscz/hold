namespace IMT_Reservas.Server.Application.Features.Notificacion;

public class NotificacionDto
{
    public int? Id { get; set; }
    public string? CarnetUsuario { get; set; }
    public string? Tipo { get; set; }
    public string? Titulo { get; set; }
    public string? Contenido { get; set; }
    public string? Detalle { get; set; }
    public bool? Leido { get; set; }
    public DateTime? FechaEnvio { get; set; }
}
