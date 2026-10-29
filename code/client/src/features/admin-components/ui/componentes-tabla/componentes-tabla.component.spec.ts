import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { ComponentesTablaComponent } from './componentes-tabla.component';
describe('ComponentesTablaComponent', () => {
  let component: ComponentesTablaComponent;
  let fixture: ComponentFixture<ComponentesTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [ComponentesTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(ComponentesTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
