import { Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Equipos } from '@entities/admin';
import { CatalogoInventarioService } from '@entities/equipment';
import { CustomSelectComponent, OpcionSelect } from '@shared/ui';

@Component({
  selector: 'app-catalogo-selector',
  standalone: true,
  imports: [FormsModule, CustomSelectComponent],
  template: `
    <div class="form-group">
      <label>Ambiente</label
      ><app-custom-select
        name="ambiente"
        [(ngModel)]="equipo.IdAmbiente"
        [opciones]="ambientes"
        placeholder="Seleccionar ambiente"
      />
    </div>
    <div class="form-group">
      <label>Procedencia</label
      ><app-custom-select
        name="procedencia"
        [(ngModel)]="equipo.IdProcedencia"
        [opciones]="procedencias"
        placeholder="Seleccionar procedencia"
      />
    </div>
    @if (error) {
      <p role="alert">{{ error }}</p>
    }
  `,
  styles: [
    ':host { display: block; } .form-group { display:grid; gap:10px; margin-bottom:20px; }',
  ],
})
export class CatalogoSelectorComponent implements OnInit {
  @Input({ required: true }) equipo!: Equipos;
  ambientes: OpcionSelect[] = [];
  procedencias: OpcionSelect[] = [];
  error = '';
  constructor(private readonly api: CatalogoInventarioService) {}
  ngOnInit() {
    for (const tipo of ['ambientes', 'procedencias'] as const)
      this.api.listar(tipo).subscribe({
        next: (items) =>
          (this[tipo] = items.map((i) => ({ value: i.Id, label: i.Nombre }))),
        error: () =>
          (this.error = 'No se pudieron cargar los catálogos de inventario.'),
      });
  }
}
