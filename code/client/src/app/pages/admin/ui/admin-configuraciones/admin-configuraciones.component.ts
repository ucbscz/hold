import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FirmaComponent } from '@features/signature';
import {
  ConfiguracionDto,
  ConfiguracionService,
} from '@app/entities/configuracion/api/configuracion.service';

@Component({
  selector: 'app-admin-configuraciones',
  standalone: true,
  imports: [CommonModule, FormsModule, FirmaComponent],
  templateUrl: './admin-configuraciones.component.html',
  styleUrls: ['./admin-configuraciones.component.css'],
})
export class AdminConfiguracionesComponent implements OnInit {
  clickfirma = signal(false);
  nuevaFirma = signal<string | null>(null);

  config: WritableSignal<ConfiguracionDto | null> = signal(null);
  cargando = signal(false);
  guardando = signal(false);
  mensaje = signal('');
  error = signal(false);

  // View models for hours
  horarioInicioHora = 8;
  horarioInicioMinuto = 0;
  horarioFinHora = 18;
  horarioFinMinuto = 0;

  constructor(private configuracionService: ConfiguracionService) {}

  ngOnInit(): void {
    this.cargarConfiguracion();
  }

  cargarConfiguracion() {
    this.cargando.set(true);
    this.configuracionService.loadConfiguracion().subscribe({
      next: (data) => {
        this.config.set({ ...data });
        this.parseHorarios(data);
        this.cargando.set(false);
      },
      error: () => {
        this.mostrarMensaje('Error al cargar la configuración', true);
        this.cargando.set(false);
      },
    });
  }

  parseHorarios(data: ConfiguracionDto) {
    this.horarioInicioHora = Math.floor(data.HorarioInicioMinutos / 60);
    this.horarioInicioMinuto = data.HorarioInicioMinutos % 60;
    this.horarioFinHora = Math.floor(data.HorarioFinMinutos / 60);
    this.horarioFinMinuto = data.HorarioFinMinutos % 60;
  }

  guardarfirma(signatureData: string) {
    this.nuevaFirma.set(signatureData);
    this.config.update((c) => {
      if (c) c.FirmaJefeCarreraBase64 = signatureData;
      return c;
    });
  }

  limpiarFirma() {
    this.clickfirma.update((v) => !v);
    this.nuevaFirma.set(null);
  }

  guardar() {
    const currentConfig = this.config();
    if (!currentConfig) return;

    currentConfig.HorarioInicioMinutos =
      this.horarioInicioHora * 60 + this.horarioInicioMinuto;
    currentConfig.HorarioFinMinutos =
      this.horarioFinHora * 60 + this.horarioFinMinuto;

    this.guardando.set(true);
    this.configuracionService.updateConfiguracion(currentConfig).subscribe({
      next: (data) => {
        this.config.set(data);
        this.mostrarMensaje('Configuración guardada exitosamente', false);
        this.guardando.set(false);
      },
      error: () => {
        this.mostrarMensaje('Error al guardar la configuración', true);
        this.guardando.set(false);
      },
    });
  }

  mostrarMensaje(msg: string, esError: boolean) {
    this.mensaje.set(msg);
    this.error.set(esError);
    setTimeout(() => this.mensaje.set(''), 3000);
  }
}
