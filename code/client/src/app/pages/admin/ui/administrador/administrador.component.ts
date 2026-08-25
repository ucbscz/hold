import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { UsuarioService } from '@entities/user';
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
import { SidebarComponent } from '@widgets/admin-sidebar';
import { AdminConfiguracionesComponent } from '../admin-configuraciones/admin-configuraciones.component';
@Component({
  selector: 'app-administrador',
  standalone: true,
  imports: [
    SidebarComponent,
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
  tablas: string[] = [
    'Prestamos',
    'Carreras',
    'Usuarios',
    'Categorias',
    'Componentes',
    'Empresas de Mantenimiento',
    'Equipos',
    'Gaveteros',
    'Grupos de Equipos',
    'Mantenimientos',
    'Muebles',
    'Accesorios',
    'Configuraciones',
  ];
  item: string = 'Prestamos';
  constructor(
    public router: Router,
    private usuario: UsuarioService,
  ) {}
  ngOnInit() {
    if (this.usuario.estaVacio()) {
      this.router.navigate(['/login']);
    } else if (this.usuario.obtenerRol() != 'administrador') {
      this.router.navigate(['/inicio']);
    }
  }
  clickitem(item: string) {
    this.item = item;
  }
}
