import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  WritableSignal,
} from '@angular/core';
@Component({
  selector: 'app-aviso-exito',
  imports: [],
  templateUrl: './aviso-exito.component.html',
  styleUrl: './aviso-exito.component.css',
})
export class AvisoExitoComponent implements OnInit, OnDestroy {
  @Input() mensaje: string = 'Aviso informativo desconocido';
  @Input() exito!: WritableSignal<boolean>;
  @Output() accion = new EventEmitter<void>();

  private timeoutId?: ReturnType<typeof setTimeout>;
  private cerrado = false;

  ngOnInit(): void {
    this.timeoutId = setTimeout(() => this.cerrar(), 1000);
  }

  cerrar() {
    if (this.cerrado) return;

    this.cerrado = true;
    if (this.timeoutId) clearTimeout(this.timeoutId);
    this.accion.emit();
    this.exito.set(false);
  }

  ngOnDestroy(): void {
    if (this.timeoutId) clearTimeout(this.timeoutId);
  }
}
