import {
  AfterViewInit,
  Directive,
  ElementRef,
  NgZone,
  OnDestroy,
} from '@angular/core';

@Directive({
  selector: '[appStickyScroll]',
  standalone: true,
})
export class StickyScrollDirective implements AfterViewInit, OnDestroy {
  private rail?: HTMLDivElement;
  private spacer?: HTMLDivElement;
  private resizeObserver?: ResizeObserver;
  private mutationObserver?: MutationObserver;
  private animationFrame?: number;
  private syncing = false;

  constructor(
    private readonly element: ElementRef<HTMLElement>,
    private readonly ngZone: NgZone,
  ) {}

  ngAfterViewInit(): void {
    if (typeof document === 'undefined') return;

    this.ngZone.runOutsideAngular(() => {
      const host = this.element.nativeElement;
      host.classList.add('sticky-scroll-host');

      this.rail = document.createElement('div');
      this.rail.className = 'sticky-scroll-rail';
      this.rail.tabIndex = 0;
      this.rail.setAttribute('aria-label', 'Desplazar tabla horizontalmente');

      this.spacer = document.createElement('div');
      this.spacer.className = 'sticky-scroll-rail__spacer';
      this.rail.appendChild(this.spacer);
      document.body.appendChild(this.rail);

      host.addEventListener('scroll', this.syncFromHost, { passive: true });
      this.rail.addEventListener('scroll', this.syncFromRail, {
        passive: true,
      });
      window.addEventListener('scroll', this.scheduleUpdate, {
        passive: true,
      });
      window.addEventListener('resize', this.scheduleUpdate, {
        passive: true,
      });

      this.resizeObserver = new ResizeObserver(this.scheduleUpdate);
      this.resizeObserver.observe(host);
      if (host.firstElementChild instanceof HTMLElement) {
        this.resizeObserver.observe(host.firstElementChild);
      }

      this.mutationObserver = new MutationObserver(this.scheduleUpdate);
      this.mutationObserver.observe(host, { childList: true, subtree: true });
      this.scheduleUpdate();
    });
  }

  ngOnDestroy(): void {
    const host = this.element.nativeElement;
    host.removeEventListener('scroll', this.syncFromHost);
    window.removeEventListener('scroll', this.scheduleUpdate);
    window.removeEventListener('resize', this.scheduleUpdate);
    this.rail?.removeEventListener('scroll', this.syncFromRail);
    this.resizeObserver?.disconnect();
    this.mutationObserver?.disconnect();
    if (this.animationFrame !== undefined) {
      window.cancelAnimationFrame(this.animationFrame);
    }
    host.classList.remove('sticky-scroll-host', 'sticky-scroll-host--floating');
    this.rail?.remove();
  }

  private readonly syncFromHost = (): void => {
    if (!this.rail || this.syncing) return;
    this.syncing = true;
    this.rail.scrollLeft = this.element.nativeElement.scrollLeft;
    this.syncing = false;
  };

  private readonly syncFromRail = (): void => {
    if (!this.rail || this.syncing) return;
    this.syncing = true;
    this.element.nativeElement.scrollLeft = this.rail.scrollLeft;
    this.syncing = false;
  };

  private readonly scheduleUpdate = (): void => {
    if (this.animationFrame !== undefined) return;
    this.animationFrame = window.requestAnimationFrame(() => {
      this.animationFrame = undefined;
      this.update();
    });
  };

  private update(): void {
    if (!this.rail || !this.spacer) return;

    const host = this.element.nativeElement;
    const rect = host.getBoundingClientRect();
    const coarsePointer = window.matchMedia(
      '(max-width: 768px), (pointer: coarse)',
    ).matches;
    const overflows = host.scrollWidth > host.clientWidth + 2;
    const bottomOutsideViewport = rect.bottom > window.innerHeight;
    const visible = rect.top < window.innerHeight - 16 && rect.bottom > 0;
    const floating =
      !coarsePointer && overflows && visible && bottomOutsideViewport;

    host.classList.toggle('sticky-scroll-host--floating', floating);
    this.rail.hidden = !floating;

    if (!floating) return;

    const left = Math.max(0, rect.left);
    const right = Math.min(window.innerWidth, rect.right);
    this.rail.style.left = `${left}px`;
    this.rail.style.width = `${Math.max(0, right - left)}px`;
    this.spacer.style.width = `${host.scrollWidth}px`;
    this.rail.scrollLeft = host.scrollLeft;
  }
}
