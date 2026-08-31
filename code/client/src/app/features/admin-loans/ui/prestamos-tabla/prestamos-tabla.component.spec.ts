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
    expect(fixture.nativeElement.querySelector('table')).not.toBeNull();
    expect(
      fixture.nativeElement.querySelector('app-tablero-prestamos'),
    ).toBeNull();
  });

  for (const width of [288, 343, 720, 1200]) {
    it(`keeps filters and pagination outside the table scroll area at ${width}px`, () => {
      const host: HTMLElement = fixture.nativeElement;
      host.style.display = 'block';
      host.style.width = `${width}px`;
      const container = host.querySelector('.prestamos-container')!;
      const bounds = container.getBoundingClientRect();
      for (const control of host.querySelectorAll(
        'app-buscador input, app-buscador app-custom-select, app-buscador .admin-toolbar-button',
      )) {
        const rect = control.getBoundingClientRect();
        expect(rect.width).toBeGreaterThan(0);
        expect(rect.left).toBeGreaterThanOrEqual(bounds.left - 1);
        expect(rect.right).toBeLessThanOrEqual(bounds.right + 1);
      }
      const table = host
        .querySelector('.loans-table-shell')!
        .getBoundingClientRect();
      const pagination = host
        .querySelector('app-table-pagination')!
        .getBoundingClientRect();
      expect(pagination.top).toBeGreaterThanOrEqual(table.bottom - 1);
      expect(
        host.querySelector('.table-responsive app-table-pagination'),
      ).toBeNull();
    });
  }

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

  it('should prioritize teachers and keep newest requests first', () => {
    cargarPrestamos([
      crearPrestamo({
        id: 1,
        nombre: 'Estudiante reciente',
        rol: 'estudiante',
        fechaSolicitud: '2026-06-14T15:00:00',
      }),
      crearPrestamo({
        id: 2,
        nombre: 'Docente antiguo',
        rol: 'docente',
        fechaSolicitud: '2026-06-13T10:00:00',
      }),
      crearPrestamo({
        id: 3,
        nombre: 'Docente reciente',
        rol: 'docente',
        fechaSolicitud: '2026-06-14T12:00:00',
      }),
    ]);

    expect(nombresRenderizados()).toEqual([
      'Docente reciente',
      'Docente antiguo',
      'Estudiante reciente',
    ]);
  });

  it('should expose and apply the administrator role filter', () => {
    cargarPrestamos([
      crearPrestamo({ id: 1, nombre: 'Administrador', rol: 'administrador' }),
      crearPrestamo({ id: 2, nombre: 'Docente', rol: 'docente' }),
    ]);

    component.seleccionarRol('administrador');
    fixture.detectChanges();

    expect(component.rolesFiltroOpciones).toContain(
      jasmine.objectContaining({
        value: 'administrador',
        label: 'Administrador general',
      }),
    );
    expect(nombresRenderizados()).toEqual(['Administrador']);
  });

  it('should render sortable loan table headers', () => {
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

    expect(sortButtons.length).toBe(component.columnas.length + 1);
    expect(sortableHeaders.length).toBe(0);
    expect(headers).toContain('Carnet');
    expect(headers).toContain('Estado');
    const carnetButton = Array.from(
      sortButtons as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.trim() === 'Carnet')!;
    carnetButton.click();
    fixture.detectChanges();
    expect(nombresRenderizados()).toEqual(['CarnetMenor', 'CarnetMayor']);
    carnetButton.click();
    fixture.detectChanges();
    expect(nombresRenderizados()).toEqual(['CarnetMayor', 'CarnetMenor']);
  });

  it('respects explicit date sorting instead of reapplying role priority', () => {
    cargarPrestamos([
      crearPrestamo({
        id: 1,
        nombre: 'Docente antiguo',
        rol: 'docente',
        fechaSolicitud: '2026-06-12T12:00:00',
      }),
      crearPrestamo({
        id: 2,
        nombre: 'Estudiante reciente',
        rol: 'estudiante',
        fechaSolicitud: '2026-06-14T12:00:00',
      }),
    ]);
    component.ordenarPorColumna('Fecha Solicitud');
    component.ordenarPorColumna('Fecha Solicitud');
    fixture.detectChanges();
    expect(nombresRenderizados()).toEqual([
      'Estudiante reciente',
      'Docente antiguo',
    ]);
    component.aplicarFiltros();
    fixture.detectChanges();
    expect(nombresRenderizados()).toEqual([
      'Estudiante reciente',
      'Docente antiguo',
    ]);
  });

  for (const column of [
    'Usuario',
    'Carnet',
    'Rol',
    'Teléfono',
    'Equipos',
    'Fecha Solicitud',
    'Fecha Préstamo Esperada',
    'Fecha Devolución Esperada',
    'Estado',
  ]) {
    it(`sorts the rendered rows in both directions by ${column}`, () => {
      cargarPrestamos([
        crearPrestamo({
          id: 1,
          nombre: 'Zeta',
          carnet: '900',
          telefono: '900',
          rol: 'estudiante',
          equipo: 'Zeta',
          estado: 'rechazado',
          fechaSolicitud: '2026-08-20',
          fechaPrestamoEsperada: '2030-08-20',
          fechaDevolucionEsperada: '2030-08-21',
        }),
        crearPrestamo({
          id: 2,
          nombre: 'Ana',
          carnet: '100',
          telefono: '100',
          rol: 'docente',
          equipo: 'Alfa',
          estado: 'pendiente',
          fechaSolicitud: '2026-08-10',
          fechaPrestamoEsperada: '2030-08-10',
          fechaDevolucionEsperada: '2030-08-11',
        }),
      ]);
      const button = Array.from(
        fixture.nativeElement.querySelectorAll(
          '.table-sort-button',
        ) as NodeListOf<HTMLButtonElement>,
      ).find((b) => b.textContent?.trim() === column)!;
      button.click();
      fixture.detectChanges();
      expect(nombresRenderizados()).toEqual(['Ana', 'Zeta']);
      button.click();
      fixture.detectChanges();
      expect(nombresRenderizados()).toEqual(['Zeta', 'Ana']);
    });
  }

  it('gives the role its own column without overlapping the phone', () => {
    cargarPrestamos([
      crearPrestamo({
        id: 1,
        nombre: 'Fernando',
        rol: 'administrador_laboratorio',
      }),
    ]);
    const cells = fixture.nativeElement.querySelectorAll(
      'tbody tr:first-child td',
    ) as NodeListOf<HTMLElement>;
    expect(cells[2].getBoundingClientRect().width).toBeGreaterThan(100);
    expect(cells[2].getBoundingClientRect().right).toBeLessThanOrEqual(
      cells[3].getBoundingClientRect().left + 1,
    );
    expect(getComputedStyle(cells[2]).overflowX).not.toBe('visible');
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
        'Rol',
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
          'Estudiante',
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
    rol?: string;
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
      TipoUsuario: datos.rol ?? 'estudiante',
    });
  }

  function nombresRenderizados(): string[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.nombre-usuario span'),
    ).map((elemento) => (elemento as HTMLElement).textContent!.trim());
  }
});
