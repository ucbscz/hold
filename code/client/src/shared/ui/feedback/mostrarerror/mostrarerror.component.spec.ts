import { signal } from '@angular/core';
import {
  ComponentFixture,
  fakeAsync,
  TestBed,
  tick,
} from '@angular/core/testing';
import { withDefaultTestingProviders } from '@shared/lib/testing';
import { MostrarerrorComponent } from './mostrarerror.component';
describe('MostrarerrorComponent', () => {
  let component: MostrarerrorComponent;
  let fixture: ComponentFixture<MostrarerrorComponent>;
  beforeEach(async () => {
    await TestBed.configureTestingModule(
      withDefaultTestingProviders({
        imports: [MostrarerrorComponent],
      }),
    ).compileComponents();
    fixture = TestBed.createComponent(MostrarerrorComponent);
    component = fixture.componentInstance;
    component.error = signal(true);
  });
  it('should create', () => {
    fixture.detectChanges();

    expect(component).toBeTruthy();
  });

  it('should remain visible for eight seconds', fakeAsync(() => {
    fixture.detectChanges();
    tick(7999);
    expect(component.error()).toBeTrue();
    tick(1);

    expect(component.error()).toBeFalse();
  }));

  it('keeps inline errors visible until dismissed', fakeAsync(() => {
    fixture.componentRef.setInput('inline', true);
    fixture.detectChanges();
    tick(9000);
    expect(component.error()).toBeTrue();
    expect(fixture.nativeElement.querySelector('.toast')).toBeNull();
    expect(getComputedStyle(fixture.nativeElement).position).toBe('static');
    fixture.nativeElement.querySelector('button').click();
    expect(component.error()).toBeFalse();
  }));
});
