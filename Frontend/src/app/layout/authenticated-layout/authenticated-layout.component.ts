import { AsyncPipe } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-authenticated-layout',
  standalone: true,
  imports: [RouterLink, RouterOutlet, AsyncPipe],
  templateUrl: './authenticated-layout.component.html',
  styleUrl: './authenticated-layout.component.scss'
})
export class AuthenticatedLayoutComponent {
  readonly currentUser$;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
    this.currentUser$ = this.authService.currentUser$;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
