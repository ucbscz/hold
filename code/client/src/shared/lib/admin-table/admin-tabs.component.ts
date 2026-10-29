import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AdminTableTab } from './admin-table-tab';

@Component({
  selector: 'app-admin-tabs',
  host: { class: 'admin-tabs' },
  template: `
    @for (tab of tabs; track tab.id) {
      <button
        type="button"
        class="admin-tabs__button"
        [class.admin-tabs__button--active]="activa === tab.id"
        (click)="cambiar.emit(tab.id)"
      >
        {{ tab.label }}
      </button>
    }
  `,
})
export class AdminTabsComponent {
  @Input({ required: true }) activa!: AdminTableTab;
  @Output() cambiar = new EventEmitter<AdminTableTab>();
  protected readonly tabs: { id: AdminTableTab; label: string }[] = [
    { id: 'tabla', label: 'Tabla' },
    { id: 'auditoria', label: 'Auditoría' },
  ];
}
