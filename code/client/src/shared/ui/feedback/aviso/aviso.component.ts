import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  WritableSignal,
} from '@angular/core';
@Component({
  selector: 'app-aviso',
  imports: [],
  templateUrl: './aviso.component.html',
  styleUrl: './aviso.component.css',
})
export class Aviso {
  @Input() cerrar: WritableSignal<boolean> = signal(true);
  @Input() mensaje: string = '';
  @Output() aceptar: EventEmitter<void> = new EventEmitter<void>();
  click() {
    this.cerrar.set(false);
  }
  onAceptar() {
    this.aceptar.emit();
    this.cerrar.set(false);
  }
}
