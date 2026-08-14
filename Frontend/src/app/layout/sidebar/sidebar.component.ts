import { AsyncPipe } from '@angular/common';
import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUser } from '../../core/models/user.models';
import { authenticatedNavigation } from '../../shared/utilities/navigation';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [AsyncPipe, RouterLink],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent {
  readonly currentUser$;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
    this.currentUser$ = this.authService.currentUser$;
  }

  navigationItems(user: CurrentUser | null): typeof authenticatedNavigation {
    if (user?.role === 'Developer') {
      return authenticatedNavigation.filter((item) => item.path === '/dashboard' || item.path === '/my-tasks');
    }

    return authenticatedNavigation;
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}