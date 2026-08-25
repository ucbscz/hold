import { ConfiguracionService } from '@app/entities/configuracion/api/configuracion.service';
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
import { Carrito } from '@entities/cart';
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
  CustomSelectComponent,
  OpcionSelect,
} from '@shared/ui';
import { finalize } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { CarreraService } from '@entities/career';
import { FormsModule } from '@angular/forms';

const CONFLICT_STATUS = 409;
const UNPROCESSABLE_ENTITY_STATUS = 422;
const SERVER_ERROR_STATUS = 500;

@Component({
  selector: 'app-formulario',
  standalone: true,
  imports: [
    FirmaComponent,
    CommonModule,
    FormsModule,
    MostrarerrorComponent,
    PantallaCargaComponent,
    Aviso,
    AvisoExitoComponent,
    CustomSelectComponent,
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

  destinoPrestamo: string = 'Casa';
  carrerasOpciones: OpcionSelect[] = [];
  idCarrera: number | null = null;
  nombreMateria: string = '';

  constructor(
    private readonly http: HttpClient,
    private readonly renderer: Renderer2,
    private readonly carrito: CarritoService,
    private readonly usuario: UsuarioService,
    private readonly mandarprestamo: PrestamosAPIService,
    private readonly loanReturnNavigation: LoanReturnNavigationService,
    private readonly route: ActivatedRoute,
    private readonly carreraService: CarreraService,
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
      const nombreCarrera =
        this.carrerasOpciones.find(
          (c) => c.value === this.idCarrera?.toString(),
        )?.label || '[Carrera no seleccionada]';
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
      nombre_jefe_carrera: escapeHtmlValue(
        config?.NombreJefeCarrera ?? 'Job Angel Ledezma Dr.Ing',
      ),
      firma_jefe_carrera: firmaSrc,

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
      detalles_clase: detallesClase,
    });
    this.contenidoHtml = processedTemplate;
  }

  ngOnInit(): void {
    this.carreraService.obtenerCarreras().subscribe((carreras) => {
      this.carrerasOpciones = carreras.map((c) => ({
        value: c.Id.toString(),
        label: c.Nombre ?? 'Desconocido',
      }));
      this.actualizarContrato();
    });

    this.route.queryParams.subscribe((params) => {
      if (params['destino']) {
        this.destinoPrestamo = params['destino'];
        this.actualizarContrato();
      }
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
          <td class="imt-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
          <td class="ucb-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
          <td>
          <strong>${escapeHtmlValue(item.nombre ?? '')}</strong>
          <p>Marca: ${escapeHtmlValue(item.marca ?? '')} </p>
          <p>Modelo: ${escapeHtmlValue(item.modelo ?? '')} </p>
          </td>
          <td class="serial-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
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
          <td class="imt-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
          <td class="ucb-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
          <td>
          <strong>${escapeHtmlValue(item.nombre ?? '')}</strong>
          <p>Marca: ${escapeHtmlValue(item.marca ?? '')} </p>
          <p>Modelo: ${escapeHtmlValue(item.modelo ?? '')} </p>
          </td>
          <td class="serial-code" data-grupo-id="${escapeHtmlValue(key)}">Pendiente de asignación</td>
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
