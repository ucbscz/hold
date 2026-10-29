import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { MantenimientosCrearComponent } from './mantenimientos-crear.component';
describe('MantenimientosCrearComponent', () => {
  let component: MantenimientosCrearComponent;
  let fixture: ComponentFixture<MantenimientosCrearComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [MantenimientosCrearComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(MantenimientosCrearComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
