import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { AccesoriosService } from '@entities/accessory';
import { Accesorio, Equipos } from '@entities/admin';
import { EquipoService } from '@entities/equipment';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
  OpcionSelect,
} from '@shared/ui';
@Component({
  selector: 'app-accesorios-editar',
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    AvisoExitoComponent,
    Aviso,
    CustomSelectComponent,
  ],
  templateUrl: './accesorios-editar.component.html',
  styleUrl: './accesorios-editar.component.css',
})
export class AccesoriosEditarComponent extends BaseTablaComponent {
  @Input() botoneditar: WritableSignal<boolean> = signal(true);
  @Output() actualizar: EventEmitter<void> = new EventEmitter<void>();
  @Input() accesorio: Accesorio = new Accesorio();
  equipos: Equipos[] = [];
  get equiposOpciones(): OpcionSelect[] {
    return this.equipos.map((e) => ({
      value: e.Id,
      label: `${e.NombreGrupoEquipo} ${e.Modelo} ${e.Marca} - ${e.CodigoImt}`,
    }));
  }

  constructor(
    private readonly accesorioapi: AccesoriosService,
    private equipoAPI: EquipoService,
  ) {
    super();
  }

  ngOnInit() {
    this.cargarEquipos();
  }

  cargarEquipos() {
    this.equipoAPI.obtenerEquipos().subscribe({
      next: (data) => {
        this.equipos = data;
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los equipos.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }

  confirmaredicion() {
    this.mensajeaviso = '¿Está seguro que desea editar el accesorio?';
    this.aviso.set(true);
  }

  confirmar() {
    this.accesorioapi.editarAccesorio(this.accesorio).subscribe({
      next: (_response) => {
        this.actualizar.emit();
        this.mensajeexito = 'Accesorio editado con éxito.';
        this.exito.set(true);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al editar el accesorio.',
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
