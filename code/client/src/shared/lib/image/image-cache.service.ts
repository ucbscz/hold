import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ImageCacheService {
  private readonly blockedOrigins = new Set([
    'https://kingsunmachining.com',
    'https://www.maisondudrone.com',
  ]);
  private readonly loadedUrls = new Set<string>();
  private readonly failedUrls = new Set<string>();
  private readonly requestedUrls = new Set<string>();

  preload(urls: readonly (string | null | undefined)[]): void {
    for (const url of urls) this.preloadOne(url);
  }

  isLoaded(url: string | null | undefined): boolean {
    const normalizedUrl = this.normalizeUrl(url);

    return normalizedUrl ? this.loadedUrls.has(normalizedUrl) : false;
  }

  hasFailed(url: string | null | undefined): boolean {
    const normalizedUrl = this.normalizeUrl(url);

    return normalizedUrl ? this.failedUrls.has(normalizedUrl) : false;
  }

  canDisplay(url: string | null | undefined): boolean {
    const normalizedUrl = this.normalizeUrl(url);

    if (!normalizedUrl) return false;

    const absoluteUrlProtocol = /^[a-z][a-z0-9+.-]*:/i;

    if (!absoluteUrlProtocol.test(normalizedUrl)) return true;
    if (!/^https?:\/\//i.test(normalizedUrl)) return false;

    const origin = normalizedUrl.match(/^(https?:\/\/[^/]+)/i)?.[1];

    return origin ? !this.blockedOrigins.has(origin.toLowerCase()) : false;
  }

  markLoaded(url: string): void {
    const normalizedUrl = this.normalizeUrl(url);

    if (!normalizedUrl) return;

    this.loadedUrls.add(normalizedUrl);
    this.failedUrls.delete(normalizedUrl);
  }

  markFailed(url: string): void {
    const normalizedUrl = this.normalizeUrl(url);

    if (!normalizedUrl) return;

    this.failedUrls.add(normalizedUrl);
    this.loadedUrls.delete(normalizedUrl);
  }

  private preloadOne(url: string | null | undefined): void {
    const normalizedUrl = this.normalizeUrl(url);

    if (!normalizedUrl) return;
    if (!this.canDisplay(normalizedUrl)) return;
    if (this.loadedUrls.has(normalizedUrl)) return;
    if (this.failedUrls.has(normalizedUrl)) return;
    if (this.requestedUrls.has(normalizedUrl)) return;
    if (typeof Image === 'undefined') return;

    this.requestedUrls.add(normalizedUrl);

    const image = new Image();
    image.decoding = 'async';
    image.onload = () => this.markLoaded(normalizedUrl);
    image.onerror = () => this.markFailed(normalizedUrl);
    image.src = normalizedUrl;
  }

  private normalizeUrl(url: string | null | undefined): string | null {
    const normalizedUrl = url?.trim();

    return normalizedUrl ? normalizedUrl : null;
  }
}
