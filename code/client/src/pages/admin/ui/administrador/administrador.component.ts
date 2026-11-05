import { Component } from '@angular/core';
import { CatalogosInventarioComponent } from '@features/admin-inventory-catalogs';
import { Router } from '@angular/router';
import {
  UsuarioService,
  esRolGestion,
  ROL_ADMINISTRADOR_LABORATORIO,
} from '@entities/user';
import { AccesoriosTablaComponent } from '@features/admin-accessories';
import { CarrerasTablaComponent } from '@features/admin-careers';
import { CategoriasTablaComponent } from '@features/admin-categories';
import { ComponentesTablaComponent } from '@features/admin-components';
import { EquiposTablaComponent } from '@features/admin-equipment';
import { GruposEquiposTablaComponent } from '@features/admin-equipment-groups';
import { MueblesTablaComponent } from '@features/admin-furniture';
import { PrestamosTablaComponent } from '@features/admin-loans';
import { GaveterosTablaComponent } from '@features/admin-lockers';
import { MantenimientosTablaComponent } from '@features/admin-maintenance';
import { EmpresasMantenimientoTablaComponent } from '@features/admin-maintenance-companies';
import { UsuariosTablaComponent } from '@features/admin-users';
import { AdminNavigationGroup, SidebarComponent } from '@widgets/admin-sidebar';
import { SidebarService } from '@shared/lib/sidebar';
import { AdminConfiguracionesComponent } from '@features/admin-settings';

const GENERAL_ADMIN_NAVIGATION: AdminNavigationGroup[] = [
  {
    label: 'Préstamos y personas',
    items: ['Prestamos', 'Usuarios', 'Carreras'],
  },
  {
    label: 'Inventario',
    items: [
      'Grupos de Equipos',
      'Equipos',
      'Componentes',
      'Accesorios',
      'Categorias',
    ],
  },
  {
    label: 'Ubicaciones',
    items: ['Ambientes', 'Muebles', 'Gaveteros', 'Procedencias'],
  },
  {
    label: 'Mantenimiento',
    items: ['Mantenimientos', 'Empresas de Mantenimiento'],
  },
  { label: 'Sistema', items: ['Configuraciones'] },
];

const LAB_ADMIN_NAVIGATION: AdminNavigationGroup[] = [
  { label: 'Préstamos y personas', items: ['Prestamos', 'Usuarios'] },
];

@Component({
  selector: 'app-administrador',
  standalone: true,
  imports: [
    SidebarComponent,
    CatalogosInventarioComponent,
    AccesoriosTablaComponent,
    CarrerasTablaComponent,
    UsuariosTablaComponent,
    CategoriasTablaComponent,
    ComponentesTablaComponent,
    EmpresasMantenimientoTablaComponent,
    EquiposTablaComponent,
    GaveterosTablaComponent,
    GruposEquiposTablaComponent,
    MantenimientosTablaComponent,
    MueblesTablaComponent,
    PrestamosTablaComponent,
    AdminConfiguracionesComponent,
  ],
  templateUrl: './administrador.component.html',
  styleUrls: ['./administrador.component.css'],
})
export class AdministradorComponent {
  navigationGroups: AdminNavigationGroup[] = [];
  item: string = 'Prestamos';
  constructor(
    public router: Router,
    private usuario: UsuarioService,
    public readonly sidebarService: SidebarService,
  ) {}
  ngOnInit() {
    const rol = this.usuario.obtenerUsuario().rol?.toLowerCase() ?? '';

    if (this.usuario.estaVacio()) {
      this.router.navigate(['/login']);
    } else if (!esRolGestion(rol)) {
      this.router.navigate(['/inicio']);
    } else {
      this.navigationGroups =
        rol === ROL_ADMINISTRADOR_LABORATORIO
          ? LAB_ADMIN_NAVIGATION
          : GENERAL_ADMIN_NAVIGATION;
    }
  }
  clickitem(item: string) {
    const allowedItems = this.navigationGroups.flatMap((group) => group.items);
    if (allowedItems.includes(item)) this.item = item;
  }
}
