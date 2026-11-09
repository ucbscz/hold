import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PrestamoDto } from '@entities/loan';
import { ESTADO_EQUIPO_OPCIONES } from '@entities/equipment';
import { CustomSelectComponent } from '@shared/ui';

export interface DevolucionPrestamo {
  observacion: string;
  bloquearUsuario: boolean;
  equipos: { CodigoImt: string; EstadoEquipo: string }[];
}

@Component({
  selector: 'app-devolucion-prestamo',
  standalone: true,
  imports: [FormsModule, CustomSelectComponent],
  templateUrl: './devolucion-prestamo.component.html',
  styleUrls: ['../modal-prestamo.css', './devolucion-prestamo.component.css'],
})
export class DevolucionPrestamoComponent implements OnInit {
  @Input() equipos: PrestamoDto[] = [];
  @Output() cancelar = new EventEmitter<void>();
  @Output() devolver = new EventEmitter<DevolucionPrestamo>();

  readonly estadoEquipoOpciones = ESTADO_EQUIPO_OPCIONES;
  estados: { [codigoImt: string]: string } = {};
  observacion = '';
  bloquearUsuario = false;

  ngOnInit(): void {
    for (const eq of this.equipos) {
      if (eq.CodigoImt) this.estados[eq.CodigoImt] = 'operativo';
    }
  }

  confirmar(): void {
    this.devolver.emit({
      observacion: this.observacion,
      bloquearUsuario: this.bloquearUsuario,
      equipos: this.equipos
        .filter((eq) => !!eq.CodigoImt)
        .map((eq) => ({
          CodigoImt: eq.CodigoImt!,
          EstadoEquipo: this.estados[eq.CodigoImt!] ?? 'operativo',
        })),
    });
  }
}
