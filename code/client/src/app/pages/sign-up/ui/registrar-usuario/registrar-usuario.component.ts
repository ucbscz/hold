import { INSTITUTIONAL_EMAIL } from '@shared/lib/forms';
import { CommonModule } from '@angular/common';
import { Component, signal } from '@angular/core';
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
import { CustomSelectComponent, ToastService } from '@shared/ui';
import { FirmaComponent } from '@features/signature';
@Component({
  selector: 'app-registrar-usuario',
  imports: [
    FormsModule,
    CommonModule,
    CustomSelectComponent,
    FirmaComponent,
    RouterLink,
  ],
  templateUrl: './registrar-usuario.component.html',
  styleUrl: './registrar-usuario.component.css',
})
export class RegistrarUsuarioComponent {
  protected readonly correoInstitucional = INSTITUTIONAL_EMAIL;
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
  constructor(
    private router: Router,
    private registrarcuenta: UsuarioServiceAPI,
    private carrerasS: CarreraService,
    private readonly route: ActivatedRoute,
    private readonly toast: ToastService,
  ) {}
  ngOnInit() {
    this.carrerasS.obtenerCarreras().subscribe({
      next: (response: Carrera[]) => {
        this.carreras = response.map((carrera) => carrera.Nombre ?? '');
      },
      error: (error) => {
        this.toast.error(
          extractErrorMessage(
            error,
            'No se pudieron cargar las carreras. Intenta nuevamente.',
          ),
        );
      },
    });
    const googleCode = this.route.snapshot.queryParamMap.get('google');
    if (googleCode) this.cargarDatosGoogle(googleCode);
    const googleError = this.route.snapshot.queryParamMap.get('googleError');
    if (googleError) {
      this.toast.error(
        googleError === 'configuracion'
          ? 'El registro con Google no está disponible. Intenta nuevamente más tarde.'
          : 'No se pudo verificar tu correo con Google. Usa tu cuenta institucional e intenta nuevamente.',
      );
    }
  }
  registrar(form: NgForm) {
    this.submitted = true;
    if (this.registrando) return;
    if (
      form.invalid ||
      !this.registroGoogle ||
      !this.codigoGoogle ||
      this.validartelefono(this.nuevoUsuario.telefono) ||
      !this.nuevoUsuario.carrera ||
      !this.aceptaTerminos ||
      this.procesandoImagen
    ) {
      return;
    }
    this.registrando = true;
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
        next: () => {
          this.registrando = false;
          this.toast.success(
            'Cuenta creada. Ya puedes iniciar sesión con Google.',
          );
          void this.router.navigate(['/login']);
        },
        error: (err) => {
          this.toast.error(
            extractErrorMessage(
              err,
              'No se pudo crear la cuenta. Revisa los datos e intenta nuevamente.',
            ),
          );
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
        this.registrando = false;
        this.toast.error(
          'La verificación con Google expiró. Vuelve a verificar tu correo.',
        );
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
      this.toast.error(
        error instanceof Error ? error.message : 'No se pudo leer la imagen.',
      );
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
