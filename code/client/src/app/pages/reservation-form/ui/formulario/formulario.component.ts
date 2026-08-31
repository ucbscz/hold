import { HttpClient } from '@angular/common/http';
import {
  Component,
  ElementRef,
  OnInit,
  Renderer2,
  ViewChild,
  WritableSignal,
  signal,
} from '@angular/core';
import { Carrito } from '@entities/cart';
import { ConfiguracionService } from '@entities/configuracion';
import { PrestamosAPIService } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { CarritoService, LoanReturnNavigationService } from '@features/cart';
import { FirmaComponent } from '@features/signature';
import { extractErrorMessage } from '@shared/lib/error';
import { escapeHtmlValue } from '@shared/lib/html';
import {
  Aviso,
  AvisoExitoComponent,
  MostrarerrorComponent,
  PantallaCargaComponent,
} from '@shared/ui';
import { finalize } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';

const CONFLICT_STATUS = 409;
const UNPROCESSABLE_ENTITY_STATUS = 422;
const SERVER_ERROR_STATUS = 500;

@Component({
  selector: 'app-formulario',
  standalone: true,
  imports: [
    FirmaComponent,
    MostrarerrorComponent,
    PantallaCargaComponent,
    Aviso,
    AvisoExitoComponent,
  ],
  templateUrl: './formulario.component.html',
  styleUrl: './formulario.component.css',
})
export class FormularioComponent implements OnInit {
  @ViewChild('contratoContainer', { static: false })
  contratoContainer!: ElementRef;

  contenidoHtml: string = '';
  clickfirma: WritableSignal<boolean> = signal(false);
  firma: string = '';
  carnetFrente = '';
  carnetAtras = '';
  procesandoCarnet = false;

