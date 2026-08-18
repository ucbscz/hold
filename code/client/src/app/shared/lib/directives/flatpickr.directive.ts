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
      position: 'auto center',
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
}
