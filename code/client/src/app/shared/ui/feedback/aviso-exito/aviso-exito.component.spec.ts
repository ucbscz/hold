import { signal } from '@angular/core';
import {
  ComponentFixture,
  fakeAsync,
  TestBed,
  tick,
} from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { AvisoExitoComponent } from './aviso-exito.component';
describe('AvisoExitoComponent', () => {
  let component: AvisoExitoComponent;
  let fixture: ComponentFixture<AvisoExitoComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [AvisoExitoComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(AvisoExitoComponent);
    component = fixture.componentInstance;
    component.exito = signal(true);
  });
  it('should create', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
  });

  it('should close automatically after one second', fakeAsync(() => {
    fixture.detectChanges();
    tick(1000);

    expect(component.exito()).toBeFalse();
  }));
});