  async cargarCarnet(event: Event, cara: 'frente' | 'atras'): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (
      !['image/jpeg', 'image/png', 'image/webp'].includes(file.type) ||
      file.size > 5 * 1024 * 1024
    ) {
      this.mensajeerror =
        'Selecciona una imagen JPG, PNG o WebP de hasta 5 MB.';
      this.error.set(true);
      input.value = '';
      return;
    }
    this.procesandoCarnet = true;
    try {
      const bitmap = await createImageBitmap(file);
      const scale = Math.min(1, 1200 / Math.max(bitmap.width, bitmap.height));
      const canvas = document.createElement('canvas');
      canvas.width = Math.round(bitmap.width * scale);
      canvas.height = Math.round(bitmap.height * scale);
      const ctx = canvas.getContext('2d');
      if (!ctx) throw new Error('Imagen no disponible');
      ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
      bitmap.close();
      const data = canvas.toDataURL('image/jpeg', 0.8);
      if (data.length > 650000) throw new Error('Imagen demasiado grande');
      if (cara === 'frente') this.carnetFrente = data;
      else this.carnetAtras = data;
      this.actualizarContrato();
      if (this.firma)
        this.contenidoHtml = this.insertarFirmaEnContrato(
          this.contenidoHtml,
          this.firma,
        );
    } catch {
      this.mensajeerror =
        'No se pudo leer la imagen. Prueba con una fotografía más pequeña.';
      this.error.set(true);
    } finally {
      this.procesandoCarnet = false;
    }
  }
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = 'Error desconocido intente mas tarde';
  cargando: boolean = false;
  aviso: WritableSignal<boolean> = signal(false);
  mensajeaviso: string =
    'Aviso desconocido , si ve esto es un error , avise al soporte si puede o intente mas tarde';
  avisoexito: WritableSignal<boolean> = signal(false);
  mensajeexito: string =
    'Aviso de exito desconocido , si ve esto es un error , avise al soporte si puede o intente mas tarde';

  destinoPrestamo: string = 'Casa';
  idCarrera: number | null = null;
  nombreCarrera: string = '';
  nombreMateria: string = '';

  constructor(
    private readonly http: HttpClient,
    private readonly renderer: Renderer2,
    private readonly carrito: CarritoService,
    private readonly usuario: UsuarioService,
    private readonly mandarprestamo: PrestamosAPIService,
    private readonly loanReturnNavigation: LoanReturnNavigationService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly configuracionService: ConfiguracionService,
  ) {}

  templateCrudo: string = '';

  actualizarContrato() {
    if (!this.templateCrudo) return;

    const fechaInicioReserva = this.carrito.obtenerFechaInicio();
    const fechaFinalReserva = this.carrito.obtenerFechaFinal();

    if (!fechaInicioReserva || !fechaFinalReserva) return;

    const fechaInicio = new Date(fechaInicioReserva);
    const fechaFinal = new Date(fechaFinalReserva);
    const duracionMinutos = Math.round(
      (fechaFinal.getTime() - fechaInicio.getTime()) / 60_000,
    );

    let detallesClase = '';
    if (this.destinoPrestamo === 'Clase') {
      const nombreCarrera = this.nombreCarrera || '[Carrera no seleccionada]';
      const materia = this.nombreMateria || '[Materia no ingresada]';
      detallesClase = `, los cuales serán utilizados para la clase de ${materia} de la carrera de ${nombreCarrera}`;
    }

    const u = this.usuario.obtenerUsuario();
    const nombreUsuario = [u.nombre, u.apellido_paterno, u.apellido_materno]
      .filter(Boolean)
      .join(' ');

    const rol = u.rol
      ? u.rol.charAt(0).toUpperCase() + u.rol.slice(1).toLowerCase()
      : 'Estudiante';
    let rolCarrera = rol;
    if (u.carrera) {
      rolCarrera = `${rol} de ${u.carrera}`;
    }

    const config = this.configuracionService.configuracionActual();

    const base64Firma = config?.FirmaJefeCarreraBase64 ?? '';
    const firmaSrc = base64Firma.startsWith('data:')
      ? base64Firma
      : `data:image/png;base64,${base64Firma}`;

    const processedTemplate = this.reemplazarMarcadores(this.templateCrudo, {
      nombre_jefe_carrera: escapeHtmlValue(config?.NombreJefeCarrera ?? ''),
      firma_jefe_carrera: firmaSrc,
      carnet_frente: this.carnetFrente,
      carnet_atras: this.carnetAtras,

      dia: new Date().getDate().toString(),
      mesliteral: new Intl.DateTimeFormat('es-ES', {
        month: 'long',
      }).format(new Date()),
      año: new Date().getFullYear().toString(),
      usuario: escapeHtmlValue(nombreUsuario),
      rol_carrera: escapeHtmlValue(rolCarrera),
      usuario_ci: escapeHtmlValue(u.carnet ?? ''),
      tablaprimera: this.primeradelobjeto(this.carrito.obtenerCarrito()),
      fechaMaxima: this.formatearDuracion(duracionMinutos),
      precio: this.carrito.calcularPrecioTotal().toString(),
      tablasegunda: this.quintavalordebienes(this.carrito.obtenerCarrito()),
      dia_devolucion: fechaFinal.getDate().toString(),
      mes_devolucion: new Intl.DateTimeFormat('es-ES', {
        month: 'long',
      }).format(fechaFinal),
      año_devolucion: fechaFinal.getFullYear().toString(),
      detalles_clase: escapeHtmlValue(detallesClase),
    });
    this.contenidoHtml = processedTemplate;
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      this.destinoPrestamo = params['destino'] || 'Casa';
      this.idCarrera = params['idCarrera'] ? Number(params['idCarrera']) : null;
      this.nombreCarrera = params['nombreCarrera'] || '';
      this.nombreMateria = params['nombreMateria'] || '';
      this.actualizarContrato();
    });

    const fechaInicioReserva = this.carrito.obtenerFechaInicio();
    const fechaFinalReserva = this.carrito.obtenerFechaFinal();

    if (!fechaInicioReserva || !fechaFinalReserva) {
      this.mensajeerror =
        'Seleccione fechas de préstamo antes de generar el contrato.';
      this.error.set(true);
      return;
    }

    this.http
      .get('assets/contrato.html?v=' + new Date().getTime(), {
        responseType: 'text',
      })
      .subscribe({
        next: (data: string) => {
          this.templateCrudo = data;
          this.actualizarContrato();
        },
        error: (error) => {
          const errorMsg = extractErrorMessage(
            error,
            'Error al cargar el contrato, intente mas tarde',
          );
          this.mensajeerror = errorMsg;
          this.error.set(true);
        },
      });
  }

  private reemplazarMarcadores(
    template: string,
    valores: { [clave: string]: string },
  ): string {
    let resultado = template;
    for (const clave in valores) {
      const regex = new RegExp(`\\[\\[${clave}\\]\\]`, 'g');
      resultado = resultado.replace(regex, valores[clave]);
    }
    return resultado;
  }

  private formatearDuracion(minutos: number): string {
    const horas = Math.floor(minutos / 60);
    const minutosRestantes = minutos % 60;

    if (horas === 0) return `${minutos} minutos`;
    if (minutosRestantes === 0) return `${horas} hora${horas === 1 ? '' : 's'}`;

    return `${horas} hora${horas === 1 ? '' : 's'} y ${minutosRestantes} minutos`;
  }

  firmar() {
    this.clickfirma.set(true);
  }

  aceptar() {
    if (this.procesandoCarnet) return;
    if (!this.carnetFrente || !this.carnetAtras) {
      this.mensajeerror =
        'Adjunta el anverso y reverso del carnet antes de confirmar.';
      this.error.set(true);
      return;
    }
    if (
      !this.carrito ||
      Object.keys(this.carrito.obtenerCarrito()).length === 0
    ) {
      this.mensajeerror =
        'El carrito está vacío. Agregue elementos antes de continuar.';
      this.error.set(true);
    } else if (!this.firma || this.firma === '') {
      this.firmar();
    } else {
      this.aviso.set(true);
      this.mensajeaviso =
        '¿Está seguro de confirmar el préstamo con los términos y condiciones establecidos en el contrato?';
    }
  }

  confirmarprestamo() {
    const contratoTexto = this.generarHTMLTexto();
    this.cargando = true;
    this.mandarprestamo
      .crearPrestamo(
        this.carrito.obtenerCarrito(),
        this.usuario.obtenerUsuario().carnet!,
        contratoTexto,
        this.destinoPrestamo,
        this.idCarrera ? Number(this.idCarrera) : undefined,
        this.nombreMateria,
      )
      .pipe(finalize(() => (this.cargando = false)))
      .subscribe({
        next: (_response) => {
          this.mensajeexito = 'El préstamo ha sido creado exitosamente.';
          this.avisoexito.set(true);
          this.carrito.vaciarCarrito();
        },
        error: (error) => {
          this.error.set(true);
          if (error?.status === CONFLICT_STATUS) {
            this.mensajeerror =
              'Ya tienes una solicitud activa para este equipo. Espera a que finalice antes de hacer una nueva reserva.';
          } else if (error?.status === UNPROCESSABLE_ENTITY_STATUS) {
            this.mensajeerror =
              'Los datos de tu solicitud no son válidos. Revisa las fechas e intenta nuevamente.';
          } else if (error?.status === SERVER_ERROR_STATUS) {
            this.mensajeerror =
              'Ocurrió un error en el servidor. Por favor, inténtalo de nuevo más tarde.';
          } else {
            this.mensajeerror =
              'No se pudo procesar tu solicitud. Verifica tu conexión e inténtalo nuevamente.';
          }
        },
      });
  }

  redirigirAnterior(): void {
    void this.loanReturnNavigation.returnToPreviousPage();
  }

  volverAlDestino(): void {
    void this.router.navigate(['/destino'], {
      queryParams: {
        destino: this.destinoPrestamo,
        idCarrera: this.idCarrera,
        nombreCarrera: this.nombreCarrera || null,
        nombreMateria: this.nombreMateria || null,
      },
    });
  }

  guardarfirma(signatureData: string): void {
    this.firma = signatureData;
    this.contenidoHtml = this.insertarFirmaEnContrato(
      this.contenidoHtml,
      signatureData,
    );
    window.setTimeout(() => this.aplicarFirmaAlDom());
  }

  private primeradelobjeto(carrito: Carrito): string {
    return this.crearFilasEquipos(carrito, false);
  }

  private quintavalordebienes(carrito: Carrito): string {
    return this.crearFilasEquipos(carrito, true);
  }

  private crearFilasEquipos(carrito: Carrito, incluirPrecio: boolean): string {
    const items = Object.entries(carrito).filter(
      ([, item]) => typeof item === 'object' && 'nombre' in item,
    );
    return `
      ${items
        .map(
          ([key, item]) => `
        <tr>
          <td class="imt-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
          <td class="ucb-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
          <td>
          <strong>${escapeHtmlValue(item.nombre ?? '')}</strong>
          <p>Marca: ${escapeHtmlValue(item.marca ?? '')} </p>
          <p>Modelo: ${escapeHtmlValue(item.modelo ?? '')} </p>
          </td>
          <td class="serial-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
          <td>${item.cantidad}</td>
          ${
            incluirPrecio
              ? `<td>${item.precio}</td><td>${item.precio * item.cantidad}</td>`
              : ''
          }
        </tr>
      `,
        )
        .join('')}
    `;
  }

  generarHTMLTexto(): string {
    const contratoHtml = this.firma
      ? this.insertarFirmaEnContrato(this.contenidoHtml, this.firma)
      : this.contenidoHtml;
    return contratoHtml;
  }

  private aplicarFirmaAlDom(): void {
    if (!this.contratoContainer || !this.firma) {
      return;
    }
    const signatureImage: HTMLImageElement | null =
      this.contratoContainer.nativeElement.querySelector(
        '#firmaUsuarioPlaceholder',
      );
    if (signatureImage) {
      this.renderer.setAttribute(signatureImage, 'src', this.firma);
      this.renderer.setAttribute(
        signatureImage,
        'alt',
        'Firma del COMODATARIO',
      );
    }
  }

  private insertarFirmaEnContrato(html: string, firma: string): string {
    const firmaAtributo = firma.replace(/"/g, '&quot;');
    const htmlConSrcActualizado = html.replace(
      /(<img\b(?=[^>]*\bid=["']firmaUsuarioPlaceholder["'])[^>]*\bsrc=["'])[^"']*(["'][^>]*>)/i,
      `$1${firmaAtributo}$2`,
    );

    if (htmlConSrcActualizado !== html) {
      return htmlConSrcActualizado;
    }

    return html.replace(
      /(<img\b(?=[^>]*\bid=["']firmaUsuarioPlaceholder["'])[^>]*)(>)/i,
      `$1 src="${firmaAtributo}"$2`,
    );
  }
}
