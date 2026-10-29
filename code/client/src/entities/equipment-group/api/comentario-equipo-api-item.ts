export interface ComentarioEquipoApiItem {
  Id: number;
  IdGrupoEquipo: number;
  IdComentarioPadre?: number | null;
  CarnetUsuario: string;
  NombreUsuario: string;
  Contenido: string;
  FechaCreacion: string;
  Likes?: number;
  LikedByCurrentUser?: boolean;
  PuedeEliminar?: boolean;
  Respuestas?: ComentarioEquipoApiItem[];
}
