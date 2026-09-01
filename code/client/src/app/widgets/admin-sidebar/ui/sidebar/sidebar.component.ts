import { CommonModule } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
  ViewEncapsulation,
} from '@angular/core';
import { SidebarService } from '../../model/sidebar.service';

const ICONS: Record<string, string> = {
  Ambientes: 'fas fa-door-open',
  Procedencias: 'fas fa-truck',
  Prestamos: 'fas fa-handshake',
  Carreras: 'fas fa-graduation-cap',
  Usuarios: 'fas fa-users',
  Categorias: 'fas fa-tags',
  Accesorios: 'fas fa-plug',
  Componentes: 'fas fa-microchip',
  Equipos: 'fas fa-laptop',
  'Grupos de Equipos': 'fas fa-layer-group',
  Gaveteros: 'fas fa-box',
  Muebles: 'fas fa-chair',
  Mantenimientos: 'fas fa-wrench',
  'Empresas de Mantenimiento': 'fas fa-building',
  Configuraciones: 'fas fa-sliders',
};

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
  encapsulation: ViewEncapsulation.None,
})
export class SidebarComponent {
  @Input() contenido: string[] = [];
  @Output() item = new EventEmitter<string>();
  @Input() activeItem = '';

  constructor(public sidebarService: SidebarService) {}

  getIcon(label: string): string {
    return ICONS[label] ?? 'fas fa-circle';
  }

  clickitem(item: string) {
    this.item.emit(item);
    this.sidebarService.close();
  }
}
