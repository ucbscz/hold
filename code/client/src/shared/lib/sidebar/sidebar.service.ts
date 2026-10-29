import { Injectable, signal, WritableSignal } from '@angular/core';
@Injectable({
  providedIn: 'root',
})
export class SidebarService {
  isOpen: WritableSignal<boolean> = signal(
    typeof window !== 'undefined' && window.innerWidth > 900,
  );
  toggle() {
    this.isOpen.update((val) => !val);
  }
  close() {
    this.isOpen.set(false);
  }

  closeOnCompactViewport() {
    if (typeof window !== 'undefined' && window.innerWidth <= 900) this.close();
  }
}
