import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class BrowserSessionStorageService {
  getItem(key: string): string | null {
    return sessionStorage.getItem(key);
  }

  setItem(key: string, value: string): void {
    sessionStorage.setItem(key, value);
  }

  removeItem(key: string): void {
    sessionStorage.removeItem(key);
  }
}
