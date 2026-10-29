import { ComponentFixture, TestBed } from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AccesoriosTablaComponent } from './accesorios-tabla.component';
describe('AccesoriosTablaComponent', () => {
  let component: AccesoriosTablaComponent;
  let fixture: ComponentFixture<AccesoriosTablaComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [AccesoriosTablaComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(AccesoriosTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });
  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
