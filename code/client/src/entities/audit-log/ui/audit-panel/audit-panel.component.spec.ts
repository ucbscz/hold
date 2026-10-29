import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AuditPanelComponent } from './audit-panel.component';
import { AuditLogDto } from '../../model/audit-log';

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

  it('should preserve protected audit changes without values', () => {
    const detail = component.parseDetalle(
      JSON.stringify({
        texto: 'Se modificaron 2 campos.',
        cambios: [
          {
            campo: 'Nombre',
            anterior: 'Ana',
            nuevo: 'Andrea',
            protegido: false,
          },
          {
            campo: 'Email',
            anterior: null,
            nuevo: null,
            protegido: true,
          },
        ],
      }),
    );

    expect(detail?.cambios?.length).toBe(2);
    expect(detail?.cambios?.[1].protegido).toBeTrue();
    expect(detail?.cambios?.[1].anterior).toBeNull();
  });

  it('opens a useful detail view even when a legacy record has no detail', () => {
    const log = {
      Id: 3,
      Accion: 'Editar',
      Entidad: 'Equipo',
      EntidadId: '25',
      EntidadNombre: 'IMT 240000025 · Osciloscopio',
    } as AuditLogDto;

    component.abrirObs(log);

    expect(component.obsLogAbierto).toBe(log);
    expect(component.obsAbierta?.texto).toContain('registro histórico');
  });

  it('does not expose generic sensitive fields in structured details', () => {
    const detail = component.parseDetalle(
      JSON.stringify({ tokenVerificacionHash: 'secret', cuentaRecreada: true }),
    );

    expect(detail?.datos).toEqual([
      { etiqueta: 'Cuenta recreada', valor: 'Sí' },
    ]);
  });

  it('offers an explicit detail action for every audit row', () => {
    component.logs = [
      {
        Id: 1,
        Accion: 'Crear',
        Entidad: 'Equipo',
        EntidadId: '10',
        EntidadNombre: 'IMT 10 · Equipo A',
      },
      {
        Id: 2,
        Accion: 'Editar',
        Entidad: 'Equipo',
        EntidadId: '11',
        EntidadNombre: 'IMT 11 · Equipo B',
        Detalle: '{"texto":"Se modificó 1 campo."}',
      },
    ];
    component.logsPaginados = component.logs;
    component.cargando = false;
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      '.audit-detail-button',
    );

    expect(buttons.length).toBe(2);
    expect(buttons[0].querySelector('.fa-eye')).not.toBeNull();
    expect(buttons[0].textContent.trim()).toBe('');
  });

  it('shows actions as a non-sortable column', () => {
    component.cargando = false;
    component.logs = [{ Id: 1, Accion: 'Editar' }] as AuditLogDto[];
    component.logsPaginados = component.logs;
    fixture.detectChanges();

    const lastHeader = fixture.nativeElement.querySelector(
      'thead th:last-child',
    );

    expect(lastHeader.textContent.trim()).toBe('Acciones');
    expect(lastHeader.querySelector('button')).toBeNull();
    expect(lastHeader.getAttribute('aria-sort')).toBeNull();
  });

  it('shows a structured audit record without repeating its summary', () => {
    const log = {
      Id: 8,
      Accion: 'Crear',
      Entidad: 'Carrera',
      EntidadNombre: 'prueba2',
      AdminNombre: 'Administrador',
      Timestamp: new Date('2026-09-10T12:00:00Z'),
      Detalle: JSON.stringify({
        texto: 'Se creó el registro de carrera “prueba2”.',
        cambios: [{ campo: 'Nombre', nuevo: 'prueba2' }],
      }),
    } as AuditLogDto;

    component.cargando = false;
    component.abrirObs(log);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.obs-lead')).toBeNull();
    expect(
      fixture.nativeElement.querySelectorAll('.snapshot-value').length,
    ).toBe(1);
  });

  it('does not claim that a missing record name is unavailable', () => {
    const log = { Entidad: 'Categoria' } as AuditLogDto;

    expect(component.registroLabel(log)).toBe('Categoría');
  });

  for (const column of ['Fecha', 'Actor', 'Acción', 'Registro']) {
    it(
      'sorts audit rows by ' + column + ' and returns to the first page',
      async () => {
        component.logs = [
          {
            Id: 2,
            Timestamp: new Date('2026-08-20T12:00:00Z'),
            AdminNombre: 'Zeta',
            AdminCarnet: '900',
            Accion: 'Eliminar',
            EntidadId: '900',
            EntidadNombre: 'Zeta',
            Detalle: 'Zeta',
          },
          {
            Id: 1,
            Timestamp: new Date('2026-08-10T12:00:00Z'),
            AdminNombre: 'Alfa',
            AdminCarnet: '100',
            Accion: 'Crear',
            EntidadId: '100',
            EntidadNombre: 'Alfa',
            Detalle: 'Alfa',
          },
        ] as AuditLogDto[];
        component.cargando = false;
        component.cambiarPagina(1);
        fixture.autoDetectChanges();
        const button = Array.from(
          fixture.nativeElement.querySelectorAll(
            'thead button',
          ) as NodeListOf<HTMLButtonElement>,
        ).find((b) => b.textContent?.trim() === column)!;
        component.paginaActual = 2;
        button.click();
        await fixture.whenStable();
        expect(component.paginaActual).toBe(1);
        expect(
          fixture.nativeElement.querySelector('.admin-name').textContent.trim(),
        ).toBe('Alfa');
        button.click();
        await fixture.whenStable();
        expect(
          fixture.nativeElement.querySelector('.admin-name').textContent.trim(),
        ).toBe('Zeta');
      },
    );
  }
});
