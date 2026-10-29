import { CommonModule } from '@angular/common';
import {
  Component,
  Input,
  OnInit,
  signal,
  SimpleChanges,
  WritableSignal,
} from '@angular/core';
import {
  PrestamoDto,
  PrestamosAPIService,
  RecursosPrestamoComponent,
  VistaPrestamosComponent,
} from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { TablePaginationComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { Aviso, AvisoExitoComponent, MostrarerrorComponent } from '@shared/ui';
import { HistorialBase } from '../base/historial-base';
import {
  ESTADOS_CANCELABLES,
  HISTORIAL_VACIO,
  HistorialVacio,
} from './historial-estado.config';

@Component({
  selector: 'app-historial-estado',
  standalone: true,
  imports: [
    CommonModule,
    RecursosPrestamoComponent,
    VistaPrestamosComponent,
    TablePaginationComponent,
    Aviso,
    AvisoExitoComponent,
    MostrarerrorComponent,
  ],
  templateUrl: './historial-estado.component.html',
  styleUrl: '../historial-list.shared.css',
})
export class HistorialEstadoComponent extends HistorialBase implements OnInit {
  @Input({ required: true }) estado = '';
  avisocancelar: WritableSignal<boolean> = signal(false);

  constructor(
    protected override prestamoApi: PrestamosAPIService,
    protected override usuario: UsuarioService,
  ) {
    super(prestamoApi, usuario);
  }

  get vacio(): HistorialVacio {
    return HISTORIAL_VACIO[this.estado];
  }

  get cancelable(): boolean {
    return ESTADOS_CANCELABLES.includes(this.estado);
  }

  get atrasado(): boolean {
    return this.estado === 'atrasado';
  }

  ngOnInit(): void {
    this.cargarDatos();
  }

  override ngOnChanges(changes: SimpleChanges): void {
    super.ngOnChanges(changes);
    if (changes['estado'] && !changes['estado'].firstChange) {
      this.paginaActual = 1;
      this.datos = new Map();
      this.cargarDatos();
    }
  }

  abrirAvisoCancelacion(event: Event, item: PrestamoDto): void {
    event.stopPropagation();
    this.itemSeleccionado = item;
    this.avisocancelar.set(true);
  }

  cancelar(): void {
    this.prestamoApi
      .cambiarEstadoPrestamo(this.itemSeleccionado!.Id, 'cancelado')
      .subscribe({
        next: () => {
          this.cargarDatos();
          this.itemSeleccionado = null;
          this.avisocancelar.set(false);
          this.mensajeexito = 'Préstamo cancelado con éxito.';
          this.exito.set(true);
        },
        error: (error) => {
          const msg = extractErrorMessage(error);
          this.mensajeerror = `Error al cancelar el préstamo: ${msg}`;
          this.error.set(true);
        },
      });
  }
}
