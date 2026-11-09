import { Component, inject, Input, OnInit } from '@angular/core';
import { CarreraService } from '@entities/career';
import { GrupoEquipo, GrupoequipoService } from '@entities/equipment-group';
import {
  FormularioAdminComponent,
  FormularioAdminEnvioComponent,
  FormularioAdminMarcoComponent,
} from '@shared/lib/admin-form';
import { extractErrorMessage } from '@shared/lib/error';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-grupos-equipos-formulario',
  imports: [
    FormularioAdminMarcoComponent,
    FormularioAdminEnvioComponent,
    ValidatedFormsModule,
    CustomSelectComponent,
  ],
  templateUrl: './grupos-equipos-formulario.component.html',
  styleUrls: [
    '../../../../shared/lib/admin-form/formulario-admin.css',
    './grupos-equipos-formulario.component.css',
  ],
})
export class GruposEquiposFormularioComponent
  extends FormularioAdminComponent<GrupoEquipo>
  implements OnInit
{
  private readonly grupoEquipoService = inject(GrupoequipoService);
  private readonly carreraService = inject(CarreraService);
  protected readonly entidad = { nombre: 'grupo de equipo' };
  @Input() categorias: string[] = [];
  urlImportacion = '';
  importando = false;
  importacionCompleta = false;
  carrerasOpciones: OpcionSelect[] = [{ value: null, label: 'Sin carrera' }];

  ngOnInit(): void {
    this.cargarOpciones(
      this.carreraService.obtenerCarreras(),
      (carreras) =>
        (this.carrerasOpciones = [
          { value: null, label: 'Sin carrera' },
          ...carreras.map((c) => ({
            value: c.Id,
            label: `${c.Codigo} · ${c.Nombre}`,
          })),
        ]),
      'No se pudieron cargar las carreras.',
    );
  }

  importar(): void {
    const url = this.urlImportacion.trim();
    if (this.importando || !url) return;
    this.importando = true;
    this.importacionCompleta = false;
    this.error.set(false);
    this.grupoEquipoService
      .importarDesdeUrl(url)
      .pipe(finalize(() => (this.importando = false)))
      .subscribe({
        next: (preview) => {
          this.modelo.nombre = preview.Nombre || this.modelo.nombre;
          this.modelo.modelo = preview.Modelo || this.modelo.modelo;
          this.modelo.marca = preview.Marca || this.modelo.marca;
          this.modelo.descripcion =
            preview.Descripcion || this.modelo.descripcion;
          this.modelo.link = preview.UrlImagen || this.modelo.link;
          this.modelo.url_data_sheet =
            preview.UrlDataSheet || this.modelo.url_data_sheet;
          this.importacionCompleta = true;
        },
        error: (error) =>
          this.mostrarError(
            extractErrorMessage(
              error,
              'No se pudo importar la información de esa página.',
            ),
          ),
      });
  }

  protected crearModelo(): GrupoEquipo {
    return new GrupoEquipo();
  }

  protected crear(grupoEquipo: GrupoEquipo) {
    return this.grupoEquipoService.crearGrupoEquipo(grupoEquipo);
  }

  protected editar(grupoEquipo: GrupoEquipo) {
    return this.grupoEquipoService.editarGrupoEquipo(grupoEquipo);
  }
}
