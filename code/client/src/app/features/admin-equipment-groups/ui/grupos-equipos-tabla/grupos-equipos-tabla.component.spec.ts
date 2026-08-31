import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { GrupoEquipo } from '@entities/equipment-group';
import { GruposEquiposTablaComponent } from './grupos-equipos-tabla.component';

describe('GruposEquiposTablaComponent', () => {
  let component: GruposEquiposTablaComponent;
  let fixture: ComponentFixture<GruposEquiposTablaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [GruposEquiposTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(GruposEquiposTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should sort groups by name in both directions', () => {
    component.gruposEquiposFiltrados = [
      Object.assign(new GrupoEquipo(), { id: 1, nombre: 'Zeta' }),
      Object.assign(new GrupoEquipo(), { id: 2, nombre: 'Alpha' }),
    ];

    component.ordenarPorColumna('Nombre');

    expect(
      component.gruposEquiposFiltrados.map((grupo) => grupo.nombre),
    ).toEqual(['Alpha', 'Zeta']);

    component.ordenarPorColumna('Nombre');

    expect(
      component.gruposEquiposFiltrados.map((grupo) => grupo.nombre),
    ).toEqual(['Zeta', 'Alpha']);
  });

  it('should expose accessible column sorting', () => {
    component.gruposEquiposFiltrados = [
      Object.assign(new GrupoEquipo(), { id: 0, nombre: 'Zeta' }),
      Object.assign(new GrupoEquipo(), { id: 0, nombre: 'Alpha' }),
    ];
    fixture.detectChanges();

    const sortableHeader = fixture.nativeElement.querySelector('.sortable-th');
    const sortButton =
      fixture.nativeElement.querySelector('.table-sort-button');
    const headers = Array.from(
      fixture.nativeElement.querySelectorAll('thead th'),
    ).map((header) => (header as HTMLElement).textContent!.trim());

    expect(sortableHeader).toBeNull();
    expect(sortButton).not.toBeNull();
    sortButton.click();
    fixture.detectChanges();
    expect(component.sortColumn).toBe('Nombre');
    expect(headers).toContain('Nombre');
  });
});
