import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CatalogoInventarioService } from '@entities/equipment';
import { UsuarioServiceAPI } from '@entities/user';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { of } from 'rxjs';
import { CatalogosInventarioComponent } from './catalogos-inventario.component';

describe('CatalogosInventarioComponent', () => {
  let fixture: ComponentFixture<CatalogosInventarioComponent>;
  const api = {
    listar: jasmine.createSpy().and.returnValue(
      of([
        { Id: 1, Nombre: 'Sala Z' },
        {
          Id: 2,
          Nombre: 'Laboratorio',
          NombreAdministrador: 'Ana Perez',
          CarnetAdministrador: '123',
        },
      ]),
    ),
    guardar: jasmine.createSpy().and.returnValue(of({})),
    eliminar: jasmine.createSpy().and.returnValue(of({})),
  };
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [CatalogosInventarioComponent],
        providers: [
          { provide: CatalogoInventarioService, useValue: api },
          {
            provide: UsuarioServiceAPI,
            useValue: {
              obtenerUsuarios: () =>
                of([
                  {
                    carnet: '123',
                    nombre: 'Ana',
                    apellido_paterno: 'Perez',
                    rol: 'administrador_laboratorio',
                  },
                  { carnet: '321', nombre: 'Alumno', rol: 'estudiante' },
                ]),
            },
          },
        ],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(CatalogosInventarioComponent);
    fixture.componentRef.setInput('tipo', 'ambientes');
    fixture.detectChanges();
  });
  it('lists names, sorts both ways and keeps pagination below the table', () => {
    const names = () =>
      Array.from(
        fixture.nativeElement.querySelectorAll(
          'tbody tr td:first-child',
        ) as NodeListOf<HTMLElement>,
      ).map((n) => n.textContent?.trim());
    expect(names()).toEqual(['Laboratorio', 'Sala Z']);
    fixture.nativeElement.querySelector('.table-sort-button').click();
    fixture.detectChanges();
    expect(names()).toEqual(['Sala Z', 'Laboratorio']);
    expect(fixture.nativeElement.textContent).toContain('Ana Perez');
    const table = fixture.nativeElement
      .querySelector('.table-responsive')
      .getBoundingClientRect();
    expect(
      fixture.nativeElement
        .querySelector('app-table-pagination')
        .getBoundingClientRect().top,
    ).toBeGreaterThanOrEqual(table.bottom);
  });
  it('edits the responsible and exposes only laboratory administrators', () => {
    const c = fixture.componentInstance;
    expect(c.administradores.map((a) => a.value)).toEqual(['', '123']);
    c.abrirEditor(c.items[1]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('dialog').open).toBeTrue();
    c.guardar();
    expect(api.guardar).toHaveBeenCalledWith(
      'ambientes',
      'Laboratorio',
      2,
      '123',
    );
    expect(fixture.nativeElement.querySelector('dialog').open).toBeFalse();
  });
  it('keeps procedencias free of administrator fields', () => {
    fixture.componentRef.setInput('tipo', 'procedencias');
    fixture.detectChanges();
    expect(fixture.componentInstance.encabezados).toEqual(['Nombre']);
    expect(
      fixture.nativeElement.querySelector('.table-sort-button'),
    ).toBeNull();
    expect(fixture.nativeElement.querySelector('#catalog-admin')).toBeNull();
  });

  for (const width of [320, 375, 768, 1280]) {
    it(
      'keeps simple catalog actions reachable with long names at ' +
        width +
        'px',
      async () => {
        fixture.componentRef.setInput('tipo', 'procedencias');
        fixture.detectChanges();
        fixture.componentInstance.items = [
          { Id: 1, Nombre: 'Nombre'.repeat(40) },
        ];
        fixture.detectChanges();
        const frame = document.createElement('iframe');
        frame.style.width = width + 'px';
        frame.style.height = '800px';
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
                  link.onerror = () =>
                    reject(new Error('Unable to load application styles'));
                  doc.head.appendChild(link);
                }),
            ),
          );
          for (const style of document.querySelectorAll('style'))
            doc.head.appendChild(style.cloneNode(true));
          doc.body.style.margin = '0';
          doc.body.appendChild(fixture.nativeElement);
          await new Promise<void>((resolve) =>
            requestAnimationFrame(() => resolve()),
          );
          const table = doc.querySelector('table')!;
          const label = doc.querySelector('.table-cell-label') as HTMLElement;
          const actions = doc.querySelector('td.actions-column') as HTMLElement;
          const edit = actions.querySelector('button')!;
          expect(table.getBoundingClientRect().width).toBeLessThanOrEqual(
            width,
          );
          expect(label.getBoundingClientRect().right).toBeLessThanOrEqual(
            actions.getBoundingClientRect().left,
          );
          expect(label.scrollWidth).toBeGreaterThan(label.clientWidth);
          expect(
            frame.contentWindow!.getComputedStyle(label).textOverflow,
          ).toBe('ellipsis');
          expect(
            frame.contentWindow!.getComputedStyle(edit).borderTopWidth,
          ).toBe('0px');
          expect(edit.getBoundingClientRect().width).toBeGreaterThanOrEqual(
            width <= 768 ? 44 : 36,
          );
          const buttons = Array.from(actions.querySelectorAll('button'));
          expect(buttons[0].getBoundingClientRect().right).toBeLessThanOrEqual(
            buttons[1].getBoundingClientRect().left,
          );
        } finally {
          document.body.appendChild(fixture.nativeElement);
          frame.remove();
        }
      },
    );
  }
});
