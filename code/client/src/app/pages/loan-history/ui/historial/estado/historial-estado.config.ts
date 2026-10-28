export interface HistorialVacio {
  icono: string;
  titulo: string;
  mensaje: string;
}

export const HISTORIAL_VACIO: Record<string, HistorialVacio> = {
  pendiente: {
    icono: 'fas fa-inbox',
    titulo: 'Sin préstamos pendientes',
    mensaje: 'No tienes solicitudes en espera de aprobación',
  },
  aprobado: {
    icono: 'fas fa-check-circle',
    titulo: 'Sin préstamos aprobados',
    mensaje: 'No tienes préstamos pendientes de recogida',
  },
  activo: {
    icono: 'fas fa-check-circle',
    titulo: 'Sin préstamos activos',
    mensaje: 'No tienes equipos prestados actualmente',
  },
  rechazado: {
    icono: 'fas fa-circle-xmark',
    titulo: 'Sin préstamos rechazados',
    mensaje: 'No tienes préstamos rechazados',
  },
  finalizado: {
    icono: 'fas fa-circle-check hist-empty__success-icon',
    titulo: 'Sin préstamos finalizados',
    mensaje: 'Aquí aparecerán tus préstamos completados',
  },
  cancelado: {
    icono: 'fas fa-ban',
    titulo: 'Sin cancelaciones',
    mensaje: 'No tienes préstamos cancelados',
  },
  atrasado: {
    icono: 'fas fa-check-circle hist-empty__success-icon',
    titulo: 'Sin retrasos',
    mensaje: 'Todos tus préstamos están al día',
  },
};

export const ESTADOS_CANCELABLES = ['pendiente', 'aprobado'];
