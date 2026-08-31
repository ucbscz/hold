import { CatalogoInventarioService } from '@entities/equipment';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Muebles } from '@entities/admin';
import { MuebleService } from '@entities/furniture';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { Aviso, AvisoExitoComponent, MostrarerrorComponent } from '@shared/ui';
@Component({
  selector: 'app-muebles-editar',
  standalone: true,
  imports: [
    CustomSelectComponent,
    FormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
  ],
  templateUrl: './muebles-editar.component.html',
  styleUrl: './muebles-editar.component.css',
})
export class MueblesEditarComponent
  extends BaseTablaComponent
  implements OnChanges
{
  @Input() botoneditar: WritableSignal<boolean> = signal(true);
  @Output() actualizar: EventEmitter<void> = new EventEmitter<void>();
  @Input() muebleOriginal: Muebles = new Muebles();
  mueble: Muebles = { ...this.muebleOriginal };
  ambientes: OpcionSelect[] = [];
  ngOnInit() {
    this.catalogos.listar('ambientes').subscribe({
      next: (items) =>
        (this.ambientes = items.map((a) => ({ value: a.Id, label: a.Nombre }))),
      error: (e) => {
        this.mensajeerror = extractErrorMessage(e);
        this.error.set(true);
      },
    });
  }
  constructor(
    private readonly muebleapi: MuebleService,
    private readonly catalogos: CatalogoInventarioService,
  ) {
    super();
  }
  ngOnChanges() {
    this.mueble = { ...this.muebleOriginal };
  }
  validaredicion() {
    this.mensajeaviso = '¿Desea confirmar los cambios realizados al mueble?';
    this.aviso.set(true);
  }
  confirmar() {
    this.muebleapi.actualizarMueble(this.mueble).subscribe({
      next: (_response) => {
        this.actualizar.emit();
        this.mensajeexito = 'Mueble editado exitosamente.';
        this.exito.set(true);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al editar el mueble.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  cerrar() {
    this.botoneditar.set(false);
  }
  @HostListener('click', ['$event'])
  onOverlayClick(event: MouseEvent) {
    if (event.target === event.currentTarget) this.cerrar();
  }
}
