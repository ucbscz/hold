import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
  ViewChild,
  WritableSignal,
  signal,
} from '@angular/core';
import SignaturePad from 'signature_pad';
@Component({
  selector: 'app-firma',
  imports: [CommonModule],
  templateUrl: './firma.component.html',
  styleUrl: './firma.component.css',
})
export class FirmaComponent implements AfterViewInit, OnDestroy {
  @ViewChild('signatureCanvas') signatureCanvas!: ElementRef<HTMLCanvasElement>;
  signaturePad!: SignaturePad;
  signatureData: string = '';
  signatureError = '';
  @Output() firma = new EventEmitter<string>();
  @Input() clickfirma: WritableSignal<boolean> = signal(true);

  private readonly resizeListener = (): void => this.resizeCanvas();

  close(): void {
    this.clickfirma.set(false);
  }
  ngAfterViewInit(): void {
    const canvas = this.signatureCanvas.nativeElement;
    this.signaturePad = new SignaturePad(canvas, {
      throttle: 0,
      minDistance: 1,
      minWidth: 0.75,
      maxWidth: 2.5,
    });
    this.resizeCanvas();
    window.addEventListener('resize', this.resizeListener);
  }

  ngOnDestroy(): void {
    window.removeEventListener('resize', this.resizeListener);
  }
  private resizeCanvas(): void {
    const canvas = this.signatureCanvas.nativeElement;
    const ratio = Math.min(Math.max(window.devicePixelRatio || 1, 1), 2);
    canvas.width = canvas.offsetWidth * ratio;
    canvas.height = canvas.offsetHeight * ratio;
    canvas.getContext('2d')?.scale(ratio, ratio);
    this.signaturePad.clear();
  }
  clearSignature(): void {
    this.signaturePad.clear();
    this.signatureData = '';
    this.signatureError = '';
  }
  saveSignature(): void {
    if (!this.signaturePad.isEmpty()) {
      this.signatureData = this.signaturePad.toDataURL();
      this.signatureError = '';
      this.firma.emit(this.signatureData);
      this.clickfirma.set(false);
    } else {
      this.signatureError = 'Firma antes de guardar.';
    }
  }
}
