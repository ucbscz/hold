import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ConfiguracionDto,
  ConfiguracionService,
} from '@entities/configuracion';
import { FirmaComponent } from '@features/signature';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-admin-configuraciones',
  standalone: true,
  imports: [FormsModule, FirmaComponent],
  templateUrl: './admin-configuraciones.component.html',
  styleUrls: ['./admin-configuraciones.component.css'],
})
export class AdminConfiguracionesComponent implements OnInit {
  readonly clickfirma = signal(false);
  readonly config: WritableSignal<ConfiguracionDto | null> = signal(null);
  readonly cargando = signal(false);
  readonly guardando = signal(false);
  readonly mensaje = signal('');
  readonly error = signal(false);
  horarioInicioHora = 8;
  horarioInicioMinuto = 0;
  horarioFinHora = 18;
  horarioFinMinuto = 0;

  constructor(private readonly configuracionService: ConfiguracionService) {}

  ngOnInit(): void {
    this.cargarConfiguracion();
  }

  cargarConfiguracion(): void {
    this.cargando.set(true);
    this.configuracionService
      .loadConfiguracion()
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({
        next: (data) => {
          this.config.set({ ...data });
          this.parseHorarios(data);
        },
        error: () =>
          this.mostrarMensaje('Error al cargar la configuración', true),
      });
  }

  abrirFirma(): void {
    this.clickfirma.set(true);
  }

  guardarfirma(signatureData: string): void {
    this.config.update((current) =>
      current ? { ...current, FirmaJefeCarreraBase64: signatureData } : current,
    );
  }

  guardar(): void {
    const currentConfig = this.config();
    if (!currentConfig || !this.configuracionValida()) return;

    const updatedConfig = {
      ...currentConfig,
      HorarioInicioMinutos:
        this.horarioInicioHora * 60 + this.horarioInicioMinuto,
      HorarioFinMinutos: this.horarioFinHora * 60 + this.horarioFinMinuto,
    };

    this.guardando.set(true);
    this.configuracionService
      .updateConfiguracion(updatedConfig)
      .pipe(finalize(() => this.guardando.set(false)))
      .subscribe({
        next: (data) => {
          this.config.set({ ...data });
          this.parseHorarios(data);
          this.mostrarMensaje('Configuración guardada exitosamente', false);
        },
        error: () =>
          this.mostrarMensaje('Error al guardar la configuración', true),
      });
  }

  configuracionValida(): boolean {
    const current = this.config();
    if (!current) return false;

    const inicio = this.horarioInicioHora * 60 + this.horarioInicioMinuto;
    const fin = this.horarioFinHora * 60 + this.horarioFinMinuto;

    return (
      inicio >= 0 &&
      fin <= 24 * 60 - 1 &&
      inicio < fin &&
      current.MontoMinimoContrato >= 0 &&
      current.TiempoMinimoReservaMinutos >= 30 &&
      current.TiempoRecordatorioPrevioMinutos >= 0 &&
      current.MinutosGraciaAtraso >= 0 &&
      current.NombreJefeCarrera.trim().length > 0
    );
  }

  private parseHorarios(data: ConfiguracionDto): void {
    this.horarioInicioHora = Math.floor(data.HorarioInicioMinutos / 60);
    this.horarioInicioMinuto = data.HorarioInicioMinutos % 60;
    this.horarioFinHora = Math.floor(data.HorarioFinMinutos / 60);
    this.horarioFinMinuto = data.HorarioFinMinutos % 60;
  }

  private mostrarMensaje(msg: string, esError: boolean): void {
    this.mensaje.set(msg);
    this.error.set(esError);
    window.setTimeout(() => this.mensaje.set(''), 3000);
  }
}
