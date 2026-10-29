using System.ComponentModel.DataAnnotations.Schema;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Domain;

public class ComentarioEquipo : Entity
{
    public int IdGrupoEquipo { get; set; }
    public int? IdComentarioPadre { get; set; }
    public string CarnetUsuario { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int Likes { get; set; }
    public string LikedBy { get; set; } = string.Empty;

    [ForeignKey("IdGrupoEquipo")]
    public GrupoEquipo? GrupoEquipo { get; set; }

    [ForeignKey("IdComentarioPadre")]
    public ComentarioEquipo? ComentarioPadre { get; set; }

    [ForeignKey("CarnetUsuario")]
    public Usuario? Usuario { get; set; }
}
