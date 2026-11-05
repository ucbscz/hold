import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import {
  ConfiguracionService,
  CONFIGURACION_PREDETERMINADA,
} from '@entities/configuracion';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { CustomSelectComponent } from '@shared/ui';
import { of, throwError } from 'rxjs';
import { AdminConfiguracionesComponent } from './admin-configuraciones.component';

describe('AdminConfiguracionesComponent', () => {
  let fixture: ComponentFixture<AdminConfiguracionesComponent>;
  let api: jasmine.SpyObj<ConfiguracionService>;
  beforeEach(async () => {
    api = jasmine.createSpyObj('ConfiguracionService', [
      'loadConfiguracion',
      'updateConfiguracion',
      'buscarResponsables',
    ]);
    const config = {
      ...CONFIGURACION_PREDETERMINADA,
      CarnetJefeCarrera: '123',
      NombreJefeCarrera: 'Ana Perez',
      FirmaJefeCarreraBase64:
        'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg"/%3E',
    };
    api.loadConfiguracion.and.returnValue(of(config));
    api.updateConfiguracion.and.callFake((dto) => of(dto));
    api.buscarResponsables.and.returnValue(
      of([{ Carnet: '456', Nombre: 'Luis Lopez' }]),
    );
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [AdminConfiguracionesComponent],
        providers: [{ provide: ConfiguracionService, useValue: api }],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(AdminConfiguracionesComponent);
    fixture.autoDetectChanges();
    await fixture.whenStable();
  });

  it('renders the assigned name, searches other users and clears the previous signature', async () => {
    const component = fixture.componentInstance;
    expect(fixture.nativeElement.textContent).toContain('Ana Perez');
    const select = fixture.debugElement
      .queryAll(By.directive(CustomSelectComponent))
      .map((d) => d.componentInstance as CustomSelectComponent)
      .at(-1)!;
    select.alternar();
    await new Promise((resolve) => setTimeout(resolve, 250));
    await fixture.whenStable();
    expect(api.buscarResponsables).toHaveBeenCalledWith('');
    expect(select.debeMostrarBusqueda).toBeTrue();
    select.seleccionar({ value: '456', label: 'Luis Lopez' });
    await fixture.whenStable();
    expect(component.config()?.NombreJefeCarrera).toBe('Luis Lopez');
    expect(component.config()?.FirmaJefeCarreraBase64).toBe('');
    component.guardar();
    expect(api.updateConfiguracion).not.toHaveBeenCalled();
    component.guardarfirma('firma-nueva');
    await fixture.whenStable();
    component.guardar();
    expect(api.updateConfiguracion).toHaveBeenCalledWith(
      jasmine.objectContaining({
        CarnetJefeCarrera: '456',
        FirmaJefeCarreraBase64: 'firma-nueva',
      }),
    );
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('app-aviso-exito'),
    ).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.config-message')).toBeNull();
  });

  it('shows inline errors without submitting and blocks empty numeric values', async () => {
    const input = fixture.nativeElement.querySelector(
      '[name="tiempoMinimoReserva"]',
    ) as HTMLInputElement;
    input.value = '10';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain(
      'El valor mínimo es 30',
    );
    input.value = '';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Completa este campo');
    fixture.componentInstance.guardar();
    expect(api.updateConfiguracion).not.toHaveBeenCalled();
  });

  it('reports the affected weekday and prevents duplicate exceptions', async () => {
    const component = fixture.componentInstance;
    component.horarios[0].FinMinutos = component.horarios[0].InicioMinutos;
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('.weekly-schedule__error')
        .textContent,
    ).toContain('30 minutos');
    component.fechaEspecial = '2026-12-25';
    component.agregarExcepcion();
    component.fechaEspecial = '2026-12-25';
    component.agregarExcepcion();
    expect(
      component.horarios.filter((h) => h.Fecha === '2026-12-25').length,
    ).toBe(1);
    expect(component.fechaRepetida).toBeTrue();
  });

  it('does not edit fallback settings when the server fails', async () => {
    fixture.componentInstance.config.set(null);
    api.loadConfiguracion.and.returnValue(
      throwError(() => new Error('Offline')),
    );
    fixture.componentInstance.cargarConfiguracion();
    await fixture.whenStable();
    expect(api.loadConfiguracion).toHaveBeenCalledWith(false);
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Reintentar');
  });

  for (const width of [320, 375, 768, 1024, 1440]) {
    it(`keeps settings and the special date aligned at ${width}px`, async () => {
      fixture.componentInstance.fechaEspecial = '2026-12-25';
      fixture.componentInstance.agregarExcepcion();
      fixture.detectChanges();
      const frame = document.createElement('iframe');
      frame.style.width = `${width}px`;
      frame.style.height = '900px';
      document.body.appendChild(frame);
      try {
        const doc = frame.contentDocument!;
        await Promise.all(
          Array.from(
            document.querySelectorAll<HTMLLinkElement>(
              'link[rel="stylesheet"]',
            ),
          ).map(
            (source) =>
              new Promise<void>((resolve, reject) => {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = source.href;
                link.onload = () => resolve();
                link.onerror = () => reject(new Error('Styles did not load'));
                doc.head.appendChild(link);
              }),
          ),
        );
        document
          .querySelectorAll('style')
          .forEach((style) => doc.head.appendChild(style.cloneNode(true)));
        doc.body.style.margin = '0';
        doc.body.appendChild(fixture.nativeElement);
        await new Promise<void>((resolve) =>
          requestAnimationFrame(() => resolve()),
        );
        expect(doc.documentElement.scrollWidth).toBeLessThanOrEqual(width);
        for (const element of doc.querySelectorAll<HTMLElement>(
          'input:not([type="checkbox"]),.cs-trigger,.btn,.btn-icon',
        )) {
          const rect = element.getBoundingClientRect();
          expect(rect.left)
            .withContext(element.outerHTML)
            .toBeGreaterThanOrEqual(0);
          expect(rect.right)
            .withContext(element.outerHTML)
            .toBeLessThanOrEqual(width);
          expect(rect.height).toBeGreaterThanOrEqual(44);
        }
        const date = doc
          .querySelector('.special-date input')!
          .getBoundingClientRect();
        const add = doc
          .querySelector('.special-date button')!
          .getBoundingClientRect();
        if (width > 640)
          expect(Math.abs(date.bottom - add.bottom)).toBeLessThanOrEqual(1);
        else expect(add.top).toBeGreaterThanOrEqual(date.bottom);
        expect(
          parseFloat(
            frame.contentWindow!.getComputedStyle(
              doc.querySelector('.config-field__control')!,
            ).borderRadius,
          ),
        ).toBeGreaterThan(20);
      } finally {
        document.body.appendChild(fixture.nativeElement);
        frame.remove();
      }
    });
  }
});
