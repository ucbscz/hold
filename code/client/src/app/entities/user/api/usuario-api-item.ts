export interface UsuarioApiItem {
  Carnet: string;
  Nombre: string | null;
  ApellidoMaterno: string | null;
  ApellidoPaterno: string | null;
  Rol: string | null;
  CarreraNombre: string | null;
  Email: string | null;
  Telefono: string | null;
  NombreReferencia: string | null;
  TelefonoReferencia: string | null;
  EmailReferencia: string | null;
  IdCarrera?: number | null;
  Bloqueado?: boolean;
  MotivoBloqueo?: string | null;
  ImagenFrenteCarnet?: string | null;
  ImagenAtrasCarnet?: string | null;
  ImagenFirma?: string | null;
}
