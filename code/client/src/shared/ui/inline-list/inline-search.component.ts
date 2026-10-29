import { Component, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { INLINE_SEARCH_STYLES } from './inline-search.styles';

@Component({
  selector: 'app-inline-search',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="audit-search-wrap">
      <i class="fas fa-search search-icon"></i>
      <input
        type="text"
        [ngModel]="texto()"
        (ngModelChange)="texto.set($event)"
        [placeholder]="placeholder()"
        class="admin-search"
      />
      @if (texto()) {
        <button
          class="clear-search"
          (click)="texto.set('')"
          title="Limpiar búsqueda"
          type="button"
        >
          <i class="fas fa-times"></i>
        </button>
      }
    </div>
  `,
  styles: [INLINE_SEARCH_STYLES],
})
export class InlineSearchComponent {
  readonly texto = model('');
  readonly placeholder = input('');
}
