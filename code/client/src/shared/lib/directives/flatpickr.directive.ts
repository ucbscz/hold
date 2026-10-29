import {
  Directive,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  Output,
  SimpleChanges,
} from '@angular/core';
import flatpickr from 'flatpickr';
import { Spanish } from 'flatpickr/dist/l10n/es';
import { Options } from 'flatpickr/dist/types/options';

@Directive({
  selector: '[appFlatpickr]',
  standalone: true,
})
export class FlatpickrDirective implements OnChanges, OnInit, OnDestroy {
  @Input() fpOptions: Partial<Options> = {};
  @Output() fpChange = new EventEmitter<Date[]>();
  @Output() fpReady = new EventEmitter<flatpickr.Instance>();

  private instance?: flatpickr.Instance;

  constructor(private readonly el: ElementRef<HTMLInputElement>) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.instance || !changes['fpOptions']) return;

    for (const [key, value] of Object.entries(this.fpOptions)) {
      this.instance.set(key as keyof Options, value);
    }
  }

  ngOnInit() {
    this.instance = flatpickr(this.el.nativeElement, {
      locale: Spanish,
      dateFormat: 'Y-m-d',
      allowInput: false,
      disableMobile: true,
      monthSelectorType: 'static',
      appendTo: document.body,
      position: (instance) => this.positionCalendar(instance),
      ...this.fpOptions,
      onChange: (dates, _dateStr) => {
        this.fpChange.emit(dates);

        this.el.nativeElement.dispatchEvent(new Event('input'));
        this.el.nativeElement.dispatchEvent(new Event('change'));
      },
      onReady: (_dates, _str, instance) => {
        const yearInput = instance.currentYearElement;
        if (yearInput) {
          yearInput.readOnly = true;
          yearInput.tabIndex = -1;
          yearInput.addEventListener('wheel', (e) => e.preventDefault(), {
            passive: false,
          });
          yearInput.addEventListener('keydown', (e) => e.preventDefault());
        }
        this.fpReady.emit(instance);
      },
      onOpen: (_dates, _str, instance) => {
        requestAnimationFrame(() => instance._positionCalendar());
      },
    }) as flatpickr.Instance;
  }

  ngOnDestroy() {
    this.instance?.destroy();
  }

  private positionCalendar(instance: flatpickr.Instance): void {
    const calendar = instance.calendarContainer;
    const anchor = instance._positionElement;
    if (!calendar || !anchor) return;

    const viewportPadding = 8;
    const gap = 6;
    const anchorRect = anchor.getBoundingClientRect();
    const calendarRect = calendar.getBoundingClientRect();
    const calendarWidth = calendarRect.width || calendar.offsetWidth;
    const calendarHeight = calendarRect.height || calendar.offsetHeight;
    const spaceAbove = anchorRect.top - viewportPadding - gap;
    const spaceBelow =
      window.innerHeight - anchorRect.bottom - viewportPadding - gap;
    const opensAbove =
      spaceAbove >= calendarHeight ||
      (spaceBelow < calendarHeight && spaceAbove > spaceBelow);

    const preferredTop = opensAbove
      ? anchorRect.top - calendarHeight - gap
      : anchorRect.bottom + gap;
    const maxTop = Math.max(
      viewportPadding,
      window.innerHeight - calendarHeight - viewportPadding,
    );
    const top = Math.min(Math.max(viewportPadding, preferredTop), maxTop);
    const centeredLeft =
      anchorRect.left + anchorRect.width / 2 - calendarWidth / 2;
    const maxLeft = Math.max(
      viewportPadding,
      window.innerWidth - calendarWidth - viewportPadding,
    );
    const left = Math.min(Math.max(viewportPadding, centeredLeft), maxLeft);

    calendar.style.setProperty('position', 'fixed', 'important');
    calendar.style.setProperty('top', `${top}px`, 'important');
    calendar.style.setProperty('left', `${left}px`, 'important');
    calendar.style.setProperty('right', 'auto', 'important');
    calendar.classList.toggle('arrowTop', !opensAbove);
    calendar.classList.toggle('arrowBottom', opensAbove);
    calendar.classList.add('arrowCenter');
    calendar.classList.remove('arrowLeft', 'arrowRight');
  }
}
