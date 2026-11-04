import { inject, signal, WritableSignal } from '@angular/core';
import { ToastService } from '@shared/ui';
import { Tabla } from './tabla';

export abstract class TablaGestion extends Tabla {
  protected readonly toast = inject(ToastService);
  expandedRowId: number | null = null;
  auditRefresh = 0;
  botoncrear: WritableSignal<boolean> = signal(false);
  botoneditar: WritableSignal<boolean> = signal(false);
  alertaeliminar = false;

  toggleExpand(id: number): void {
    this.expandedRowId = this.expandedRowId === id ? null : id;
  }
}
