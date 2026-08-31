import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  forwardRef,
  HostBinding,
  Input,
  Output,
  EventEmitter,
  NgZone,
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
  changeDetection: ChangeDetectionStrategy.OnPush,
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
  @Input() remoteSearch = false;
  @Output() searchChange = new EventEmitter<string>();
  @Input() menuPosition: 'auto' | 'top' | 'bottom' = 'auto';
  @Input() set opciones(valor: Array<OpcionSelect | string>) {
    const opciones = this.normalizarOpciones(valor ?? []);

    if (this.sonLasMismasOpciones(opciones)) return;

    this.opcionesNormalizadas = opciones;
    this.actualizarOpcionesFiltradas();
    if (this.abierto) this.programarPosicionMenu();
    this.changeDetector.markForCheck();
  }

  opcionesNormalizadas: OpcionSelect[] = [];
  opcionesFiltradas: OpcionSelect[] = [];
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
  private listenersActivos = false;
  private destruido = false;
  private readonly onDocumentClick = (evento: Event) => {
    if (
      evento.target instanceof Node &&
      !this.elementRef.nativeElement.contains(evento.target) &&
      !this.menuRef.nativeElement.contains(evento.target)
    ) {
      this.ngZone.run(() => this.cerrar());
    }
  };
  private readonly onDocumentKeydown = (evento: KeyboardEvent) => {
    if (evento.key === 'Escape') this.ngZone.run(() => this.cerrar());
  };
  private readonly onViewportScroll = (evento: Event) => {
    const menu = this.menuRef?.nativeElement;
    if (menu && evento.target instanceof Node && menu.contains(evento.target)) {
      return;
    }
    this.programarPosicionMenu(true);
  };
  private readonly onViewportResize = () => this.programarPosicionMenu(true);

  constructor(
    private readonly elementRef: ElementRef<HTMLElement>,
    private readonly changeDetector: ChangeDetectorRef,
    private readonly ngZone: NgZone,
  ) {}

  ngAfterViewInit(): void {
    const container =
      this.elementRef.nativeElement.closest('dialog') ?? document.body;
    container.appendChild(this.menuRef.nativeElement);
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

  alternar(): void {
    if (this.disabled) return;
    if (this.abierto) {
      this.cerrar();
      return;
    }

    this.abierto = true;
    this.onTouched();
    this.menuPosicionado = false;
    this.enfocarBusquedaAlPosicionar = true;
    if (this.remoteSearch) this.searchChange.emit('');
    this.activarListenersGlobales();
    this.programarPosicionMenu();
  }

  seleccionar(opcion: OpcionSelect): void {
    this.valor = opcion.value;
    this.onChange(this.valor);
    this.cerrar();
  }

  buscar(evento: Event): void {
    this.busqueda = (evento.target as HTMLInputElement).value;
    this.searchChange.emit(this.busqueda);
    this.actualizarOpcionesFiltradas();
    this.programarPosicionMenu();
  }

  writeValue(valor: unknown): void {
    this.valor = valor;
    this.changeDetector.markForCheck();
  }
  registerOnChange(fn: (valor: unknown) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(disabled: boolean): void {
    this.disabled = disabled;
    this.changeDetector.markForCheck();
  }

  ngOnDestroy(): void {
    this.destruido = true;
    this.desactivarListenersGlobales();
    if (this.animationFrameId !== undefined)
      cancelAnimationFrame(this.animationFrameId);
    this.menuRef.nativeElement.remove();
  }

  private programarPosicionMenu(conservarAltura = false): void {
    if (!this.abierto) return;
    if (this.animationFrameId !== undefined)
      cancelAnimationFrame(this.animationFrameId);

    this.ngZone.runOutsideAngular(() => {
      this.animationFrameId = requestAnimationFrame(() => {
        this.animationFrameId = undefined;
        this.posicionarMenu(conservarAltura);
      });
    });
  }

  private posicionarMenu(conservarAltura = false): void {
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
    const desiredHeight =
      conservarAltura && this.menuPosicionado
        ? this.menuMaxHeight
        : Math.min(
            this.calcularAlturaNaturalMenu(),
            viewportHeight - viewportPadding * 2,
          );

    this.abreHaciaArriba =
      this.menuPosition === 'top' ||
      (this.menuPosition === 'auto' &&
        spaceBelow < desiredHeight &&
        spaceAbove > spaceBelow);

    const availableSpace = this.abreHaciaArriba ? spaceAbove : spaceBelow;
    this.menuMaxHeight = this.normalizarMedida(
      Math.max(80, Math.min(desiredHeight, availableSpace)),
      80,
    );
    this.menuWidth = this.normalizarMedida(
      Math.min(triggerRect.width, viewportWidth - viewportPadding * 2),
    );
    this.menuLeft = this.normalizarMedida(
      Math.min(
        Math.max(viewportPadding, triggerRect.left),
        viewportWidth - this.menuWidth - viewportPadding,
      ),
      viewportPadding,
    );
    this.menuTop = this.normalizarMedida(
      this.abreHaciaArriba
        ? Math.max(
            viewportPadding,
            triggerRect.top - menuGap - this.menuMaxHeight,
          )
        : triggerRect.bottom + menuGap,
      viewportPadding,
    );
    this.menuPosicionado = true;
    if (!this.destruido) this.changeDetector.detectChanges();

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
    this.actualizarOpcionesFiltradas();
  }

  private cerrar(): void {
    if (!this.abierto) return;
    this.abierto = false;
    this.enfocarBusquedaAlPosicionar = false;
    this.limpiarBusqueda();
    this.desactivarListenersGlobales();
    this.changeDetector.markForCheck();
  }

  private activarListenersGlobales(): void {
    if (this.listenersActivos) return;
    this.listenersActivos = true;
    this.ngZone.runOutsideAngular(() => {
      document.addEventListener('click', this.onDocumentClick, true);
      document.addEventListener('keydown', this.onDocumentKeydown, true);
      document.addEventListener('scroll', this.onViewportScroll, true);
      window.addEventListener('resize', this.onViewportResize);
    });
  }

  private desactivarListenersGlobales(): void {
    if (!this.listenersActivos) return;
    this.listenersActivos = false;
    document.removeEventListener('click', this.onDocumentClick, true);
    document.removeEventListener('keydown', this.onDocumentKeydown, true);
    document.removeEventListener('scroll', this.onViewportScroll, true);
    window.removeEventListener('resize', this.onViewportResize);
  }

  private actualizarOpcionesFiltradas(): void {
    const busquedaNormalizada = this.normalizarTexto(this.busqueda);
    this.opcionesFiltradas =
      busquedaNormalizada && !this.remoteSearch
        ? this.opcionesNormalizadas.filter((opcion) =>
            this.normalizarTexto(opcion.label).includes(busquedaNormalizada),
          )
        : this.opcionesNormalizadas;
  }

  private normalizarOpciones(
    valor: Array<OpcionSelect | string>,
  ): OpcionSelect[] {
    const unicas = new Map<unknown, OpcionSelect>();
    for (const opcion of valor) {
      const normalizada =
        typeof opcion === 'string' ? { value: opcion, label: opcion } : opcion;
      if (!unicas.has(normalizada.value)) {
        unicas.set(normalizada.value, normalizada);
      }
    }
    return [...unicas.values()];
  }

  private sonLasMismasOpciones(opciones: OpcionSelect[]): boolean {
    return (
      opciones.length === this.opcionesNormalizadas.length &&
      opciones.every((opcion, indice) => {
        const actual = this.opcionesNormalizadas[indice];
        return opcion.value === actual.value && opcion.label === actual.label;
      })
    );
  }

  private calcularAlturaNaturalMenu(): number {
    const optionCount = Math.max(1, this.opcionesFiltradas.length);
    const visibleRows = Math.min(optionCount, this.debeMostrarBusqueda ? 5 : 6);
    const searchHeight = this.debeMostrarBusqueda ? 53 : 0;
    const optionsPadding = 12;
    const optionHeight = 42;

    return Math.min(
      300,
      searchHeight + optionsPadding + visibleRows * optionHeight,
    );
  }

  private normalizarTexto(texto: unknown): string {
    return String(texto ?? '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }

  private normalizarMedida(valor: number, fallback = 0): number {
    return Number.isFinite(valor) ? Math.max(0, valor) : fallback;
  }
}
