import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '@environments/environment';
import { ApiResponse, extractApiValue } from '@shared/api';
import { map } from 'rxjs';
import { Usuario } from '../model/usuario';
import { UsuarioApiItem } from './usuario-api-item';
import { UsuarioListApiResponse } from './usuario-list-api-response';
import { UsuarioLoginApiResponse } from './usuario-login-api-response';
@Injectable({
  providedIn: 'root',
})
export class UsuarioServiceAPI {
  private readonly apiUrl = environment.apiUrl + '/api/usuarios';
  constructor(private readonly http: HttpClient) {}

  registrarCuenta(
    usuario: Usuario,
    contrasena: string,
    rol: string | null,
    aceptaTerminos = false,
  ) {
    if (!rol) {
      rol = 'Estudiante';
    }
    const envio = {
      Carnet: usuario.carnet,
      Nombre: usuario.nombre,
      ApellidoPaterno: usuario.apellido_paterno,
      ApellidoMaterno: usuario.apellido_materno,
      Rol: rol,
      Email: usuario.correo,
      Contrasena: contrasena,
      CarreraNombre: usuario.carrera,
      IdCarrera: 0,
      Telefono: usuario.telefono,
      TelefonoReferencia: usuario.telefono_referencia,
      NombreReferencia: usuario.nombre_referencia,
      EmailReferencia: usuario.email_referencia,
      AceptaTerminos: aceptaTerminos,
    };
    return this.http.post(this.apiUrl, envio);
  }

  iniciarSesion(correo: string, contrasena: string) {
    const api = `${environment.apiUrl}/api/auth/login`;
    const body = { Email: correo, Contrasena: contrasena };
    return this.http.post<UsuarioLoginApiResponse>(api, body).pipe(
      map((response) => ({
        accessToken: response.Value.AccessToken as string,
        refreshToken: response.Value.RefreshToken as string,
        usuario: this.mapearUsuario(response.Value.Usuario),
      })),
    );
  }

  actualizarUsuario(usuario: Usuario) {
    const envio = {
      Carnet: usuario.carnet,
      Nombre: usuario.nombre,
      ApellidoPaterno: usuario.apellido_paterno,
      ApellidoMaterno: usuario.apellido_materno,
      Email: usuario.correo,
      CarreraNombre: usuario.carrera,
      IdCarrera: usuario.carrera_Id || 0,
      Telefono: usuario.telefono,
      TelefonoReferencia: usuario.telefono_referencia,
      NombreReferencia: usuario.nombre_referencia,
      EmailReferencia: usuario.email_referencia,
    };
    return this.http.put<Usuario>(`${this.apiUrl}/${envio.Carnet}`, envio);
  }

  obtenerPerfil() {
    return this.http
      .get<ApiResponse<UsuarioApiItem>>(`${this.apiUrl}/perfil`)
      .pipe(
        map((response) =>
          this.mapearUsuario(extractApiValue(response, {} as UsuarioApiItem)),
        ),
      );
  }

  actualizarPerfil(usuario: Usuario, contrasena: string) {
    const envio = {
      Telefono: usuario.telefono,
      Contrasena: contrasena || null,
      TelefonoReferencia: usuario.telefono_referencia,
      NombreReferencia: usuario.nombre_referencia,
      EmailReferencia: usuario.email_referencia,
      ImagenFrenteCarnet: usuario.imagen_frente_carnet,
      ImagenAtrasCarnet: usuario.imagen_atras_carnet,
      ImagenFirma: usuario.imagen_firma,
    };
    return this.http
      .put<ApiResponse<UsuarioApiItem>>(`${this.apiUrl}/perfil`, envio)
      .pipe(
        map((response) =>
          this.mapearUsuario(extractApiValue(response, {} as UsuarioApiItem)),
        ),
      );
  }

  obtenerUsuarios() {
    return this.http
      .get<UsuarioListApiResponse>(this.apiUrl)
      .pipe(
        map((response) =>
          response.Value.map((item) => this.mapearUsuario(item)),
        ),
      );
  }

  editarUsuario(usuario: Usuario, entrada: string) {
    let contrasena;
    if (entrada === '') {
      contrasena = null;
    } else {
      contrasena = entrada;
    }
    const envio = {
      Carnet: usuario.carnet,
      Nombre: usuario.nombre,
      ApellidoPaterno: usuario.apellido_paterno,
      ApellidoMaterno: usuario.apellido_materno,
      Email: usuario.correo,
      Contrasena: contrasena,
      Rol: usuario.rol,
      CarreraNombre: usuario.carrera,
      IdCarrera: usuario.carrera_Id || 0,
      Telefono: usuario.telefono,
      TelefonoReferencia: usuario.telefono_referencia,
      NombreReferencia: usuario.nombre_referencia,
      EmailReferencia: usuario.email_referencia,
    };
    return this.http.put<Usuario>(`${this.apiUrl}/${envio.Carnet}`, envio);
  }

  eliminarUsuario(id: string) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  private mapearUsuario(item: UsuarioApiItem): Usuario {
    return {
      id: item.Carnet,
      carnet: item.Carnet,
      nombre: item.Nombre,
      apellido_materno: item.ApellidoMaterno,
      apellido_paterno: item.ApellidoPaterno,
      rol: item.Rol,
      correo: item.Email,
      telefono: item.Telefono,
      telefono_referencia: item.TelefonoReferencia,
      nombre_referencia: item.NombreReferencia,
      email_referencia: item.EmailReferencia,
      carrera_Id: item.IdCarrera ?? null,
      carrera: item.CarreraNombre,
      bloqueado: item.Bloqueado ?? false,
      motivo_bloqueo: item.MotivoBloqueo ?? null,
      imagen_frente_carnet: item.ImagenFrenteCarnet ?? null,
      imagen_atras_carnet: item.ImagenAtrasCarnet ?? null,
      imagen_firma: item.ImagenFirma ?? null,
    };
  }

  bloquearUsuario(carnet: string, bloqueado: boolean, motivo: string | null) {
    return this.http.patch(`${this.apiUrl}/${carnet}/bloqueo`, {
      Bloqueado: bloqueado,
      MotivoBloqueo: motivo,
    });
  }
}
