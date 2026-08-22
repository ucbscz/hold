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
});
