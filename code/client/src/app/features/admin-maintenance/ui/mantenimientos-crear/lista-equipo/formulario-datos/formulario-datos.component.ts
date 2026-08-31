import { Component, Input, signal, WritableSignal } from '@angular/core';
import { ValidatedFormsModule } from '@shared/lib/forms';
import { Equipos } from '@entities/admin';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';
import { MantenimientosServiceEquipos } from '../../../../model/mantenimientos-equipos.service';
@Component({
  selector: 'app-formulario-datos',
  imports: [ValidatedFormsModule, CustomSelectComponent],
  templateUrl: './formulario-datos.component.html',
  styleUrl: './formulario-datos.component.css',
})
export class FormularioDatosComponent {
  @Input() agregarEquipoSeleccionado: WritableSignal<boolean> = signal(false);
  @Input() equipo: Equipos = {} as Equipos;
  @Input() agregarequipo: WritableSignal<boolean> = signal(true);
  constructor(private readonly mantenimiento: MantenimientosServiceEquipos) {}
  tipomantenimiento: string = '';
  descripcion: string = '';
  submitted: boolean = false;
  tipoOpciones: OpcionSelect[] = [
    { value: 'preventivo', label: 'Preventivo' },
    { value: 'correctivo', label: 'Correctivo' },
  ];

  agregarEquipo() {
    this.submitted = true;
    if (!this.tipomantenimiento || !this.descripcion) {
      return;
    }
    this.mantenimiento.agregarEquipoMantenimiento(
      this.equipo.CodigoImt!,
      this.tipomantenimiento,
      this.descripcion,
      this.equipo.NombreGrupoEquipo!,
    );
    this.cerrarYRegresar();
  }
  cerrar() {
    this.agregarEquipoSeleccionado.set(false);
  }
  cerrarYRegresar() {
    this.agregarEquipoSeleccionado.set(false);
    this.agregarequipo.set(false);
  }
}
