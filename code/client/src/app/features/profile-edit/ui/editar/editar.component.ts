import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Usuario, UsuarioService, UsuarioServiceAPI } from '@entities/user';
import { extractErrorMessage } from '@shared/lib/error';
import {
  identityBase64ToDataUrl,
  identityDataUrlToBase64,
  processIdentityImage,
} from '@shared/lib/image/identity-image';
import { MostrarerrorComponent, ToastService } from '@shared/ui';
import { FirmaComponent } from '@features/signature';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-editar',
  imports: [
    CommonModule,
    ValidatedFormsModule,
    MostrarerrorComponent,
    FirmaComponent,
  ],
  templateUrl: './editar.component.html',
  styleUrl: './editar.component.css',
})
export class EditarComponent {
  @Input() botoneditar: WritableSignal<boolean> = signal(true);
  @Input() usuario: Usuario = new Usuario();
  @Output() guardado = new EventEmitter<Usuario>();
  localUsuario: Usuario = new Usuario();
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = '';
  contrasena: string = '';
  cargando: boolean = false;
  procesandoCarnet = false;
  carnetFrentePreview = '';
  carnetAtrasPreview = '';
  firmaPreview = '';
  clickfirma: WritableSignal<boolean> = signal(false);
  mostrarContrasena = false;

  constructor(
    private readonly usuarioApi: UsuarioServiceAPI,
    private readonly usuarioStore: UsuarioService,
    private readonly toast: ToastService,
  ) {}
  ngOnInit() {
    this.localUsuario = { ...this.usuario };
    this.carnetFrentePreview = identityBase64ToDataUrl(
      this.localUsuario.imagen_frente_carnet,
    );
    this.carnetAtrasPreview = identityBase64ToDataUrl(
      this.localUsuario.imagen_atras_carnet,
    );
    this.firmaPreview = identityBase64ToDataUrl(
      this.localUsuario.imagen_firma,
      'image/png',
    );
  }
  guardarFirma(signatureData: string): void {
    this.firmaPreview = signatureData;
    this.localUsuario.imagen_firma = identityDataUrlToBase64(signatureData);
  }
  confirmar() {
    if (this.cargando || this.procesandoCarnet) return;
    this.cargando = true;
    this.usuarioApi
      .actualizarPerfil(this.localUsuario, this.contrasena)
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: (usuarioActualizado) => {
          this.usuarioStore.actualizarUsuario(usuarioActualizado);
          this.guardado.emit(usuarioActualizado);
          this.toast.success('Perfil actualizado correctamente.');
          this.cerrar();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al actualizar el usuario, intente mas tarde',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
  }

  async cargarCarnet(event: Event, cara: 'frente' | 'atras'): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.procesandoCarnet = true;
    try {
      const dataUrl = await processIdentityImage(file);
      const base64 = identityDataUrlToBase64(dataUrl);
      if (cara === 'frente') {
        this.carnetFrentePreview = dataUrl;
        this.localUsuario.imagen_frente_carnet = base64;
      } else {
        this.carnetAtrasPreview = dataUrl;
        this.localUsuario.imagen_atras_carnet = base64;
      }
    } catch (error) {
      this.mensajeerror =
        error instanceof Error ? error.message : 'No se pudo leer la imagen.';
      this.error.set(true);
      input.value = '';
    } finally {
      this.procesandoCarnet = false;
    }
  }
  cerrar() {
    this.botoneditar.set(false);
  }
}
