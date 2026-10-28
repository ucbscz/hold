import { Component, inject } from '@angular/core';
import { Categorias } from '@entities/admin';
import { CategoriaService } from '@entities/category';
import { FormularioAdminComponent } from '@shared/lib/admin-form';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Aviso, MostrarerrorComponent } from '@shared/ui';

@Component({
  selector: 'app-categorias-formulario',
  imports: [ValidatedFormsModule, MostrarerrorComponent, Aviso],
  templateUrl: './categorias-formulario.component.html',
  styleUrl: '../../../../shared/lib/admin-form/formulario-admin.css',
})
export class CategoriasFormularioComponent extends FormularioAdminComponent<Categorias> {
  private readonly servicio = inject(CategoriaService);
  protected readonly entidad = { nombre: 'categoría', femenino: true };

  protected crearModelo(): Categorias {
    return new Categorias();
  }

  protected override validar(): string | null {
    return this.modelo.Nombre?.trim()
      ? null
      : 'El nombre de la categoría no puede estar vacío.';
  }

  protected crear(registro: Categorias) {
    return this.servicio.crearCategoria(registro);
  }

  protected editar(registro: Categorias) {
    return this.servicio.actualizarCategoria(registro);
  }
}
