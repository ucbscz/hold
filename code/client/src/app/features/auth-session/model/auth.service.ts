import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Usuario, esRolGestion } from '@entities/user';
import { environment } from '@environments/environment';
import { parseJsonResult } from '@shared/lib/result';
import {
  BrowserSessionStorageService,
  SESSION_STORAGE_KEYS,
} from '@shared/lib/session';
import { Observable } from 'rxjs';
import { finalize, map } from 'rxjs/operators';

interface SessionResponse {
  Value: {
    Usuario: Usuario;
  };
}

const LEGACY_TOKEN_KEYS = ['ucbhold_access', 'ucbhold_refresh'];

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http: HttpClient;

  constructor(
    backend: HttpBackend,
    private readonly sessionStorage: BrowserSessionStorageService,
  ) {
    this.http = new HttpClient(backend);
    this.clearLegacyTokens();
  }

  getStoredUser(): Usuario | null {
    const storedUser = this.sessionStorage.getItem(SESSION_STORAGE_KEYS.user);

    return storedUser
      ? parseJsonResult<Usuario>(storedUser).unwrapOr(null)
      : null;
  }

  isLoggedIn(): boolean {
    return this.getStoredUser() !== null;
  }

  getRole(): string | null {
    return this.getStoredUser()?.rol ?? null;
  }

  isAdmin(): boolean {
    return esRolGestion(this.getRole());
  }

  refreshSession(): Observable<void> {
    return this.http
      .post<SessionResponse>(
        `${environment.apiUrl}/api/auth/refresh`,
        {},
        { withCredentials: true },
      )
      .pipe(map(() => undefined));
  }

  clear(): void {
    this.sessionStorage.removeItem(SESSION_STORAGE_KEYS.user);
    this.clearLegacyTokens();
  }

  logout(): Observable<void> {
    return this.http
      .post<void>(
        `${environment.apiUrl}/api/auth/logout`,
        {},
        { withCredentials: true },
      )
      .pipe(finalize(() => this.clear()));
  }

  private clearLegacyTokens(): void {
    LEGACY_TOKEN_KEYS.forEach((key) => this.sessionStorage.removeItem(key));
  }
}
