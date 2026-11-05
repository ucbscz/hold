namespace UCB_Hold.Server.Modules.Usuarios.Application;

public interface IUsuarioEliminacion
{
    Task AlEliminarUsuario(string carnet);
}
