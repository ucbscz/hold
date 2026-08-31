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
    expect(fixture.nativeElement.querySelector('#catalog-admin')).toBeNull();
  });
});
