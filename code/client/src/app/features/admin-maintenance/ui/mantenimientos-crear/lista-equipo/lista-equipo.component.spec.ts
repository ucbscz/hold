import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ListaEquipoComponent } from './lista-equipo.component';
import { Equipos } from '@entities/admin';
describe('ListaEquipoComponent', () => {
  let component: ListaEquipoComponent;
  let fixture: ComponentFixture<ListaEquipoComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [ListaEquipoComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(ListaEquipoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
  it('orders costs numerically and preserves the order through search and clearing', async () => {
    component.equiposcopia = [
      Object.assign(new Equipos(), {
        Id: 1,
        NombreGrupoEquipo: 'Equipo A',
        CostoReferencia: 100,
      }),
      Object.assign(new Equipos(), {
        Id: 2,
        NombreGrupoEquipo: 'Equipo B',
        CostoReferencia: 20,
      }),
    ];
    component.buscar();
    fixture.autoDetectChanges();
    const button = Array.from(
      fixture.nativeElement.querySelectorAll(
        'thead button',
      ) as NodeListOf<HTMLButtonElement>,
    ).find((b) => b.textContent?.trim() === 'Costo')!;
    button.click();
    await fixture.whenStable();
    expect(
      fixture.nativeElement.querySelector('tbody td').textContent.trim(),
    ).toBe('Equipo B');
    component.terminoBusqueda = 'Equipo';
    component.buscar();
    expect(component.equipos.map((e) => e.CostoReferencia)).toEqual([20, 100]);
    button.click();
    await fixture.whenStable();
    component.limpiarBusqueda();
    expect(component.equipos.map((e) => e.CostoReferencia)).toEqual([100, 20]);
  });
});
