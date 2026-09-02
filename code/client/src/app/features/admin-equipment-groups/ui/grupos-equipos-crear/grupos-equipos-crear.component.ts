import {
  Component,
  EventEmitter,
  inject,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { GrupoEquipo, GrupoequipoService } from '@entities/equipment-group';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { finalize } from 'rxjs';
import {
  Aviso,
  CustomSelectComponent,
  MostrarerrorComponent,
  ToastService,
} from '@shared/ui';
@Component({
  selector: 'app-grupos-equipos-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    CustomSelectComponent,
  ],
  templateUrl: './grupos-equipos-crear.component.html',
  styleUrl: './grupos-equipos-crear.component.css',
})
export class GruposEquiposCrearComponent extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Input() categorias: string[] = [];
  @Output() Actualizar = new EventEmitter<void>();
  grupoEquipo: GrupoEquipo = new GrupoEquipo();
  urlImportacion = '';
  importando = false;
  importacionCompleta = false;
  constructor(private readonly grupoEquipoapi: GrupoequipoService) {
    super();
  }
  validarregistro() {
    this.mensajeaviso = 'Desea registrar el nuevo grupo de equipo?';
    this.aviso.set(true);
  }
  importar(): void {
    const url = this.urlImportacion.trim();
    if (this.importando || !url) return;
    this.importando = true;
    this.importacionCompleta = false;
    this.error.set(false);
    this.grupoEquipoapi
      .importarDesdeUrl(url)
      .pipe(finalize(() => (this.importando = false)))
      .subscribe({
        next: (preview) => {
          this.grupoEquipo.nombre = preview.Nombre || this.grupoEquipo.nombre;
          this.grupoEquipo.modelo = preview.Modelo || this.grupoEquipo.modelo;
          this.grupoEquipo.marca = preview.Marca || this.grupoEquipo.marca;
          this.grupoEquipo.descripcion =
            preview.Descripcion || this.grupoEquipo.descripcion;
          this.grupoEquipo.link = preview.UrlImagen || this.grupoEquipo.link;
          this.grupoEquipo.url_data_sheet =
            preview.UrlDataSheet || this.grupoEquipo.url_data_sheet;
          this.importacionCompleta = true;
        },
        error: (error) => {
          this.mensajeerror = extractErrorMessage(
            error,
            'No se pudo importar la información de esa página.',
          );
          this.error.set(true);
        },
      });
  }
  registrar() {
    if (!this.iniciarEnvio()) return;
    this.grupoEquipoapi.crearGrupoEquipo(this.grupoEquipo).subscribe({
      next: (_response) => {
        this.Actualizar.emit();
        this.finalizarEnvio();
        this.toast.success('Grupo de equipo registrado exitosamente.');
        this.cerrar();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al registrar el grupo de equipo',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
        this.finalizarEnvio();
      },
    });
  }
  cerrar() {
    this.botoncrear.set(false);
  }
}
