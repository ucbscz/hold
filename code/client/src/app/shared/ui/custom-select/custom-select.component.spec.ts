import {
  ComponentFixture,
  fakeAsync,
  TestBed,
  tick,
} from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { CustomSelectComponent } from './custom-select.component';

describe('CustomSelectComponent', () => {
  let component: CustomSelectComponent;
  let fixture: ComponentFixture<CustomSelectComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CustomSelectComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CustomSelectComponent);
    component = fixture.componentInstance;
  });

  it('should show search for long option lists and filter labels', () => {
    component.opciones = [
      'Preventivo',
      'Correctivo',
      'Predictivo',
      'Inspección',
      'Calibración',
      'Actualización',
    ];
    fixture.detectChanges();

    fixture.debugElement.query(By.css('.cs-trigger')).nativeElement.click();
    fixture.detectChanges();

    const search = document.body.querySelector(
      '.cs-search__input',
    ) as HTMLInputElement;
    search.value = 'calibracion';
    search.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const labels = Array.from(document.body.querySelectorAll('.cs-item')).map(
      (item) => item.textContent?.trim(),
    );
    expect(labels).toEqual(['Calibración']);
  });

  it('should render its menu outside transformed form containers', () => {
    fixture.detectChanges();

    const menu = document.body.querySelector('.cs-menu');
    expect(menu?.parentElement).toBe(document.body);

    fixture.destroy();
    expect(menu?.isConnected).toBeFalse();
  });

  it('should focus search only once and ignore its own scrolling', fakeAsync(() => {
    component.opciones = [
      'Preventivo',
      'Correctivo',
      'Predictivo',
      'Inspección',
      'Calibración',
      'Actualización',
    ];
    fixture.detectChanges();

    const search = document.body.querySelector(
      '.cs-search__input',
    ) as HTMLInputElement;
    const focusSpy = spyOn(search, 'focus');

    fixture.debugElement.query(By.css('.cs-trigger')).nativeElement.click();
    tick(32);

    const options = document.body.querySelector('.cs-options') as HTMLElement;
    options.dispatchEvent(new Event('scroll', { bubbles: true }));
    component.onResize();
    tick(32);

    expect(focusSpy).toHaveBeenCalledTimes(1);
    expect(focusSpy).toHaveBeenCalledWith({ preventScroll: true });
  }));
});
