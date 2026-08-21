import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Equipos } from '@entities/admin';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { EquiposTablaComponent } from './equipos-tabla.component';
describe('EquiposTablaComponent', () => {
  let component: EquiposTablaComponent;
  let fixture: ComponentFixture<EquiposTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [EquiposTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(EquiposTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('keeps the active search after the equipment collection reloads', () => {
    const station = Object.assign(new Equipos(), {
      Id: 1,
      NombreGrupoEquipo: 'Estacion de soldadura',
    });
    const lamp = Object.assign(new Equipos(), {
      Id: 2,
      NombreGrupoEquipo: 'Lampara de aumento',
    });
    component.equiposcopia = [station, lamp];
    component.aplicarFiltros(['estacion', '']);

    component.equiposcopia = [
      station,
      lamp,
      Object.assign(new Equipos(), {
        Id: 3,
        NombreGrupoEquipo: 'Mini dron',
      }),
    ];
    component.aplicarFiltros();

    expect(component.equipos).toEqual([station]);
  });
});
