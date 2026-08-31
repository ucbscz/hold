import {
  Directive,
  ElementRef,
  Input,
  OnChanges,
  booleanAttribute,
  forwardRef,
  inject,
} from '@angular/core';
import {
  AbstractControl,
  NG_VALIDATORS,
  ValidationErrors,
  Validator,
  Validators,
} from '@angular/forms';

@Directive({
  selector:
    '[ngModel][required],input[ngModel][type=email],input[ngModel][type=number][step]',
  standalone: true,
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => FieldRulesDirective),
      multi: true,
    },
  ],
})
export class FieldRulesDirective implements Validator, OnChanges {
  @Input({ transform: booleanAttribute }) required = false;
  @Input() step: string | number = 'any';
  private readonly element =
    inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private onChange = () => {};

  validate(control: AbstractControl): ValidationErrors | null {
    if (
      this.required &&
      typeof control.value === 'string' &&
      !control.value.trim()
    )
      return { required: true };
    if (this.element.getAttribute('type') === 'email')
      return Validators.email(control);
    if (
      this.element.getAttribute('type') !== 'number' ||
      control.value == null ||
      control.value === ''
    )
      return null;
    const step = Number(this.step);
    if (!Number.isFinite(step) || step <= 0) return null;
    const base = Number(this.element.getAttribute('min') ?? 0);
    const value = (Number(control.value) - base) / step;
    return Number.isFinite(value) &&
      Math.abs(value - Math.round(value)) < 0.0000001
      ? null
      : { step: { step } };
  }

  registerOnValidatorChange(fn: () => void): void {
    this.onChange = fn;
  }
  ngOnChanges(): void {
    this.onChange();
  }
}
