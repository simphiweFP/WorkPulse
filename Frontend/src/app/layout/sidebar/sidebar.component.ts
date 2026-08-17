import { AsyncPipe } from '@angular/common';
import { Component, EventEmitter, Input, Output, computed } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUser } from '../../core/models/user.models';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [AsyncPipe, RouterLink, RouterLinkActive],
  styleUrl: './sidebar.component.scss',
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent {
  readonly currentUser$;
  readonly currentUser = computed(() => this.authService.getCurrentUserSnapshot());
  @Input() open = false;
  @Output() closeDrawer = new EventEmitter<void>();

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {
    this.currentUser$ = this.authService.currentUser$;
  }

  isDeveloper(user: CurrentUser | null): boolean {
    return user?.role === 'Developer';
  }

  isAdmin(user: CurrentUser | null): boolean {
    return user?.role === 'Admin';
  }

  fullName(user: CurrentUser | null): string {
    return this.authService.getFullName(user);
  }

  initials(user: CurrentUser | null): string {
    return this.authService.getInitials(user);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
