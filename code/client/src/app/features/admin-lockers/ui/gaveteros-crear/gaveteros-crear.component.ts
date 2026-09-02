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
import { Gaveteros, Muebles } from '@entities/admin';
import { MuebleService } from '@entities/furniture';
import { GaveteroService } from '@entities/locker';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import {
  Aviso,
  CustomSelectComponent,
  MostrarerrorComponent,
  ToastService,
} from '@shared/ui';
@Component({
  selector: 'app-gaveteros-crear',
  standalone: true,
  imports: [
    ValidatedFormsModule,
    MostrarerrorComponent,
    Aviso,
    CustomSelectComponent,
  ],
  templateUrl: './gaveteros-crear.component.html',
  styleUrl: './gaveteros-crear.component.css',
})
export class GaveterosCrearComponent extends BaseTablaComponent {
  private readonly toast = inject(ToastService);
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
    if (!this.iniciarEnvio()) return;
    this.gaveteroapi.crearGavetero(this.gavetero).subscribe({
      next: (_response) => {
        this.Actualizar.emit();
        this.finalizarEnvio();
        this.toast.success('Gavetero registrado con éxito.');
        this.cerrar();
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'Error al registrar el gavetero',
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
