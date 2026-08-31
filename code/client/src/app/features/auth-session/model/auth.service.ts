import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Usuario } from '@entities/user';
import { environment } from '@environments/environment';
import { decodeBase64JsonResult, parseJsonResult } from '@shared/lib/result';
import {
  BrowserSessionStorageService,
  SESSION_STORAGE_KEYS,
} from '@shared/lib/session';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { AccessTokenPayload } from './access-token-payload';
import { RefreshTokenResponse } from './refresh-token-response';

const MILLISECONDS_PER_SECOND = 1000;
const ROLE_CLAIM =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

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
  }

  setSession(
    accessToken: string,
    refreshToken: string,
    usuario: Usuario,
  ): void {
    this.sessionStorage.setItem(SESSION_STORAGE_KEYS.accessToken, accessToken);
    this.sessionStorage.setItem(
      SESSION_STORAGE_KEYS.refreshToken,
      refreshToken,
    );
    this.sessionStorage.setItem(
      SESSION_STORAGE_KEYS.user,
      JSON.stringify(usuario),
    );
  }

  getAccessToken(): string | null {
    return this.sessionStorage.getItem(SESSION_STORAGE_KEYS.accessToken);
  }

  getRefreshToken(): string | null {
    return this.sessionStorage.getItem(SESSION_STORAGE_KEYS.refreshToken);
  }

  getStoredUser(): Usuario | null {
    const storedUser = this.sessionStorage.getItem(SESSION_STORAGE_KEYS.user);

    return storedUser
      ? parseJsonResult<Usuario>(storedUser).unwrapOr(null)
      : null;
  }

  isLoggedIn(): boolean {
    const tokenPayload = this.getTokenPayload();
    const expirationTime = tokenPayload?.exp
      ? tokenPayload.exp * MILLISECONDS_PER_SECOND
      : 0;

    return expirationTime > Date.now();
  }

  getRole(): string | null {
    const tokenPayload = this.getTokenPayload();

    return tokenPayload?.role ?? tokenPayload?.[ROLE_CLAIM] ?? null;
  }

  private getTokenPayload(): AccessTokenPayload | null {
    const accessToken = this.getAccessToken();

    if (!accessToken) return null;

    const encodedPayload = accessToken.split('.')[1];

    if (!encodedPayload) return null;

    const normalizedPayload = encodedPayload
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    return decodeBase64JsonResult<AccessTokenPayload>(
      normalizedPayload,
    ).unwrapOr(null);
  }

  isAdmin(): boolean {
    return ['administrador', 'administrador_laboratorio'].includes(
      this.getRole() ?? '',
    );
  }

  refreshTokens(
    refreshToken: string,
  ): Observable<{ accessToken: string; refreshToken: string }> {
    return this.http
      .post<RefreshTokenResponse>(`${environment.apiUrl}/api/auth/refresh`, {
        RefreshToken: refreshToken,
      })
      .pipe(
        map((rawResponse) => ({
          accessToken: rawResponse.Value.AccessToken,
          refreshToken: rawResponse.Value.RefreshToken,
        })),
        tap((newTokens) => {
          this.sessionStorage.setItem(
            SESSION_STORAGE_KEYS.accessToken,
            newTokens.accessToken,
          );
          this.sessionStorage.setItem(
            SESSION_STORAGE_KEYS.refreshToken,
            newTokens.refreshToken,
          );
        }),
      );
  }

  clear(): void {
    this.sessionStorage.removeItem(SESSION_STORAGE_KEYS.accessToken);
    this.sessionStorage.removeItem(SESSION_STORAGE_KEYS.refreshToken);
    this.sessionStorage.removeItem(SESSION_STORAGE_KEYS.user);
  }
}
