import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { PantallaCargaComponent } from './pantalla-carga.component';
describe('PantallaCargaComponent', () => {
  let component: PantallaCargaComponent;
  let fixture: ComponentFixture<PantallaCargaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [PantallaCargaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(PantallaCargaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
