import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { Categorias } from '@entities/admin';
import { CategoriasTablaComponent } from './categorias-tabla.component';
describe('CategoriasTablaComponent', () => {
  let component: CategoriasTablaComponent;
  let fixture: ComponentFixture<CategoriasTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [CategoriasTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(CategoriasTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('keeps categories alphabetical without sorting controls', () => {
    component.categoriascopia = [
      Object.assign(new Categorias(), { Id: 1, Nombre: 'Zeta' }),
      Object.assign(new Categorias(), { Id: 2, Nombre: 'Alpha' }),
    ];
    component.aplicarFiltros();
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('.table-sort-button');
    const headers = fixture.nativeElement.querySelectorAll('thead th');

    expect(component.categorias.map((c) => c.Nombre)).toEqual([
      'Alpha',
      'Zeta',
    ]);
    expect(button).toBeNull();
    expect(
      fixture.nativeElement.querySelector('tbody td').textContent.trim(),
    ).toBe('Alpha');
    expect(headers.length).toBe(2);
  });
});
