import { CommonModule } from '@angular/common';
import { Component, EventEmitter, HostListener, Output, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUser } from '../../core/models/user.models';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './navbar.component.scss',
  templateUrl: './navbar.component.html'
})
export class NavbarComponent {
  @Output() readonly menuToggle = new EventEmitter<void>();
  @Output() readonly closeDrawer = new EventEmitter<void>();

  readonly searchOpen = signal(false);
  readonly today = new Date();

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  pageTitle(): string {
    const url = this.router.url;
    if (url.startsWith('/today')) {
      return 'Today';
    }
    if (url.startsWith('/backlog')) {
      return 'Backlog';
    }
    if (url.startsWith('/sprints')) {
      return 'Sprints';
    }
    if (url.startsWith('/clients')) {
      return 'Clients';
    }
    if (url.startsWith('/projects')) {
      return 'Projects';
    }
    if (url.startsWith('/tasks')) {
      return 'Tasks';
    }
    if (url.startsWith('/task-details')) {
      return 'Task Details';
    }
    if (url.startsWith('/my-tasks')) {
      return 'My Tasks';
    }
    if (url.startsWith('/team')) {
      return 'Team';
    }
    if (url.startsWith('/profile')) {
      return 'Profile';
    }
    return 'WorkPulse';
  }

  private currentUser(): CurrentUser | null {
    return this.authService.getCurrentUserSnapshot();
  }

  userName(): string {
    return this.authService.getFullName(this.currentUser()) || 'User';
  }

  role(): string {
    return this.currentUser()?.role ?? 'Member';
  }

  initials(): string {
    return this.authService.getInitials(this.currentUser());
  }

  toggleMenu(): void {
    this.router.navigate(['/profile']);
  }

  toggleSearch(): void {
    this.searchOpen.update((value) => !value);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.searchOpen.set(false);
    this.closeDrawer.emit();
  }
}
