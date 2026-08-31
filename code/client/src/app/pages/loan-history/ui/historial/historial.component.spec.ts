import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { PrestamoDto } from '@entities/admin';
import { HistorialComponent } from './historial.component';
describe('HistorialComponent', () => {
  let component: HistorialComponent;
  let fixture: ComponentFixture<HistorialComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [HistorialComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(HistorialComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
  it('abre el contrato por id de prestamo, no por id de contrato', () => {
    component.prestamos = [
      Object.assign(new PrestamoDto(), { Id: 27, IdContrato: '901' }),
    ];
    component.abrirContrato({ id: 27, accion: 'contrato' });
    expect(component.contratoId).toBe(27);
    expect(component.contrato()).toBeTrue();
  });
  it('no abre contratos inexistentes y pide confirmar una cancelacion', () => {
    component.abrirContrato({ id: 99, accion: 'contrato' });
    expect(component.contrato()).toBeFalse();
    component.abrirContrato({ id: 27, accion: 'cancelar' });
    expect(component.avisoCancelar()).toBeTrue();
  });
});
