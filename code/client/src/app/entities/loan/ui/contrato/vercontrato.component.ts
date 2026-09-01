import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  inject,
  OnDestroy,
  signal,
  ViewChild,
  WritableSignal,
} from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { PrestamosAPIService } from '../../api/prestamos-api.service';
import { BaseTablaComponent } from '@shared/lib/admin-table';
import { extractErrorMessage } from '@shared/lib/error';
import { MostrarerrorComponent } from '@shared/ui';

const PRINT_READY_TIMEOUT_MS = 15_000;
const PRINT_CLEANUP_TIMEOUT_MS = 300_000;
const DOWNLOAD_CLEANUP_DELAY_MS = 1_000;

@Component({
  selector: 'app-vercontrato',
  imports: [CommonModule, MostrarerrorComponent],
  templateUrl: './vercontrato.component.html',
  styleUrl: './vercontrato.component.css',
})
export class VercontratoComponent
  extends BaseTablaComponent
  implements AfterViewInit, OnDestroy
{
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  @Input() vercontraro: WritableSignal<boolean> = signal(true);
  @Input() idprestamo: number = 0;
  @ViewChild('contractContent') contractContentRef?: ElementRef<HTMLElement>;
  contratoContent: string = '';
  contratoContentSeguro: SafeHtml | null = null;
  tieneCarnet = false;
  imprimiendo = false;
  private cancelarImpresion?: () => void;
  private readonly descargas = new Map<string, number>();
  constructor(
    private readonly prestamo: PrestamosAPIService,
    private readonly sanitizer: DomSanitizer,
  ) {
    super();
  }
  ngOnInit() {
    this.cargarcontrato();
  }
  ngAfterViewInit(): void {
    document.body.appendChild(this.host.nativeElement);
  }
  cargarcontrato() {
    this.prestamo.obtenercontratoPrestamo(this.idprestamo).subscribe({
      next: (data) => {
        if (!data || typeof data !== 'string' || data.trim() === '') {
          this.mensajeerror = 'El contrato no está disponible.';
          this.error.set(true);
          return;
        }
        this.contratoContent = this.normalizarContratoHtml(data);
        this.tieneCarnet = this.obtenerImagenesCarnet().length === 2;
        this.contratoContentSeguro = this.sanitizer.bypassSecurityTrustHtml(
          this.contratoContent,
        );
      },
      error: (error) => {
        const errorMsg = extractErrorMessage(
          error,
          'No se pudo cargar el contrato del prestamo.',
        );
        this.mensajeerror = errorMsg;
        this.error.set(true);
      },
    });
  }

  descargarContrato() {
    if (!this.contratoContent) return;

    let url: string | undefined;
    const enlace = document.createElement('a');
    try {
      const archivo = new Blob([this.crearDocumento(this.contratoContent)], {
        type: 'text/html;charset=utf-8',
      });
      url = URL.createObjectURL(archivo);
      enlace.href = url;
      enlace.download = `contrato-${this.idprestamo}.html`;
      document.body.appendChild(enlace);
      enlace.click();
    } catch {
      this.mostrarError(
        'No se pudo descargar el contrato. Intenta nuevamente.',
      );
    } finally {
      enlace.remove();
      if (url) {
        const descargaUrl = url;
        this.descargas.set(
          descargaUrl,
          window.setTimeout(() => {
            URL.revokeObjectURL(descargaUrl);
            this.descargas.delete(descargaUrl);
          }, DOWNLOAD_CLEANUP_DELAY_MS),
        );
      }
    }
  }

  imprimirContrato(): void {
    if (this.contratoContent) this.imprimir(this.contratoContent);
  }

  imprimirCarnet(): void {
    const imagenes = this.obtenerImagenesCarnet();
    if (imagenes.length !== 2) {
      this.mostrarError('El contrato no incluye ambas caras del carnet.');
      return;
    }

    const contenido = document.createElement('div');
    const titulo = document.createElement('h1');
    titulo.textContent = 'Carnet de identidad';
    contenido.appendChild(titulo);
    for (const imagen of imagenes) {
      const lado = imagen.dataset['carnet'];
      const seccion = document.createElement('div');
      seccion.className = 'carnet';
      const etiqueta = document.createElement('p');
      etiqueta.textContent = lado === 'frente' ? 'Frente' : 'Reverso';
      const copia = document.createElement('img');
      copia.src = imagen.getAttribute('src')!;
      copia.alt = `Carnet: ${etiqueta.textContent}`;
      copia.dataset['carnet'] = lado;
      seccion.append(etiqueta, copia);
      contenido.appendChild(seccion);
    }
    this.imprimir(contenido.innerHTML, 'Carnet de identidad');
  }

  private obtenerImagenesCarnet(): HTMLImageElement[] {
    const documento = new DOMParser().parseFromString(
      this.contratoContent,
      'text/html',
    );
    return ['frente', 'atras'].flatMap((lado) => {
      const imagen = documento.querySelector<HTMLImageElement>(
        `img[data-carnet="${lado}"]`,
      );
      const fuente = imagen?.getAttribute('src') ?? '';
      return imagen &&
        /^data:image\/(?:png|jpeg|gif|webp);base64,[A-Za-z0-9+/]+={0,2}$/i.test(
          fuente,
        )
        ? [imagen]
        : [];
    });
  }

  private imprimir(contenido: string, titulo = 'Contrato de Préstamo'): void {
    if (this.imprimiendo) return;
    this.imprimiendo = true;
    const iframe = document.createElement('iframe');
    iframe.title = titulo;
    iframe.setAttribute('aria-hidden', 'true');
    iframe.tabIndex = -1;
    Object.assign(iframe.style, {
      position: 'fixed',
      left: '-10000px',
      top: '0',
      width: '794px',
      height: '1123px',
      border: '0',
      opacity: '0',
      pointerEvents: 'none',
    });
    let temporizador: number;
    const limpiar = (): void => {
      window.clearTimeout(temporizador);
      iframe.onload = null;
      iframe.onerror = null;
      iframe.contentWindow?.removeEventListener('afterprint', limpiar);
      iframe.remove();
      this.imprimiendo = false;
      this.cancelarImpresion = undefined;
    };
    const fallar = (): void => {
      limpiar();
      this.mostrarError(
        'No se pudo preparar la impresión. Comprueba las imágenes y permite imprimir en el navegador.',
      );
    };
    this.cancelarImpresion = limpiar;
    temporizador = window.setTimeout(fallar, PRINT_READY_TIMEOUT_MS);
    iframe.onerror = fallar;
    iframe.onload = async () => {
      iframe.onload = null;
      try {
        const ventana = iframe.contentWindow;
        if (!ventana) throw new Error('Documento de impresión no disponible');
        await Promise.all(
          Array.from(ventana.document.images)
            .filter((imagen) => !!imagen.getAttribute('src'))
            .map((imagen) => imagen.decode()),
        );
        if (!iframe.isConnected) return;
        window.clearTimeout(temporizador);
        ventana.addEventListener('afterprint', limpiar, { once: true });
        temporizador = window.setTimeout(limpiar, PRINT_CLEANUP_TIMEOUT_MS);
        ventana.focus();
        ventana.print();
      } catch {
        if (iframe.isConnected) fallar();
      }
    };
    try {
      iframe.srcdoc = this.crearDocumento(contenido, titulo);
      document.body.appendChild(iframe);
    } catch {
      fallar();
    }
  }

  private mostrarError(mensaje: string): void {
    this.mensajeerror = mensaje;
    this.error.set(true);
  }

  ngOnDestroy(): void {
    this.cancelarImpresion?.();
    for (const [url, temporizador] of this.descargas) {
      window.clearTimeout(temporizador);
      URL.revokeObjectURL(url);
    }
    this.descargas.clear();
    this.host.nativeElement.remove();
  }

  private crearDocumento(
    contenido: string,
    titulo = 'Contrato de Préstamo',
  ): string {
    return `<!doctype html>
          <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>${titulo}</title>
              <style>
                body {
                  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                  padding: 20px;
                  color: #333;
                  line-height: 1.35;
                }
                h1 {
                  text-align: center;
                  font-size: 24px;
                  color: #2c3e50;
                  margin-bottom: 12px;
                  text-transform: uppercase;
                }
                p {
                  text-align: justify;
                  margin-bottom: 10px;
                  font-size: 15px;
                }
                strong {
                  font-size: 16px;
                  color: #2c3e50;
                }
                table {
                  width: 100%;
                  border-collapse: collapse;
                  margin: 20px 0;
                  font-size: 14px;
                }
                td p {
                  margin: 0;
                  padding: 0;
                  line-height: 1.1 !important;
                  font-size: 13px;
                }
                td strong {
                  display: block;
                  margin-bottom: 2px;
                  font-size: 14px;
                }
                td {
                  border: 1px solid #aaa;
                  padding: 4px 6px;
                  text-align: left;
                  vertical-align: top;
                }
                th {
                  background-color: #e0e0e0;
                  padding: 6px 8px;
                  border: 1px solid #aaa;
                }
                .signature {
                  margin-top: 20px;
                  display: flex;
                  justify-content: center;
                  align-items: flex-start;
                  gap: 40px;
                  width: 100%;
                }
                .signature > div {
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  text-align: center;
                  max-width: 300px;
                }
                .signature img {
                  width: 240px;
                  max-width: 100%;
                  height: 140px;
                  object-fit: contain;
                  border: 1px solid #ccc;
                  padding: 5px;
                  background-color: #fff;
                  margin-bottom: 5px;
                }
                .signature p {
                  margin: 0;
                  font-weight: bold;
                  text-align: center;
                }
                img { max-width: 100%; object-fit: contain; }
                .carnet { break-inside: avoid; margin: 24px 0; }
                img[data-carnet] {
                  display: block;
                  width: 85.6mm;
                  height: 54mm;
                  margin: 6px auto;
                  border: 1px solid #aaa;
                  border-radius: 4px;
                  break-inside: avoid;
                }
                .contract-identity {
                  margin-top: 18px;
                  padding-top: 12px;
                  border-top: 1px solid #aaa;
                  break-inside: avoid;
                }
                .contract-identity > strong {
                  display: block;
                  margin-bottom: 8px;
                  text-align: center;
                }
                .contract-identity table { margin: 0; table-layout: fixed; }
                .contract-identity td { border: 0; padding: 8px; }
                .contract-identity td + td { border-left: 8px solid #fff; }
                .contract-identity td p {
                  margin-bottom: 6px;
                  color: #475569;
                  font-weight: 700;
                  text-align: center;
                }
                @page { margin: 15mm; }
              </style>
            </head>
            <body>
              ${contenido}
            </body>
          </html>
        `;
  }
  cerrar() {
    this.cancelarImpresion?.();
    this.vercontraro.set(false);
  }

  private normalizarContratoHtml(html: string): string {
    const placeholder =
      'data:image/gif;base64,R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=';
    const placeholderSrc = `src="${placeholder}"`;

    const normalized = html
      .replace(/src=(["'])unsafe:(data:image\/[^"']+)\1/g, 'src=$1$2$1')
      .replace(/src=(["'])\[\[firmausuario\]\]\1/g, placeholderSrc)
      .replace(
        /(<img\b(?=[^>]*\bid=["']firmaUsuarioPlaceholder["'])[^>]*\bsrc=["'])(["'][^>]*>)/i,
        `$1${placeholder}$2`,
      );

    const document = new DOMParser().parseFromString(normalized, 'text/html');
    const section = document.querySelector('.contract-identity');
    const table = section?.querySelector('table');
    const images = ['frente', 'atras'].map((side) =>
      section?.querySelector<HTMLImageElement>(`img[data-carnet="${side}"]`),
    );
    const hasBothSides = images.every((image) => {
      const source = image?.getAttribute('src') ?? '';
      return /^data:image\/(?:png|jpeg|gif|webp);base64,[A-Za-z0-9+/]+={0,2}$/i.test(
        source,
      );
    });

    if (section && table && !hasBothSides) {
      const pending = document.createElement('p');
      pending.className = 'contract-identity-empty';
      pending.textContent = 'Carnet no adjunto.';
      table.replaceWith(pending);
    }

    return document.body.innerHTML;
  }
}
