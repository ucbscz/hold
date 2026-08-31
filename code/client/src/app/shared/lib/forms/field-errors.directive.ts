import {
  AfterViewInit,
  Directive,
  ElementRef,
  Input,
  OnDestroy,
  Renderer2,
  inject,
} from '@angular/core';
import { NgControl, ValidationErrors } from '@angular/forms';
import { Subscription } from 'rxjs';

let nextErrorId = 0;

@Directive({
  selector: '[ngModel],[formControl],[formControlName]',
  standalone: true,
})
export class FieldErrorsDirective implements AfterViewInit, OnDestroy {
  @Input() fieldErrors = true;
  @Input() validationMessages: Record<string, string> = {};
  private readonly control = inject(NgControl, { self: true });
  private readonly host =
    inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly renderer = inject(Renderer2);
  private readonly id = `field-error-${++nextErrorId}`;
  private subscription?: Subscription;
  private message?: HTMLElement;
  private destroyed = false;

  ngAfterViewInit(): void {
    if (!this.fieldErrors) return;
    this.subscription = this.control.control?.events.subscribe(() =>
      this.render(),
    );
    queueMicrotask(() => {
      if (!this.destroyed) this.render();
    });
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.subscription?.unsubscribe();
    this.message?.remove();
  }

  private render(): void {
    const control = this.control.control;
    if (!control) return;
    const text =
      control.invalid && (control.dirty || control.touched)
        ? this.errorMessage(control.errors ?? {})
        : '';
    const target = this.host.matches('input,textarea,select')
      ? this.host
      : this.host.querySelector<HTMLElement>(
          'input,button[role="combobox"],button',
        );
    if (target) {
      const descriptions = (target.getAttribute('aria-describedby') ?? '')
        .split(/\s+/)
        .filter((id) => id && id !== this.id);
      if (text) descriptions.push(this.id);
      if (descriptions.length)
        this.renderer.setAttribute(
          target,
          'aria-describedby',
          descriptions.join(' '),
        );
      else this.renderer.removeAttribute(target, 'aria-describedby');
      this.renderer.setAttribute(target, 'aria-invalid', String(!!text));
    }
    if (!text) {
      this.message?.remove();
      this.message = undefined;
      this.renderer.removeClass(this.host, 'field-invalid');
      return;
    }
    this.renderer.addClass(this.host, 'field-invalid');
    if (!this.message) {
      const anchor =
        this.host.closest(
          '.config-field__control,.password-field,.password-input-container,.input-with-icon',
        ) ?? this.host;
      this.message = this.renderer.createElement('span');
      this.renderer.setAttribute(this.message, 'id', this.id);
      this.renderer.setAttribute(this.message, 'class', 'field-error');
      this.renderer.setAttribute(this.message, 'aria-live', 'polite');
      this.renderer.insertBefore(
        anchor.parentNode,
        this.message,
        anchor.nextSibling,
      );
    }
    this.renderer.setProperty(this.message, 'textContent', text);
  }

  private errorMessage(errors: ValidationErrors): string {
    const key =
      [
        'required',
        'email',
        'minlength',
        'maxlength',
        'min',
        'max',
        'pattern',
      ].find((key) => errors[key]) ?? Object.keys(errors)[0];
    if (this.validationMessages[key]) return this.validationMessages[key];
    switch (key) {
      case 'required':
        return 'Completa este campo.';
      case 'email':
        return 'Ingresa un correo válido.';
      case 'minlength':
        return `Usa al menos ${errors[key].requiredLength} caracteres.`;
      case 'maxlength':
        return `Usa como máximo ${errors[key].requiredLength} caracteres.`;
      case 'min':
        return `El valor mínimo es ${errors[key].min}.`;
      case 'max':
        return `El valor máximo es ${errors[key].max}.`;
      case 'step':
        return errors[key].step === 1
          ? 'Ingresa un número entero.'
          : `Usa incrementos de ${errors[key].step}.`;
      case 'pattern': {
        const pattern = String(errors[key].requiredPattern);
        if (pattern.includes('ucb'))
          return 'Usa un correo institucional @ucb.edu.bo.';
        if (pattern.includes('{8,10}')) return 'Ingresa entre 8 y 10 dígitos.';
        if (pattern === '^[0-9]+$') return 'Usa solo números.';
        return 'Revisa el formato de este campo.';
      }
      default:
        return 'Revisa el valor de este campo.';
    }
  }
}
