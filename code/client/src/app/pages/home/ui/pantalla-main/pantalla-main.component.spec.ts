import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { PantallaMainComponent } from './pantalla-main.component';
describe('PantallaMainComponent', () => {
  let component: PantallaMainComponent;
  let fixture: ComponentFixture<PantallaMainComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [PantallaMainComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(PantallaMainComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should keep a stable category input between change detection cycles', () => {
    const categories = component.categoriasArray;

    fixture.detectChanges();
    fixture.detectChanges();

    expect(component.categoriasArray).toBe(categories);
  });

  it('closes category filters when clicking outside the search control', () => {
    component.showCategories = true;
    fixture.detectChanges();

    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(component.showCategories).toBeFalse();
  });

  it('summarizes active categories without filling the search field', () => {
    component.seleccionarCategoria('Electrónica');
    component.seleccionarCategoria('Control');
    fixture.detectChanges();

    const summary = fixture.nativeElement.querySelector('.category-summary');

    expect(summary.textContent).toContain('2 categorías');
    expect(component.solicitud).toBe('');
  });
});
