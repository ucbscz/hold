namespace IMT_Reservas.Server.Application.Features.GrupoEquipo;

public class ComentarioEquipoDto
{
    public int Id { get; set; }
    public int IdGrupoEquipo { get; set; }
    public int? IdComentarioPadre { get; set; }
    public string CarnetUsuario { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public int Likes { get; set; }
    public bool LikedByCurrentUser { get; set; }
    public bool PuedeEliminar { get; set; }
    public List<ComentarioEquipoDto> Respuestas { get; set; } = [];
}
