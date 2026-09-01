import { CommonModule } from '@angular/common';
import { Component, signal, WritableSignal } from '@angular/core';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
  RouterOutlet,
} from '@angular/router';
import { LoanReturnNavigationService } from '@features/cart';
import {
  AvisoExitoComponent,
  PantallaCargaComponent,
  ToastService,
} from '@shared/ui';
import { NavbarComponent } from '@widgets/navigation';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: true,
  imports: [
    NavbarComponent,
    RouterOutlet,
    PantallaCargaComponent,
    AvisoExitoComponent,
    CommonModule,
  ],
})
export class AppComponent {
  cargando: WritableSignal<boolean> = signal(false);

  constructor(
    private readonly router: Router,
    private readonly loanReturnNavigation: LoanReturnNavigationService,
    readonly toast: ToastService,
  ) {
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationStart) {
        this.cargando.set(true);
      } else if (
        event instanceof NavigationEnd ||
        event instanceof NavigationCancel ||
        event instanceof NavigationError
      ) {
        this.cargando.set(false);

        if (event instanceof NavigationEnd) {
          this.loanReturnNavigation.track(event.urlAfterRedirects);
        }
      }
    });
  }
}
