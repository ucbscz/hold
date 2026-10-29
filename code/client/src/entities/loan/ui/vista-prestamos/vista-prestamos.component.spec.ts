import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { VistaPrestamosComponent } from './vista-prestamos.component';
describe('VistaPrestamosComponent', () => {
  let component: VistaPrestamosComponent;
  let fixture: ComponentFixture<VistaPrestamosComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [VistaPrestamosComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(VistaPrestamosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
