import { CommonModule } from '@angular/common';
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
import { Router } from '@angular/router';
import { Carrito } from '@entities/cart';
import { PrestamosAPIService } from '@entities/loan';
import { UsuarioService } from '@entities/user';
import { CarritoService } from '@features/cart';
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

const CONFLICT_STATUS = 409;
const UNPROCESSABLE_ENTITY_STATUS = 422;
const SERVER_ERROR_STATUS = 500;

@Component({
  selector: 'app-formulario',
  standalone: true,
  imports: [
    FirmaComponent,
    CommonModule,
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
  error: WritableSignal<boolean> = signal(false);
  mensajeerror: string = 'Error desconocido intente mas tarde';
  cargando: boolean = false;
  aviso: WritableSignal<boolean> = signal(false);
  mensajeaviso: string =
    'Aviso desconocido , si ve esto es un error , avise al soporte si puede o intente mas tarde';
  avisoexito: WritableSignal<boolean> = signal(false);
  mensajeexito: string =
    'Aviso de exito desconocido , si ve esto es un error , avise al soporte si puede o intente mas tarde';

  constructor(
    private readonly http: HttpClient,
    private readonly renderer: Renderer2,
    private readonly carrito: CarritoService,
    private readonly router: Router,
    private readonly usuario: UsuarioService,
    private readonly mandarprestamo: PrestamosAPIService,
  ) {}

  ngOnInit(): void {
    const fechaInicioReserva = this.carrito.obtenerFechaInicio();
    const fechaFinalReserva = this.carrito.obtenerFechaFinal();

    if (!fechaInicioReserva || !fechaFinalReserva) {
      this.mensajeerror =
        'Seleccione fechas de préstamo antes de generar el contrato.';
      this.error.set(true);
      return;
    }

    const fechaInicio = new Date(fechaInicioReserva);
    const fechaFinal = new Date(fechaFinalReserva);
    const duracionMinutos = Math.round(
      (fechaFinal.getTime() - fechaInicio.getTime()) / 60_000,
    );
    this.http.get('assets/contrato.html', { responseType: 'text' }).subscribe({
      next: (data: string) => {
        const processedTemplate = this.reemplazarMarcadores(data, {
          dia: new Date().getDate().toString(),
          mesliteral: new Intl.DateTimeFormat('es-ES', {
            month: 'long',
          }).format(new Date()),
          año: new Date().getFullYear().toString(),
          usuario: escapeHtmlValue(this.usuario.obtenerUsuario().nombre ?? ''),
          usuario_ci: escapeHtmlValue(
            this.usuario.obtenerUsuario().carnet ?? '',
          ),
          tablaprimera: this.primeradelobjeto(this.carrito.obtenerCarrito()),
          fechaMaxima: this.formatearDuracion(duracionMinutos),
          precio: this.carrito.calcularPrecioTotal().toString(),
          tablasegunda: this.quintavalordebienes(this.carrito.obtenerCarrito()),
          dia_devolucion: fechaFinal.getDate().toString(),
          mes_devolucion: new Intl.DateTimeFormat('es-ES', {
            month: 'long',
          }).format(fechaFinal),
          año_devolucion: fechaFinal.getFullYear().toString(),
        });
        this.contenidoHtml = processedTemplate;
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

  irhome() {
    this.router.navigate(['/inicio']);
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
    const items = Object.entries(carrito).filter(
      ([, item]) => typeof item === 'object' && 'nombre' in item,
    );
    return `
      ${items
        .map(
          ([key, item]) => `
        <tr>
          <td class="imt-code" data-grupo-id="${escapeHtmlValue(key)}">Por definirse</td>
          <td class="ucb-code" data-grupo-id="${escapeHtmlValue(key)}">Por definirse</td>
          <td>
          <strong>${escapeHtmlValue(item.nombre ?? '')}</strong>
          <p>Marca: ${escapeHtmlValue(item.marca ?? '')} </p>
          <p>Modelo: ${escapeHtmlValue(item.modelo ?? '')} </p>
          </td>
          <td class="serial-code" data-grupo-id="${escapeHtmlValue(key)}">Por definirse</td>
          <td>${item.cantidad}</td>
        </tr>
      `,
        )
        .join('')}
    `;
  }

  private quintavalordebienes(carrito: Carrito): string {
    const items = Object.entries(carrito).filter(
      ([, item]) => typeof item === 'object' && 'nombre' in item,
    );
    return `
      ${items
        .map(
          ([key, item]) => `
        <tr>
          <td class="imt-code" data-grupo-id="${escapeHtmlValue(key)}">Por definirse</td>
          <td class="ucb-code" data-grupo-id="${escapeHtmlValue(key)}">Por definirse</td>
          <td>
          <strong>${escapeHtmlValue(item.nombre ?? '')}</strong>
          <p>Marca: ${escapeHtmlValue(item.marca ?? '')} </p>
          <p>Modelo: ${escapeHtmlValue(item.modelo ?? '')} </p>
          </td>
          <td class="serial-code" data-grupo-id="${escapeHtmlValue(key)}">Por definirse</td>
          <td>${item.cantidad}</td>
          <td>${item.precio}</td>
          <td>${item.precio * item.cantidad}</td>
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
