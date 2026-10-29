import { Component } from '@angular/core';
import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  tick,
} from '@angular/core/testing';
import { StickyScrollDirective } from './sticky-scroll.directive';

@Component({
  standalone: true,
  imports: [StickyScrollDirective],
  template: `
    <div class="table-container" appStickyScroll>
      <div class="table-content"></div>
    </div>
  `,
})
class TestHostComponent {}

describe('StickyScrollDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let host: HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TestHostComponent] });
    fixture = TestBed.createComponent(TestHostComponent);
    host = fixture.nativeElement.querySelector('.table-container');

    Object.defineProperty(host, 'clientWidth', {
      configurable: true,
      value: 320,
    });
    Object.defineProperty(host, 'scrollWidth', {
      configurable: true,
      value: 960,
    });
  });

  afterEach(() => {
    fixture.destroy();
    document.querySelectorAll('.sticky-scroll-rail').forEach((rail) => {
      rail.remove();
    });
  });

  it('shows one floating rail until the native scrollbar enters the viewport', fakeAsync(() => {
    spyOn(window, 'matchMedia').and.returnValue({
      matches: false,
    } as MediaQueryList);
    const rect = {
      bottom: window.innerHeight + 400,
      height: 900,
      left: 40,
      right: 360,
      top: 80,
      width: 320,
      x: 40,
      y: 80,
      toJSON: () => ({}),
    } as DOMRect;
    const rectSpy = spyOn(host, 'getBoundingClientRect').and.returnValue(rect);

    fixture.detectChanges();
    tick(20);

    const rail = document.querySelector<HTMLDivElement>('.sticky-scroll-rail');
    expect(rail).not.toBeNull();
    expect(rail?.hidden).toBeFalse();
    expect(host.classList.contains('sticky-scroll-host--floating')).toBeTrue();

    rectSpy.and.returnValue({
      ...rect,
      bottom: window.innerHeight - 20,
    } as DOMRect);
    window.dispatchEvent(new Event('scroll'));
    tick(20);

    expect(rail?.hidden).toBeTrue();
    expect(host.classList.contains('sticky-scroll-host--floating')).toBeFalse();
  }));
});
