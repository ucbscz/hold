import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Carrera } from '@entities/admin';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CarrerasTablaComponent } from './carreras-tabla.component';
describe('CarrerasTablaComponent', () => {
  let component: CarrerasTablaComponent;
  let fixture: ComponentFixture<CarrerasTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [CarrerasTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(CarrerasTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('keeps alphabetical order without sorting controls in a two-column table', () => {
    component.carrerascopia = [
      Object.assign(new Carrera(), { Id: 2, Nombre: 'Mecatrónica' }),
      Object.assign(new Carrera(), { Id: 1, Nombre: 'Civil' }),
    ];
    component.aplicarFiltros();
    fixture.detectChanges();
    expect(component.carreras.map((c) => c.Nombre)).toEqual([
      'Civil',
      'Mecatrónica',
    ]);
    expect(
      fixture.nativeElement.querySelector('.table-sort-button'),
    ).toBeNull();
    component.aplicarFiltros(['civil', '']);
    component.limpiarBusqueda();
    expect(component.carreras.map((c) => c.Nombre)).toEqual([
      'Civil',
      'Mecatrónica',
    ]);
  });

  it('keeps the active search after the career collection reloads', () => {
    const civil = Object.assign(new Carrera(), { Id: 1, Nombre: 'Civil' });
    const industrial = Object.assign(new Carrera(), {
      Id: 2,
      Nombre: 'Industrial',
    });
    component.carrerascopia = [civil, industrial];
    component.aplicarFiltros(['civil', '']);

    component.carrerascopia = [civil, industrial];
    component.aplicarFiltros();

    expect(component.carreras).toEqual([civil]);
  });
});
