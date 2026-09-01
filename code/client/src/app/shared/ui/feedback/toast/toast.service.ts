import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly visible = signal(false);
  readonly message = signal('');

  success(message: string): void {
    this.visible.set(false);
    this.message.set(message);
    queueMicrotask(() => this.visible.set(true));
  }
}
