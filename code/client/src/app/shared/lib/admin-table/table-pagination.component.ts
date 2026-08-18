import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-table-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (totalItems > pageSize) {
      <nav class="table-pagination" aria-label="Paginación de tabla">
        <button
          type="button"
          class="table-pagination__arrow"
          (click)="goTo(page - 1)"
          [disabled]="page === 1"
          aria-label="Página anterior"
        >
          <img src="assets/back_line.png" alt="" width="29" height="29" />
        </button>
        <div class="table-pagination__numbers">
          @for (item of visiblePages; track item) {
            <button
              type="button"
              class="table-pagination__page"
              [class.table-pagination__current]="item === page"
              (click)="goTo(item)"
              [attr.aria-current]="item === page ? 'page' : null"
              [attr.aria-label]="'Página ' + item"
            >
              {{ item }}
            </button>
          }
        </div>
        <button
          type="button"
          class="table-pagination__arrow"
          (click)="goTo(page + 1)"
          [disabled]="page === totalPages"
          aria-label="Página siguiente"
        >
          <img src="assets/forward_line.png" alt="" width="29" height="29" />
        </button>
      </nav>
    }
  `,
  styles: [
    `
      :host {
        display: block;
        flex: 0 0 100%;
        order: 100;
        width: 100%;
      }
      .table-pagination {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        width: 100%;
        margin: 1.25rem 0 0.5rem;
        font-family: var(--font);
      }
      .table-pagination__numbers {
        display: flex;
        justify-content: center;
        gap: 0.5rem;
        min-width: 14.5rem;
      }
      .table-pagination__page {
        display: grid;
        place-items: center;
        flex: 0 0 2.5rem;
        width: 2.5rem;
        height: 2.5rem;
        min-height: 0;
        padding: 0;
        border: 1px solid var(--border);
        border-radius: 0.5rem;
        background: var(--surface);
        color: var(--ink-muted);
        font: 500 0.875rem var(--font);
        cursor: pointer;
        transition:
          color var(--t-fast),
          background-color var(--t-fast),
          border-color var(--t-fast);
      }
      .table-pagination__current {
        background: var(--brand);
        border-color: var(--brand);
        color: var(--brand-text);
        font-weight: 700;
      }
      .table-pagination__arrow {
        display: grid;
        place-items: center;
        flex: 0 0 1.8rem;
        width: 1.8rem;
        height: 1.8rem;
        min-height: 0;
        padding: 0;
        border: 0;
        border-radius: 0.5rem;
        background: transparent;
        cursor: pointer;
      }
      .table-pagination__arrow img {
        width: 100%;
        height: 100%;
        object-fit: contain;
        opacity: 0.7;
      }
      .table-pagination__arrow:disabled {
        visibility: hidden;
        pointer-events: none;
      }
      @media (hover: hover) and (pointer: fine) {
        .table-pagination__page:hover:not(.table-pagination__current) {
          background: var(--sidebar);
          color: var(--ink-secondary);
        }
        .table-pagination__current:hover {
          background: var(--brand-hover);
          border-color: var(--brand-hover);
        }
      }
      @media (max-width: 480px) {
        .table-pagination__numbers {
          min-width: 0;
        }
        .table-pagination__page {
          flex-basis: 2.25rem;
          width: 2.25rem;
          height: 2.25rem;
        }
      }
    `,
  ],
})
export class TablePaginationComponent {
  @Input({ required: true }) totalItems = 0;
  @Input() page = 1;
  @Input() pageSize = 10;
  @Output() pageChange = new EventEmitter<number>();

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalItems / this.pageSize));
  }
  get visiblePages(): number[] {
    const maxButtons = 5;
    if (this.totalPages <= maxButtons)
      return Array.from({ length: this.totalPages }, (_, index) => index + 1);

    const halfRange = Math.floor(maxButtons / 2);
    const start = Math.min(
      Math.max(1, this.page - halfRange),
      this.totalPages - maxButtons + 1,
    );

    return Array.from({ length: maxButtons }, (_, index) => start + index);
  }
  goTo(page: number): void {
    this.pageChange.emit(Math.min(Math.max(1, page), this.totalPages));
  }
}
