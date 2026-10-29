import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ListaObjetosComponent } from './lista-objetos.component';
describe('ListaObjetosComponent', () => {
  let component: ListaObjetosComponent;
  let fixture: ComponentFixture<ListaObjetosComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [ListaObjetosComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(ListaObjetosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('consulta el lunes al cerrar el sábado', () => {
    jasmine.clock().install();
    jasmine.clock().mockDate(new Date(2026, 7, 22, 19, 0, 0));

    const siguienteHorario = component['siguienteBloqueDeMediaHora']();

    expect(siguienteHorario).toEqual(new Date(2026, 7, 24, 8, 0, 0));
    jasmine.clock().uninstall();
  });
});
