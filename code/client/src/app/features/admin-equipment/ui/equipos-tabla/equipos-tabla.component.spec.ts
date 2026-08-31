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

  it('sorts equipment costs numerically through the rendered header', () => {
    component.equiposcopia = [
      Object.assign(new Equipos(), {
        Id: 1,
        NombreGrupoEquipo: 'Equipo A',
        CostoReferencia: 200,
      }),
      Object.assign(new Equipos(), {
        Id: 2,
        NombreGrupoEquipo: 'Equipo B',
        CostoReferencia: 50,
      }),
    ];
    component.aplicarFiltros();
    fixture.detectChanges();
    const buttons = Array.from(
      fixture.nativeElement.querySelectorAll('.table-sort-button'),
    ) as HTMLButtonElement[];
    const costo = buttons.find(
      (button) => button.textContent?.trim() === 'Costo',
    )!;
    costo.click();
    fixture.detectChanges();
    expect(component.equipos.map((e) => e.CostoReferencia)).toEqual([50, 200]);
    costo.click();
    fixture.detectChanges();
    expect(component.equipos.map((e) => e.CostoReferencia)).toEqual([200, 50]);
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
