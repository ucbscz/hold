import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Gaveteros, Muebles } from '@entities/admin';
import { MuebleService } from '@entities/furniture';
import { GaveteroService } from '@entities/locker';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  AvisoExitoComponent,
  CustomSelectComponent,
  MostrarerrorComponent,
} from '@shared/ui';
@Component({
  selector: 'app-gaveteros-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    AvisoExitoComponent,
    CustomSelectComponent,
  ],
  templateUrl: './gaveteros-crear.component.html',
  styleUrl: './gaveteros-crear.component.css',
})
export class GaveterosCrearComponent extends BaseTablaComponent {
  @Input() botoncrear: WritableSignal<boolean> = signal(true);
  @Output() Actualizar = new EventEmitter<void>();
  muebles: string[] = [];
  gavetero: Gaveteros = new Gaveteros();
  constructor(
    private readonly gaveteroapi: GaveteroService,
    private mueblesAPI: MuebleService,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarMuebles();
  }
  cargarMuebles() {
    this.mueblesAPI.obtenerMuebles().subscribe({
      next: (data: Muebles[]) => {
        this.muebles = data.map((mueble) => mueble.Nombre ?? '');
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al cargar los muebles',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  validarregistro() {
    this.mensajeaviso = '¿Desea registrar el gavetero?';
    this.aviso.set(true);
  }
  registrar() {
    this.gaveteroapi.crearGavetero(this.gavetero).subscribe({
      next: (_response) => {
        this.Actualizar.emit();
        this.mensajeexito = 'Gavetero registrado con exito';
        this.exito.set(true);
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al registrar el gavetero',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }
  cerrar() {
    this.botoncrear.set(false);
  }
}
