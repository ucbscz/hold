import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  forwardRef,
  HostBinding,
  HostListener,
  Input,
  OnDestroy,
  ViewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { OpcionSelect } from './opcion-select';

@Component({
  selector: 'app-custom-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-select.component.html',
  styleUrl: './custom-select.component.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CustomSelectComponent),
      multi: true,
    },
  ],
})
export class CustomSelectComponent
  implements ControlValueAccessor, AfterViewInit, OnDestroy
{
  @ViewChild('menu', { static: true })
  private menuRef!: ElementRef<HTMLElement>;

  @Input() placeholder = 'Seleccionar';
  @Input() icon = '';
  @Input() invalid = false;
  @Input() menuAnimation = true;
  @Input() searchThreshold = 6;
  @Input() menuPosition: 'auto' | 'top' | 'bottom' = 'auto';
  @Input() set opciones(valor: Array<OpcionSelect | string>) {
    this.opcionesNormalizadas = (valor ?? []).map((o) =>
      typeof o === 'string' ? { value: o, label: o } : o,
    );
    if (this.abierto) this.programarPosicionMenu();
  }

  opcionesNormalizadas: OpcionSelect[] = [];
  abierto = false;
  disabled = false;
  valor: unknown = null;
  busqueda = '';
  menuLeft = 0;
  menuTop = 0;
  menuWidth = 0;
  menuMaxHeight = 300;
  menuPosicionado = false;
  abreHaciaArriba = false;

  private onChange: (valor: unknown) => void = () => {};
  private onTouched: () => void = () => {};
  private animationFrameId?: number;
  private enfocarBusquedaAlPosicionar = false;
  private readonly onViewportScroll = (evento: Event) => {
    const menu = this.menuRef?.nativeElement;
    if (menu && evento.target instanceof Node && menu.contains(evento.target)) {
      return;
    }
    this.programarPosicionMenu();
  };

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {
    document.addEventListener('scroll', this.onViewportScroll, true);
  }

  ngAfterViewInit(): void {
    document.body.appendChild(this.menuRef.nativeElement);
  }

  @HostBinding('class.cs-open')
  get estaAbierto(): boolean {
    return this.abierto;
  }

  get etiquetaActual(): string {
    const opcion = this.opcionesNormalizadas.find(
      (o) => o.value === this.valor,
    );
    return opcion ? opcion.label : this.placeholder;
  }

  get tienePlaceholder(): boolean {
    return !this.opcionesNormalizadas.some((o) => o.value === this.valor);
  }

  get debeMostrarBusqueda(): boolean {
    return this.opcionesNormalizadas.length >= this.searchThreshold;
  }

  get opcionesFiltradas(): OpcionSelect[] {
    const busquedaNormalizada = this.normalizarTexto(this.busqueda);

    if (!busquedaNormalizada) return this.opcionesNormalizadas;

    return this.opcionesNormalizadas.filter((opcion) =>
      this.normalizarTexto(opcion.label).includes(busquedaNormalizada),
    );
  }

  alternar(): void {
    if (this.disabled) return;
    this.abierto = !this.abierto;
    if (this.abierto) {
      this.onTouched();
      this.menuPosicionado = false;
      this.enfocarBusquedaAlPosicionar = true;
      this.programarPosicionMenu();
    } else {
      this.limpiarBusqueda();
    }
  }

  seleccionar(opcion: OpcionSelect): void {
    this.valor = opcion.value;
    this.onChange(this.valor);
    this.abierto = false;
    this.enfocarBusquedaAlPosicionar = false;
    this.limpiarBusqueda();
  }

  buscar(evento: Event): void {
    this.busqueda = (evento.target as HTMLInputElement).value;
  }

  @HostListener('document:click', ['$event'])
  onClickFuera(evento: Event): void {
    if (
      this.abierto &&
      evento.target instanceof Node &&
      !this.elementRef.nativeElement.contains(evento.target) &&
      !this.menuRef.nativeElement.contains(evento.target)
    ) {
      this.abierto = false;
      this.enfocarBusquedaAlPosicionar = false;
      this.limpiarBusqueda();
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.abierto) return;
    this.abierto = false;
    this.enfocarBusquedaAlPosicionar = false;
    this.limpiarBusqueda();
  }

  @HostListener('window:resize')
  onResize(): void {
    this.programarPosicionMenu();
  }

  writeValue(valor: unknown): void {
    this.valor = valor;
  }
  registerOnChange(fn: (valor: unknown) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(disabled: boolean): void {
    this.disabled = disabled;
  }

  ngOnDestroy(): void {
    document.removeEventListener('scroll', this.onViewportScroll, true);
    if (this.animationFrameId !== undefined)
      cancelAnimationFrame(this.animationFrameId);
    this.menuRef.nativeElement.remove();
  }

  private programarPosicionMenu(): void {
    if (!this.abierto) return;
    if (this.animationFrameId !== undefined)
      cancelAnimationFrame(this.animationFrameId);

    this.animationFrameId = requestAnimationFrame(() => {
      this.animationFrameId = undefined;
      this.posicionarMenu();
    });
  }

  private posicionarMenu(): void {
    const trigger =
      this.elementRef.nativeElement.querySelector<HTMLElement>('.cs-trigger');
    const menu = this.menuRef.nativeElement;
    if (!trigger || !menu) return;

    const viewportPadding = 8;
    const menuGap = 4;
    const triggerRect = trigger.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;
    const spaceAbove = Math.max(0, triggerRect.top - viewportPadding - menuGap);
    const spaceBelow = Math.max(
      0,
      viewportHeight - triggerRect.bottom - viewportPadding - menuGap,
    );
    const desiredHeight = Math.min(
      menu.scrollHeight || 300,
      300,
      viewportHeight - viewportPadding * 2,
    );

    this.abreHaciaArriba =
      this.menuPosition === 'top' ||
      (this.menuPosition === 'auto' &&
        spaceBelow < desiredHeight &&
        spaceAbove > spaceBelow);

    const availableSpace = this.abreHaciaArriba ? spaceAbove : spaceBelow;
    this.menuMaxHeight = Math.max(80, Math.min(desiredHeight, availableSpace));
    this.menuWidth = Math.min(
      triggerRect.width,
      viewportWidth - viewportPadding * 2,
    );
    this.menuLeft = Math.min(
      Math.max(viewportPadding, triggerRect.left),
      viewportWidth - this.menuWidth - viewportPadding,
    );
    this.menuTop = this.abreHaciaArriba
      ? Math.max(
          viewportPadding,
          triggerRect.top - menuGap - this.menuMaxHeight,
        )
      : triggerRect.bottom + menuGap;
    this.menuPosicionado = true;

    if (this.debeMostrarBusqueda && this.enfocarBusquedaAlPosicionar) {
      this.enfocarBusquedaAlPosicionar = false;
      requestAnimationFrame(() => {
        menu
          .querySelector<HTMLInputElement>('.cs-search__input')
          ?.focus({ preventScroll: true });
      });
    }
  }

  private limpiarBusqueda(): void {
    this.busqueda = '';
    this.menuPosicionado = false;
  }

  private normalizarTexto(texto: unknown): string {
    return String(texto ?? '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }
}
