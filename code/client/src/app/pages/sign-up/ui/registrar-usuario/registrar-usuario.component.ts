import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Carrera } from '@entities/admin';
import { CarreraService } from '@entities/career';
import { Usuario, UsuarioServiceAPI } from '@entities/user';
import { extractErrorMessage } from '@shared/lib/error';
import {
  identityDataUrlToBase64,
  processIdentityImage,
} from '@shared/lib/image/identity-image';
import {
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
} from '@shared/ui';
import { FirmaComponent } from '@features/signature';
@Component({
  selector: 'app-registrar-usuario',
  imports: [
    FormsModule,
    CommonModule,
    MostrarerrorComponent,
    AvisoExitoComponent,
    CustomSelectComponent,
    FirmaComponent,
    RouterLink,
  ],
  templateUrl: './registrar-usuario.component.html',
  styleUrl: './registrar-usuario.component.css',
})
export class RegistrarUsuarioComponent {
  nuevoUsuario: Usuario = new Usuario();
  password: string = '';
  confirmPassword: string = '';
  mostrarPassword = false;
  mostrarConfirmPassword = false;
  carreras: string[] = [];
  submitted: boolean = false;
  registrando: boolean = false;
  aceptaTerminos = false;
  registroGoogle = false;
  codigoGoogle: string | null = null;
  procesandoImagen = false;
  fotoPerfilPreview = '';
  carnetFrentePreview = '';
  carnetAtrasPreview = '';
  firmaPreview = '';
  capturandoFirma = signal(false);
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = '';
  aviso: WritableSignal<boolean> = signal(false);
  mensajeaviso: string =
    'Aviso desconocido , si ve esto es un error , avise al soporte si puede o intente mas tarde';
  constructor(
    private router: Router,
    private registrarcuenta: UsuarioServiceAPI,
    private carrerasS: CarreraService,
    private readonly route: ActivatedRoute,
  ) {}
  ngOnInit() {
    this.carrerasS.obtenerCarreras().subscribe({
      next: (response: Carrera[]) => {
        this.carreras = response.map((carrera) => carrera.Nombre ?? '');
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al obtener las carreras intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
    const googleCode = this.route.snapshot.queryParamMap.get('google');
    if (googleCode) this.cargarDatosGoogle(googleCode);
    if (
      this.route.snapshot.queryParamMap.get('googleError') === 'configuracion'
    ) {
      this.mensajeerror =
        'El acceso con Google aún no está configurado. Usa el registro institucional por ahora.';
      this.error.set(true);
    }
  }
  registrar(form: NgForm) {
    this.submitted = true;
    if (this.registrando) return;
    if (
      form.invalid ||
      (!this.registroGoogle && this.password !== this.confirmPassword) ||
      this.validartelefono(this.nuevoUsuario.telefono) ||
      !this.nuevoUsuario.carrera ||
      !this.aceptaTerminos ||
      this.procesandoImagen
    ) {
      return;
    }
    this.registrando = true;
    this.error.set(false);
    this.aviso.set(false);
    this.nuevoUsuario.rol = 'usuario';
    this.registrarcuenta
      .registrarCuenta(
        this.nuevoUsuario,
        this.password,
        'estudiante',
        this.aceptaTerminos,
        this.codigoGoogle,
      )
      .subscribe({
        next: (verificationSent) => {
          this.mensajeaviso = this.registroGoogle
            ? 'Cuenta completada. Ya puedes iniciar sesión con Google.'
            : verificationSent
              ? 'Cuenta creada. Revisa tu correo para verificarla antes de iniciar sesión.'
              : 'Cuenta creada, pero no se pudo enviar el correo. Intenta reenviarlo desde el inicio de sesión.';
          this.aviso.set(true);
          this.registrando = false;
        },
        error: (err) => {
          const errorMsg = extractErrorMessage(
            err,
            'Error al registrar el usuario. Intenta más tarde.',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
          this.registrando = false;
        },
      });
  }

  private cargarDatosGoogle(codigo: string): void {
    this.registrando = true;
    this.registrarcuenta.intercambiarCodigoGoogle(codigo).subscribe({
      next: (result) => {
        if (!result.RequiereRegistro || !result.CodigoRegistro) {
          void this.router.navigate(['/login']);
          return;
        }
        this.registroGoogle = true;
        this.codigoGoogle = result.CodigoRegistro;
        this.nuevoUsuario.correo = result.Email;
        this.nuevoUsuario.nombre = result.Nombre;
        this.nuevoUsuario.apellido_paterno = result.ApellidoPaterno;
        this.nuevoUsuario.apellido_materno = result.ApellidoMaterno;
        this.registrando = false;
      },
      error: () => {
        this.mensajeerror =
          'El registro con Google expiró. Vuelve a iniciar sesión con Google.';
        this.error.set(true);
        this.registrando = false;
      },
    });
  }
  irALogin() {
    this.router.navigate(['/login']);
  }

  iniciarRegistroGoogle(): void {
    if (!this.registrando) this.registrarcuenta.iniciarSesionGoogle(true);
  }

  alternarVisibilidadPassword(): void {
    this.mostrarPassword = !this.mostrarPassword;
  }

  alternarVisibilidadConfirmPassword(): void {
    this.mostrarConfirmPassword = !this.mostrarConfirmPassword;
  }

  async cargarImagen(
    event: Event,
    destino: 'perfil' | 'frente' | 'atras',
  ): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.procesandoImagen = true;
    this.error.set(false);
    try {
      const dataUrl = await processIdentityImage(file);
      const base64 = identityDataUrlToBase64(dataUrl);
      if (destino === 'perfil') {
        this.fotoPerfilPreview = dataUrl;
        this.nuevoUsuario.imagen_perfil = base64;
      } else if (destino === 'frente') {
        this.carnetFrentePreview = dataUrl;
        this.nuevoUsuario.imagen_frente_carnet = base64;
      } else {
        this.carnetAtrasPreview = dataUrl;
        this.nuevoUsuario.imagen_atras_carnet = base64;
      }
    } catch (error) {
      this.mensajeerror =
        error instanceof Error ? error.message : 'No se pudo leer la imagen.';
      this.error.set(true);
      input.value = '';
    } finally {
      this.procesandoImagen = false;
    }
  }

  guardarFirma(firma: string): void {
    this.firmaPreview = firma;
    this.nuevoUsuario.imagen_firma = identityDataUrlToBase64(firma);
  }

  validartelefono(telefono: string | null | undefined): boolean {
    const regex = /^[-+0-9]+$/;
    return !regex.test(<string>telefono);
  }
}
