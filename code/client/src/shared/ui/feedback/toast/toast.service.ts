import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly visible = signal(false);
  readonly message = signal('');
  readonly kind = signal<'success' | 'error'>('success');

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string): void {
    this.show(message, 'error');
  }

  private show(message: string, kind: 'success' | 'error'): void {
    this.visible.set(false);
    this.message.set(message);
    this.kind.set(kind);
    queueMicrotask(() => this.visible.set(true));
  }
}
