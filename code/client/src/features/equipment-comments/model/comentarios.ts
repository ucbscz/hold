import {
  ComentarioEquipo,
  ComentarioEquipoOrden,
} from '@entities/equipment-group';

export function esOrdenComentarios(
  orden: unknown,
): orden is ComentarioEquipoOrden {
  return orden === 'recientes' || orden === 'antiguos' || orden === 'likes';
}

export function totalComentarios(comentarios: ComentarioEquipo[]): number {
  return comentarios.reduce(
    (total, comentario) => total + 1 + comentario.respuestas.length,
    0,
  );
}

export function actualizarComentario(
  comentarios: ComentarioEquipo[],
  actualizado: ComentarioEquipo,
): ComentarioEquipo[] {
  return comentarios.map((comentario) =>
    comentario.id === actualizado.id
      ? {
          ...comentario,
          likes: actualizado.likes,
          likedByCurrentUser: actualizado.likedByCurrentUser,
          puedeEliminar: actualizado.puedeEliminar,
        }
      : {
          ...comentario,
          respuestas: actualizarComentario(comentario.respuestas, actualizado),
        },
  );
}

export function removerComentario(
  comentarios: ComentarioEquipo[],
  comentarioId: number,
): ComentarioEquipo[] {
  return comentarios
    .filter((comentario) => comentario.id !== comentarioId)
    .map((comentario) => ({
      ...comentario,
      respuestas: removerComentario(comentario.respuestas, comentarioId),
    }));
}
