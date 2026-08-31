import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AccesoriosTablaComponent } from '@features/admin-accessories';
import { ComponentesTablaComponent } from '@features/admin-components';
import { EquiposTablaComponent } from '@features/admin-equipment';
import { GruposEquiposTablaComponent } from '@features/admin-equipment-groups';
import { MueblesTablaComponent } from '@features/admin-furniture';
import { GaveterosTablaComponent } from '@features/admin-lockers';
import { MantenimientosTablaComponent } from '@features/admin-maintenance';
import { EmpresasMantenimientoTablaComponent } from '@features/admin-maintenance-companies';
import { UsuariosTablaComponent } from '@features/admin-users';
import { Tabla } from '@shared/lib/admin-table';
import { withDefaultTestingProviders } from '@shared/lib/testing';

type SortCase = {
  component: Type<Tabla & { ngOnInit(): void }>;
  target: string;
  source: string;
  row: (name: string, amount: number) => object;
};

const cases: SortCase[] = [
  {
    component: AccesoriosTablaComponent,
    target: 'accesorios',
    source: 'accesorioscopia',
    row: (name, n) => ({
      id: n,
      nombre: name,
      modelo: name,
      tipo: name,
      codigo_imt: n,
      precio: n,
    }),
  },
  {
    component: ComponentesTablaComponent,
    target: 'componentes',
    source: 'componentescopia',
    row: (name, n) => ({
      Id: n,
      Nombre: name,
      Modelo: name,
      Tipo: name,
      CodigoImtEquipo: n,
      PrecioReferencia: n,
    }),
  },
  {
    component: EquiposTablaComponent,
    target: 'equipos',
    source: 'equiposcopia',
    row: (name, n) => ({
      Id: n,
      NombreGrupoEquipo: name,
      EstadoEquipo: n === 100 ? 'inoperativo' : 'operativo',
      Ubicacion: name,
      CodigoImt: n,
      CostoReferencia: n,
    }),
  },
  {
    component: GruposEquiposTablaComponent,
    target: 'gruposEquiposFiltrados',
    source: 'gruposEquipos',
    row: (name, n) => ({
      id: n,
      nombre: name,
      Cantidad: n,
      modelo: name,
      marca: name,
      nombreCategoria: name,
      TiempoMaximoPrestamoDias: n,
      descripcion: name,
    }),
  },
  {
    component: MueblesTablaComponent,
    target: 'mueblesFiltrados',
    source: 'muebles',
    row: (name, n) => ({
      Id: n,
      Nombre: name,
      Tipo: name,
      NombreAmbiente: name,
      Ubicacion: name,
      Costo: n,
      NumeroGaveteros: n,
      Longitud: n,
      Profundidad: n,
      Altura: n,
    }),
  },
  {
    component: GaveterosTablaComponent,
    target: 'gaveteros',
    source: 'gaveteroscopia',
    row: (name, n) => ({
      Id: n,
      Nombre: name,
      Tipo: name,
      NombreMueble: name,
      Longitud: n,
      Altura: n,
      Profundidad: n,
    }),
  },
  {
    component: MantenimientosTablaComponent,
    target: 'mantenimientosFiltrados',
    source: 'mantenimientos',
    row: (name, n) => ({
      datosgrupo: {
        Id: n,
        NombreEmpresaMantenimiento: name,
        CodigoImtEquipo: String(n),
        FechaMantenimiento: new Date(2026, 0, n === 100 ? 1 : 20),
        FechaFinalDeMantenimiento: new Date(2026, 1, n === 100 ? 1 : 20),
        Costo: n,
      },
      matenimientos: [{ NombreGrupoEquipo: name }],
    }),
  },
  {
    component: EmpresasMantenimientoTablaComponent,
    target: 'empresas',
    source: 'empresascopia',
    row: (name, n) => ({
      Id: n,
      NombreEmpresa: name,
      NombreResponsable: name,
      ApellidoResponsable: name,
      Telefono: String(n),
      Nit: n,
    }),
  },
  {
    component: UsuariosTablaComponent,
    target: 'usuarios',
    source: 'usuarioscopia',
    row: (name, n) => ({
      carnet: String(n),
      nombre: name,
      apellido_paterno: name,
      apellido_materno: name,
      correo: name + '@ucb.edu.bo',
      telefono: String(n),
      rol: n === 100 ? 'docente' : 'estudiante',
      carrera: name,
      nombre_referencia: name,
      telefono_referencia: String(n),
    }),
  },
];

for (const test of cases) {
  describe(test.component.name + ' column sorting', () => {
    it('sorts every rendered column both ways and preserves the order after filtering', async () => {
      await TestBed.configureTestingModule(
        withDefaultTestingProviders({ imports: [test.component] }),
      ).compileComponents();
      const fixture = TestBed.createComponent(test.component);
      const component = fixture.componentInstance;
      spyOn(component, 'ngOnInit');
      const high = test.row('Zeta', 900);
      const low = test.row('Alfa', 100);
      Object.assign(component, {
        [test.target]: [high, low],
        [test.source]: [high, low],
      });
      fixture.detectChanges();
      fixture.autoDetectChanges();
      const buttons = Array.from(
        fixture.nativeElement.querySelectorAll('thead .table-sort-button'),
      ) as HTMLButtonElement[];
      expect(buttons.length).toBe(component.columnas.length);
      const firstCell = () =>
        fixture.nativeElement.querySelector('tbody tr td').textContent.trim();
      for (const button of buttons) {
        component.sortColumn = '';
        const column = button.textContent?.trim() ?? '';
        button.scrollIntoView({ block: 'center', inline: 'center' });
        const rect = button.getBoundingClientRect();
        const hit = document.elementFromPoint(
          rect.left + rect.width / 2,
          rect.top + rect.height / 2,
        ) as HTMLElement;
        expect(hit?.closest('button'))
          .withContext(column + ' is reachable')
          .toBe(button);
        button.click();
        await fixture.whenStable();
        expect(firstCell())
          .withContext(column + ' ascending')
          .toBe(test.component === UsuariosTablaComponent ? '100' : 'Alfa');
        button.click();
        await fixture.whenStable();
        expect(firstCell())
          .withContext(column + ' descending')
          .toBe(test.component === UsuariosTablaComponent ? '900' : 'Zeta');
        component.aplicarFiltros(['', '']);
        fixture.detectChanges();
        expect(firstCell())
          .withContext(column + ' after filtering')
          .toBe(test.component === UsuariosTablaComponent ? '900' : 'Zeta');
      }
    });
  });
}
