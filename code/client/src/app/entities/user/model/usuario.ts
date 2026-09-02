export class Usuario {
  id?: string;
  carnet?: string | null;
  nombre?: string | null;
  apellido_materno?: string | null;
  apellido_paterno?: string | null;
  rol?: string | null;
  carrera_Id?: number | null;
  carrera?: string | null;
  correo?: string | null;
  telefono?: string | null;
  nombre_referencia?: string | null;
  telefono_referencia?: string | null;
  email_referencia?: string | null;
  bloqueado?: boolean;
  motivo_bloqueo?: string | null;
  imagen_frente_carnet?: string | null;
  imagen_atras_carnet?: string | null;
  constructor() {
    this.id = '';
    this.carnet = null;
    this.nombre = null;
    this.apellido_materno = null;
    this.apellido_paterno = null;
    this.rol = null;
    this.carrera_Id = null;
    this.carrera = null;
    this.correo = null;
    this.telefono = null;
    this.nombre_referencia = null;
    this.telefono_referencia = null;
    this.email_referencia = null;
    this.imagen_frente_carnet = null;
    this.imagen_atras_carnet = null;
  }
}
