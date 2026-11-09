import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-texto-expandible',
  template: `
    @if (texto) {
      <button
        type="button"
        class="resumen"
        [attr.aria-label]="'Ver ' + etiqueta.toLowerCase() + ' completa'"
        (click)="dialogo.showModal()"
      >
        {{ texto }}
      </button>
    }
    <dialog #dialogo [attr.aria-label]="etiqueta">
      <header>
        <span>{{ etiqueta }}</span>
        <h2>{{ titulo }}</h2>
        <button
          type="button"
          class="modal-close-btn"
          aria-label="Cerrar"
          (click)="dialogo.close()"
        >
          <i class="fas fa-times" aria-hidden="true"></i>
        </button>
      </header>
      <p>{{ texto }}</p>
    </dialog>
  `,
  styles: `
    .resumen {
      display: block;
      max-width: 24ch;
      padding: 0;
      border: 0;
      background: none;
      color: inherit;
      font: inherit;
      text-align: inherit;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      cursor: pointer;
    }
    .resumen:hover {
      text-decoration: underline;
    }
    dialog {
      width: min(560px, calc(100vw - 24px));
      max-height: calc(100dvh - 24px);
      padding: 0;
      border: 1px solid var(--border);
      border-radius: var(--radius-lg);
      color: var(--ink);
      background: var(--surface);
      font-family: var(--font);
      text-align: left;
      box-sizing: border-box;
      overflow: auto;
    }
    dialog::backdrop {
      background: rgba(0, 0, 0, 0.45);
    }
    header {
      position: relative;
      padding: 24px 64px 16px 24px;
      border-bottom: 1px solid var(--border);
    }
    header span {
      font-size: 13px;
      color: var(--ink-secondary);
    }
    h2 {
      font-size: 20px;
      margin: 4px 0 0;
      overflow-wrap: anywhere;
    }
    .modal-close-btn {
      position: absolute;
      right: 18px;
      top: 18px;
    }
    p {
      margin: 0;
      padding: 20px 24px 24px;
      line-height: 1.6;
      white-space: pre-line;
      overflow-wrap: anywhere;
    }
  `,
})
export class TextoExpandibleComponent {
  @Input() texto: string | null | undefined = '';
  @Input() titulo: string | null | undefined = '';
  @Input() etiqueta = 'Descripción';
}
