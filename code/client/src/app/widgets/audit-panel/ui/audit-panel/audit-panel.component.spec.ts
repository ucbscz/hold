import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AuditPanelComponent } from './audit-panel.component';

describe('AuditPanelComponent', () => {
  let component: AuditPanelComponent;
  let fixture: ComponentFixture<AuditPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({ imports: [AuditPanelComponent] }),
    ).compileComponents();
    fixture = TestBed.createComponent(AuditPanelComponent);
    component = fixture.componentInstance;
  });

  it('should parse legacy loan details into labeled fields', () => {
    const detail = component.parseDetalle(
      '{"texto":"Usuario: Fernando Terrazas Llanos (12890061). Equipos: Relé Temporizador. Inicio: 2026-08-22 17:30 UTC. Devolución: 2026-08-24 18:00 UTC."}',
    );

    expect(detail).toEqual(
      jasmine.objectContaining({
        usuarioNombre: 'Fernando Terrazas Llanos',
        usuarioCarnet: '12890061',
        equiposPrestamo: 'Relé Temporizador',
        fechaInicio: '2026-08-22T17:30:00Z',
        fechaDevolucion: '2026-08-24T18:00:00Z',
      }),
    );
  });

  it('should preserve structured loan audit details', () => {
    const detail = component.parseDetalle(
      '{"usuarioNombre":"Ana Pérez","usuarioCarnet":"100","equiposPrestamo":"Osciloscopio","fechaInicio":"2026-08-28T12:00:00Z","fechaDevolucion":"2026-08-28T13:00:00Z"}',
    );

    expect(detail?.usuarioNombre).toBe('Ana Pérez');
    expect(
      component.resumenObs({ Detalle: JSON.stringify(detail) } as never),
    ).toBe('Reserva de Osciloscopio');
  });
});
