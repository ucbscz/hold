import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CustomSelectComponent } from '@shared/ui/custom-select/custom-select.component';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { BuscadorComponent } from './buscador.component';
describe('BuscadorComponent', () => {
  let component: BuscadorComponent;
  let fixture: ComponentFixture<BuscadorComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [BuscadorComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(BuscadorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
  it('keeps the displayed query and emitted filter synchronized when cleared', async () => {
    const emit = spyOn(component.terminoBusqueda, 'emit');
    const input: HTMLInputElement =
      fixture.nativeElement.querySelector('input');
    input.value = 'estacion';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(emit).toHaveBeenCalledWith(['estacion', '']);
    fixture.nativeElement.querySelector('.clear-search').click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(input.value).toBe('');
    expect(emit).toHaveBeenCalledWith(['', '']);
  });
});

@Component({
  imports: [BuscadorComponent, CustomSelectComponent],
  template: `
    <app-buscador>
      <app-custom-select
        class="admin-filter-select"
        placeholder="Todos los estados"
      />
      <app-custom-select
        class="admin-filter-select"
        placeholder="Todos los roles"
      />
      <button class="btn admin-toolbar-button">Exportar</button>
      <button
        class="btn admin-toolbar-button admin-toolbar-button--icon"
        aria-label="Imprimir"
      >
        <i class="fas fa-print"></i>
      </button>
      <div search-secondary>Inicio desde</div>
    </app-buscador>
  `,
})
class ToolbarHost {}

describe('Admin search toolbar layout', () => {
  let fixture: ComponentFixture<ToolbarHost>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToolbarHost],
    }).compileComponents();
    fixture = TestBed.createComponent(ToolbarHost);
    fixture.nativeElement.style.display = 'block';
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
  });

  for (const width of [288, 343, 720, 1200]) {
    it(`keeps controls within a ${width}px container without overlapping`, () => {
      const host: HTMLElement = fixture.nativeElement;
      host.style.width = `${width}px`;
      const row = host.querySelector('.search-and-filter-row')!;
      const bounds = row.getBoundingClientRect();
      const controls = Array.from(
        row.querySelectorAll(
          '.search-input-wrapper, app-custom-select, .admin-toolbar-button',
        ),
      );
      const rectangles = controls.map((control) =>
        control.getBoundingClientRect(),
      );
      for (const rect of rectangles) {
        expect(rect.width).toBeGreaterThan(0);
        expect(rect.left).toBeGreaterThanOrEqual(bounds.left - 1);
        expect(rect.right).toBeLessThanOrEqual(bounds.right + 1);
      }
      for (let index = 1; index < rectangles.length; index++) {
        const previous = rectangles[index - 1];
        const current = rectangles[index];
        expect(
          current.left >= previous.right - 1 ||
            current.top >= previous.bottom - 1,
        ).toBeTrue();
      }
      expect(
        host.querySelector('.search-secondary [search-secondary]'),
      ).not.toBeNull();
      expect(row.querySelector('[search-secondary]')).toBeNull();
    });
  }
});
