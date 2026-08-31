import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Componente, Equipos } from '@entities/admin';
import { ComponenteService } from '@entities/component';
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
  selector: 'app-componentes-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    AvisoExitoComponent,
    Aviso,
    CustomSelectComponent,
  ],
  templateUrl: './componentes-crear.component.html',
  styleUrl: './componentes-crear.component.css',
})
export class ComponentesCrearComponent extends BaseTablaComponent {
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  equipos: Equipos[] = [];
  componente: Componente = new Componente();
  get equiposOpciones(): OpcionSelect[] {
    return this.equipos.map((e) => ({
      value: e.Id,
      label: `${e.NombreGrupoEquipo} ${e.CodigoImt}`,
    }));
  }
  constructor(
    private readonly componenteService: ComponenteService,
    private equiposAPI: EquipoService,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarEquipos();
  }
  cargarEquipos() {
    this.equiposAPI.obtenerEquipos().subscribe({
      next: (data: Equipos[]) => {
        this.equipos = data;
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al obtener los equipos , intente mas tarde',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  validarregistro() {
    this.mensajeaviso = 'Estas seguro de crear este componente?';
    this.aviso.set(true);
  }
  registrar() {
    this.componenteService.crearComponente(this.componente).subscribe({
      next: (_response) => {
        this.Actualizar.emit();
        this.mensajeexito = 'Componente creado con exito';
        this.exito.set(true);
      },
      error: (_error) => {
        this.mensajeerror = 'Error al crear el componente , Intente mas tarde ';
        this.error.set(true);
      },
    });
  }
  cerrar() {
    this.botoncrear.set(false);
  }
}
