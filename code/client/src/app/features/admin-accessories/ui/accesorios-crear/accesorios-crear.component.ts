import {
  Component,
  EventEmitter,
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
  selector: 'app-accesorios-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
    CustomSelectComponent,
  ],
  templateUrl: './accesorios-crear.component.html',
  styleUrl: './accesorios-crear.component.css',
})
export class AccesoriosCrearComponent extends BaseTablaComponent {
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  equipos: Equipos[] = [];
  accesorio: Accesorio = new Accesorio();
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

  registrar() {
    this.accesorioapi.crearAccesorio(this.accesorio).subscribe({
      next: (_response) => {
        this.Actualizar.emit();
        this.mensajeexito = 'Accesorio creado exitosamente.';
        this.exito.set(true);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al crear el accesorio.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }

  confirmarcreacion() {
    this.mensajeaviso = '¿Está seguro que desea crear este accesorio?';
    this.aviso.set(true);
  }

  cerrar() {
    this.botoncrear.set(false);
  }
}
