import {
  Component,
  Input,
  OnDestroy,
  OnInit,
  WritableSignal,
} from '@angular/core';
@Component({
  selector: 'app-mostrarerror',
  imports: [],
  templateUrl: './mostrarerror.component.html',
  styleUrl: './mostrarerror.component.css',
})
export class MostrarerrorComponent implements OnInit, OnDestroy {
  @Input() error!: WritableSignal<boolean>;
  @Input() mensaje: string = 'Error desconocido , intente mas tarde';

  private timeoutId?: ReturnType<typeof setTimeout>;
  private cerrado = false;

  ngOnInit(): void {
    this.timeoutId = setTimeout(() => this.cerrar(), 1000);
  }

  cerrar(): void {
    if (this.cerrado) return;

    this.cerrado = true;
    if (this.timeoutId) clearTimeout(this.timeoutId);
    this.error.set(false);
  }

  ngOnDestroy(): void {
    if (this.timeoutId) clearTimeout(this.timeoutId);
  }
}
