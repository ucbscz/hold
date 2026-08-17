import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-table-pagination',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (totalItems > pageSize) {
      <nav class="table-pagination" aria-label="Paginación de tabla">
        <span class="table-pagination__summary"
          >{{ firstItem }}-{{ lastItem }} de {{ totalItems }}</span
        >
        <button
          type="button"
          (click)="goTo(page - 1)"
          [disabled]="page === 1"
          aria-label="Página anterior"
        >
          <i class="fas fa-chevron-left"></i>
        </button>
        @for (item of visiblePages; track item) {
          @if (item === 0) {
            <span class="table-pagination__ellipsis">…</span>
          } @else {
            <button
              type="button"
              [class.table-pagination__current]="item === page"
              (click)="goTo(item)"
            >
              {{ item }}
            </button>
          }
        }
        <button
          type="button"
          (click)="goTo(page + 1)"
          [disabled]="page === totalPages"
          aria-label="Página siguiente"
        >
          <i class="fas fa-chevron-right"></i>
        </button>
      </nav>
    }
  `,
  styles: [
    `
      .table-pagination {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: 0.375rem;
        margin: 1rem 0;
        color: var(--ink-secondary);
        font: 600 0.8125rem var(--font);
      }
      .table-pagination__summary {
        margin-right: 0.375rem;
      }
      button {
        width: 2rem;
        height: 2rem;
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        background: var(--surface);
        color: var(--ink-secondary);
        cursor: pointer;
      }
      button:hover:not(:disabled),
      .table-pagination__current {
        background: var(--interactive);
        border-color: var(--interactive);
        color: var(--brand-text);
      }
      button:disabled {
        cursor: not-allowed;
        opacity: 0.45;
      }
      .table-pagination__ellipsis {
        width: 1.25rem;
        text-align: center;
      }
      @media (max-width: 480px) {
        .table-pagination {
          justify-content: center;
        }
        .table-pagination__summary {
          display: none;
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
  get firstItem(): number {
    return this.totalItems === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }
  get lastItem(): number {
    return Math.min(this.page * this.pageSize, this.totalItems);
  }
  get visiblePages(): number[] {
    if (this.totalPages <= 5)
      return Array.from({ length: this.totalPages }, (_, index) => index + 1);
    const pages = [1];
    if (this.page > 3) pages.push(0);
    for (
      let current = Math.max(2, this.page - 1);
      current <= Math.min(this.totalPages - 1, this.page + 1);
      current++
    )
      pages.push(current);
    if (this.page < this.totalPages - 2) pages.push(0);
    pages.push(this.totalPages);
    return [...new Set(pages)];
  }
  goTo(page: number): void {
    this.pageChange.emit(Math.min(Math.max(1, page), this.totalPages));
  }
}
