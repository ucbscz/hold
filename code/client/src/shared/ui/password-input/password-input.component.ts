import { Component, forwardRef, Input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-password-input',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PasswordInputComponent),
      multi: true,
    },
  ],
  template: `
    <div class="password-field">
      <input
        [id]="inputId"
        [type]="visible ? 'text' : 'password'"
        [value]="value"
        [disabled]="disabled"
        [required]="required"
        [attr.aria-invalid]="invalid"
        [attr.aria-describedby]="describedBy || null"
        [placeholder]="placeholder"
        maxlength="30"
        autocomplete="new-password"
        (input)="updateValue($event)"
        (blur)="onTouched()"
      />
      <button
        type="button"
        [disabled]="disabled"
        [attr.aria-label]="
          visible ? 'Ocultar contraseña' : 'Mostrar contraseña'
        "
        [attr.title]="visible ? 'Ocultar contraseña' : 'Mostrar contraseña'"
        [attr.aria-controls]="inputId"
        [attr.aria-pressed]="visible"
        (click)="visible = !visible"
      >
        <i
          class="fas"
          [class.fa-eye]="!visible"
          [class.fa-eye-slash]="visible"
          aria-hidden="true"
        ></i>
      </button>
    </div>
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }
    .password-field {
      position: relative;
    }
    input {
      box-sizing: border-box;
      width: 100%;
      min-width: 0;
      min-height: 44px;
      padding: 0.75rem 3.25rem 0.75rem 1rem;
      border: 2px solid var(--border);
      border-radius: var(--radius-full);
      font: inherit;
      color: var(--ink);
      background: var(--surface);
    }
    input:focus-visible {
      outline: 2px solid var(--interactive-text);
      outline-offset: 2px;
    }
    input[aria-invalid='true'] {
      border-color: var(--error);
    }
    button {
      position: absolute;
      right: 4px;
      top: 50%;
      transform: translateY(-50%);
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 44px;
      height: 44px;
      padding: 0;
      min-width: 44px;
      border: 0;
      border-radius: var(--radius-full);
      background: transparent;
      color: var(--ink-muted);
      cursor: pointer;
    }
    button:hover {
      background: var(--interactive-subtle);
      color: var(--ink-secondary);
    }
    button:focus-visible {
      outline: 2px solid var(--interactive-text);
      outline-offset: -2px;
    }
    button:disabled {
      cursor: not-allowed;
      opacity: 0.5;
    }
  `,
})
export class PasswordInputComponent implements ControlValueAccessor {
  @Input() inputId = '';
  @Input() placeholder = '';
  @Input() required = false;
  @Input() invalid = false;
  @Input() describedBy = '';
  value = '';
  visible = false;
  disabled = false;
  private onChange: (value: string) => void = () => {};
  onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    this.value = value ?? '';
  }
  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(disabled: boolean): void {
    this.disabled = disabled;
  }
  updateValue(event: Event): void {
    this.value = (event.target as HTMLInputElement).value;
    this.onChange(this.value);
  }
}
