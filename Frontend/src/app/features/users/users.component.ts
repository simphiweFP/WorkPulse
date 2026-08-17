import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth/auth.service';
import { UserManagementItem, UsersService } from '../../core/services/users.service';
import { FeedbackAlertService } from '../../shared/services/feedback-alert.service';

const ASSIGNABLE_ROLES = ['Pending', 'Developer', 'Admin'];

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrl: './users.component.scss',
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);
  private readonly feedback = inject(FeedbackAlertService);

  readonly assignableRoles = ASSIGNABLE_ROLES;
  readonly users = signal<UserManagementItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly modalOpen = signal(false);
  readonly saving = signal(false);
  readonly selectedUser = signal<UserManagementItem | null>(null);
  readonly selectedRole = signal<string>('Pending');

  readonly totalUsers = computed(() => this.users().length);
  readonly pendingCount = computed(() => this.users().filter((u) => this.isPending(u)).length);
  readonly developerCount = computed(() => this.users().filter((u) => u.role === 'Developer').length);
  readonly adminCount = computed(() => this.users().filter((u) => u.role === 'Admin').length);

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.error.set(null);

    this.usersService.getUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('We could not load the user list. Please try again.');
        this.loading.set(false);
      }
    });
  }

  isPending(user: UserManagementItem): boolean {
    return user.isPending === true || user.role === 'Pending';
  }

  roleLabel(user: UserManagementItem): string {
    return this.isPending(user) ? 'Pending' : user.role;
  }

  roleBadgeClass(user: UserManagementItem): string {
    if (this.isPending(user)) {
      return 'badge badge--warning';
    }

    if (user.role === 'Admin') {
      return 'badge badge--admin';
    }

    return 'badge badge--developer';
  }

  isCurrentUser(user: UserManagementItem): boolean {
    return this.authService.getCurrentUserSnapshot()?.id === user.id;
  }

  openManageRole(user: UserManagementItem): void {
    this.selectedUser.set(user);
    this.selectedRole.set(this.roleLabel(user));
    this.modalOpen.set(true);
  }

  closeModal(): void {
    if (this.saving()) {
      return;
    }

    this.modalOpen.set(false);
    this.selectedUser.set(null);
  }

  async saveRole(): Promise<void> {
    const user = this.selectedUser();
    if (!user) {
      return;
    }

    const role = this.selectedRole();
    if (role === this.roleLabel(user)) {
      this.closeModal();
      return;
    }

    const confirmed = await this.feedback.confirmAction(
      'Update role',
      `Change ${user.fullName}'s access role to ${role}?`,
      'Update'
    );

    if (!confirmed) {
      return;
    }

    this.saving.set(true);

    this.usersService.updateRole(user.id, role).subscribe({
      next: () => {
        this.users.update((items) =>
          items.map((existing) =>
            existing.id === user.id
              ? { ...existing, role, isPending: role === 'Pending' }
              : existing
          )
        );
        this.saving.set(false);
        this.modalOpen.set(false);
        this.selectedUser.set(null);
        void this.feedback.success('Role updated', `${user.fullName} is now a ${role}. They must sign in again for the change to take effect.`);
      },
      error: (response) => {
        this.saving.set(false);
        const message = response?.error?.message ?? 'We could not update the role. Please try again.';
        void this.feedback.error('Update failed', message);
      }
    });
  }
}
