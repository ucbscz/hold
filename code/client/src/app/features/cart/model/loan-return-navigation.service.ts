import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import {
  BrowserSessionStorageService,
  SESSION_STORAGE_KEYS,
} from '@shared/lib/session';

const DEFAULT_RETURN_URL = '/inicio';
const LOAN_FLOW_PATHS = new Set(['/carrito', '/reserva']);
const PUBLIC_ACCESS_PATHS = new Set(['/login', '/registro']);

@Injectable({ providedIn: 'root' })
export class LoanReturnNavigationService {
  constructor(
    private readonly sessionStorage: BrowserSessionStorageService,
    private readonly router: Router,
  ) {}

  track(url: string): void {
    if (!this.isValidReturnUrl(url)) return;

    this.sessionStorage.setItem(SESSION_STORAGE_KEYS.loanReturnUrl, url);
  }

  returnToPreviousPage(): Promise<boolean> {
    const storedUrl = this.sessionStorage.getItem(
      SESSION_STORAGE_KEYS.loanReturnUrl,
    );
    const returnUrl =
      storedUrl && this.isValidReturnUrl(storedUrl)
        ? storedUrl
        : DEFAULT_RETURN_URL;

    this.sessionStorage.removeItem(SESSION_STORAGE_KEYS.loanReturnUrl);

    return this.router.navigateByUrl(returnUrl);
  }

  private isValidReturnUrl(url: string): boolean {
    if (!url.startsWith('/') || url.startsWith('//')) return false;

    const path = url.split(/[?#]/, 1)[0].toLowerCase();

    return !LOAN_FLOW_PATHS.has(path) && !PUBLIC_ACCESS_PATHS.has(path);
  }
}
