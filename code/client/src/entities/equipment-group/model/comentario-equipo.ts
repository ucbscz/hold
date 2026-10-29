export type ComentarioEquipoOrden = 'recientes' | 'antiguos' | 'likes';

export interface ComentarioEquipo {
  id: number;
  idGrupoEquipo: number;
  idComentarioPadre: number | null;
  carnetUsuario: string;
  nombreUsuario: string;
  contenido: string;
  fechaCreacion: string;
  likes: number;
  likedByCurrentUser: boolean;
  puedeEliminar: boolean;
  respuestas: ComentarioEquipo[];
}
