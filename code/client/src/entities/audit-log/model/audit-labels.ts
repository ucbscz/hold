export const ACCIONES_POR_ENTIDAD: Record<string, string[]> = {
  Prestamo: [
    'Crear',
    'Aprobar',
    'Rechazar',
    'Recoger',
    'Devolver',
    'Cancelar',
    'AtrasadoAutomatico',
    'RegistrarContrato',
    'EliminarContrato',
    'Eliminar',
  ],
  Usuario: ['Crear', 'Editar', 'Bloquear', 'Desbloquear', 'Eliminar'],
  Equipo: ['Crear', 'Editar', 'Eliminar'],
  GrupoEquipo: ['Crear', 'Editar', 'EliminarComentario', 'Eliminar'],
  Accesorio: ['Crear', 'Editar', 'Eliminar'],
  Componente: ['Crear', 'Editar', 'Eliminar'],
  Gavetero: ['Crear', 'Editar', 'Eliminar'],
  Mueble: ['Crear', 'Editar', 'Eliminar'],
  Mantenimiento: ['Crear', 'Editar', 'Eliminar'],
  EmpresaMantenimiento: ['Crear', 'Editar', 'Eliminar'],
  Carrera: ['Crear', 'Editar', 'Eliminar'],
  Categoria: ['Crear', 'Editar', 'Eliminar'],
};

const BADGE_POR_ACCION: Record<string, string> = {
  crear: 'badge-aprobado',
  editar: 'badge-pendiente',
  aprobar: 'badge-activo',
  recoger: 'badge-activo',
  devolver: 'badge-finalizado',
  eliminar: 'badge-rechazado',
  rechazar: 'badge-rechazado',
  cancelar: 'badge-rechazado',
  atrasadoautomatico: 'badge-atrasado',
  registrarcontrato: 'badge-aprobado',
  eliminarcontrato: 'badge-rechazado',
};

const ETIQUETAS_ENTIDAD: Record<string, string> = {
  Prestamo: 'Préstamo',
  Usuario: 'Usuario',
  Equipo: 'Equipo',
  GrupoEquipo: 'Grupo de equipos',
  Accesorio: 'Accesorio',
  Componente: 'Componente',
  Gavetero: 'Gavetero',
  Mueble: 'Mueble',
  Mantenimiento: 'Mantenimiento',
  EmpresaMantenimiento: 'Empresa de mantenimiento',
  Carrera: 'Carrera',
  Categoria: 'Categoría',
  Ambiente: 'Ambiente',
  Procedencia: 'Procedencia',
  ConfiguracionSistema: 'Configuración del sistema',
};

export function badgeAccion(accion: string | undefined): string {
  return BADGE_POR_ACCION[accion?.toLowerCase() ?? ''] ?? 'badge-cancelado';
}

export function etiquetaEntidad(
  entidad: string | undefined,
  porDefecto: string,
): string {
  return ETIQUETAS_ENTIDAD[entidad ?? ''] ?? entidad ?? porDefecto;
}

export function descripcionAccion(
  accion: string | undefined,
  entidad: string,
  registro: string,
): string {
  const sujeto = `${entidad} “${registro}”`;
  const descripciones: Record<string, string> = {
    crear: `Se creó el registro de ${sujeto}.`,
    editar: `Se actualizaron los datos de ${sujeto}.`,
    eliminar: `Se eliminó el registro de ${sujeto}.`,
    aprobar: `Se aprobó el ${sujeto}.`,
    rechazar: `Se rechazó el ${sujeto}.`,
    recoger: `Se registró la entrega del ${sujeto}.`,
    devolver: `Se registró la devolución del ${sujeto}.`,
    cancelar: `Se canceló el ${sujeto}.`,
    bloquear: `Se bloqueó el registro de ${sujeto}.`,
    desbloquear: `Se desbloqueó el registro de ${sujeto}.`,
    atrasadoautomatico: `El sistema marcó el ${sujeto} como atrasado.`,
    registrarcontrato: `Se registró el contrato del ${sujeto}.`,
    eliminarcontrato: `Se eliminó el contrato del ${sujeto}.`,
    eliminarcomentario: `Se eliminó un comentario de ${sujeto}.`,
  };

  return (
    descripciones[accion?.toLowerCase() ?? ''] ??
    `Se registró una acción en ${sujeto}.`
  );
}
