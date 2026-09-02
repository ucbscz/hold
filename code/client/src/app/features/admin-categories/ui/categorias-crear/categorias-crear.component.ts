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
import { Categorias } from '@entities/admin';
import { CategoriaService } from '@entities/category';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { Aviso, MostrarerrorComponent, ToastService } from '@shared/ui';
@Component({
  selector: 'app-categorias-crear',
  standalone: true,
  imports: [ValidatedFormsModule, MostrarerrorComponent, Aviso],
  templateUrl: './categorias-crear.component.html',
  styleUrl: './categorias-crear.component.css',
})
export class CategoriasCrearComponent extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  nombreCategoria: string = '';
  constructor(private readonly categoriaService: CategoriaService) {
    super();
  }
  validarregistro() {
    if (this.nombreCategoria.trim() === '') {
      this.mensajeerror = 'el nombre de la categoria no puede estar vacia';
      this.error.set(true);
      return;
    }
    this.mensajeaviso = 'esta seguro de crear esta categoria?';
    this.aviso.set(true);
  }
  registrar() {
    if (!this.iniciarEnvio()) return;
    const categoria: Categorias = {
      Id: 0,
      Nombre: this.nombreCategoria,
    };
    this.categoriaService.crearCategoria(categoria).subscribe({
      next: (_response) => {
        this.Actualizar.emit();
        this.finalizarEnvio();
        this.toast.success('Categoría creada exitosamente.');
        this.cerrar();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al crear la categoría, intente más tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
        this.finalizarEnvio();
      },
    });
  }
  cerrar() {
    this.nombreCategoria = '';
    this.botoncrear.set(false);
  }
}
