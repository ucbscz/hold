import { BRAND, EXTERNAL_SERVICES } from '@shared/config';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-terminos',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './terminos.component.html',
  styleUrl: './terminos.component.css',
})
export class TerminosComponent {
  protected readonly servicios = EXTERNAL_SERVICES;
  protected readonly marca = BRAND;
}
