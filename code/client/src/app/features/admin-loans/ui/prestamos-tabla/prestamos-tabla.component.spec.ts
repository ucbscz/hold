import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { PrestamoDto } from '@entities/admin';
import { PrestamoAgrupados } from '@entities/loan';
import { PrestamosTablaComponent } from './prestamos-tabla.component';

describe('PrestamosTablaComponent', () => {
  let component: PrestamosTablaComponent;
  let fixture: ComponentFixture<PrestamosTablaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [PrestamosTablaComponent],
      }),
    ).compileComponents();

    fixture = TestBed.createComponent(PrestamosTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initially render newest loans first by Fecha Solicitud', () => {
    cargarPrestamos([
      crearPrestamo({
        id: 1,
        nombre: 'Antiguo',
        fechaSolicitud: '2026-06-12T18:21:00',
      }),
      crearPrestamo({
        id: 2,
        nombre: 'Reciente',
        fechaSolicitud: '2026-06-14T12:38:00',
      }),
    ]);

    expect(nombresRenderizados()).toEqual(['Reciente', 'Antiguo']);
  });

  it('should render loan table headers without sort buttons', () => {
    cargarPrestamos([
      crearPrestamo({ id: 1, nombre: 'CarnetMayor', carnet: '200' }),
      crearPrestamo({ id: 2, nombre: 'CarnetMenor', carnet: '100' }),
    ]);

    const sortButtons =
      fixture.nativeElement.querySelectorAll('.table-sort-button');
    const sortableHeaders =
      fixture.nativeElement.querySelectorAll('.sortable-th');
    const headers = Array.from(
      fixture.nativeElement.querySelectorAll('thead th'),
    ).map((header) => (header as HTMLElement).textContent!.trim());

    expect(sortButtons.length).toBe(0);
    expect(sortableHeaders.length).toBe(0);
    expect(headers).toContain('Carnet');
    expect(headers).toContain('Estado');
  });

  it('marks an active loan as overdue at its exact return time', () => {
    const prestamo = new PrestamoAgrupados([
      crearPrestamo({
        id: 3,
        nombre: 'Préstamo horario',
        estado: 'activo',
        fechaDevolucionEsperada: '2020-06-14T10:00:00',
      }),
    ]);

    expect(component.getEstadoCalculado(prestamo)).toBe('atrasado');
  });

  it('shows only the first equipment name when a loan has multiple items', () => {
    const prestamo = new PrestamoAgrupados([
      crearPrestamo({ id: 4, nombre: 'Usuario', equipo: 'Cargador Litio-Ion' }),
      crearPrestamo({
        id: 5,
        nombre: 'Usuario',
        equipo: 'Analizador de energía',
      }),
    ]);

    expect(component.resumenEquipo(prestamo)).toBe('Cargador Litio-Ion...');
    expect(component.detalleEquipos(prestamo)).toBe(
      'Cargador Litio-Ion, Analizador de energía',
    );
  });

  it('does not allow approving a loan after its expected start', () => {
    const prestamo = new PrestamoAgrupados([
      crearPrestamo({
        id: 6,
        nombre: 'Usuario',
        fechaPrestamoEsperada: '2020-06-13T00:00:00',
      }),
    ]);

    expect(component.puedeAprobar(prestamo)).toBeFalse();
  });

  it('exports the same data columns shown in the loan table', () => {
    cargarPrestamos([
      crearPrestamo({
        id: 7,
        nombre: 'Fernando',
        carnet: '12890061',
        telefono: '799430792',
      }),
    ]);
    const exportSpy = spyOn(component, 'exportarCsv');

    component.exportarPrestamos();

    expect(exportSpy).toHaveBeenCalledWith(
      'prestamos',
      [
        'Usuario',
        'Carnet',
        'Teléfono',
        'Equipos',
        'Fecha Solicitud',
        'Fecha Préstamo Esperada',
        'Fecha Devolución Esperada',
        'Estado',
      ],
      [
        [
          'Fernando',
          '12890061',
          '799430792',
          'Mini Dron',
          jasmine.any(String),
          jasmine.any(String),
          jasmine.any(String),
          'pendiente',
        ],
      ],
    );
  });

  function cargarPrestamos(prestamos: PrestamoDto[]): void {
    component.sortColumn = 'Fecha Solicitud';
    component.sortDirection = 'desc';
    component.agruparPrestamos(prestamos);
    component.aplicarFiltros();
    fixture.detectChanges();
  }

  function crearPrestamo(datos: {
    id: number;
    nombre: string;
    carnet?: string;
    telefono?: string;
    equipo?: string;
    fechaSolicitud?: string;
    fechaPrestamoEsperada?: string;
    fechaDevolucionEsperada?: string;
    estado?: string;
  }): PrestamoDto {
    return Object.assign(new PrestamoDto(), {
      Id: datos.id,
      NombreUsuario: datos.nombre,
      ApellidoPaternoUsuario: '',
      CarnetUsuario: datos.carnet ?? '12890061',
      TelefonoUsuario: datos.telefono ?? '799430792',
      NombreGrupoEquipo: datos.equipo ?? 'Mini Dron',
      FechaSolicitud: new Date(datos.fechaSolicitud ?? '2026-06-12T18:21:00'),
      FechaPrestamoEsperada: new Date(
        datos.fechaPrestamoEsperada ?? '2026-06-13T00:00:00',
      ),
      FechaDevolucionEsperada: new Date(
        datos.fechaDevolucionEsperada ?? '2026-06-14T00:00:00',
      ),
      EstadoPrestamo: datos.estado ?? 'pendiente',
    });
  }

  function nombresRenderizados(): string[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.nombre-usuario span'),
    ).map((elemento) => (elemento as HTMLElement).textContent!.trim());
  }
});
