import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';
import { CurrentUser } from '../../core/models/user.models';
import { FeedbackAlertService } from '../../shared/services/feedback-alert.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  styleUrl: './profile.component.scss',
  templateUrl: './profile.component.html'
})
export class ProfileComponent {
  private readonly authService = inject(AuthService);
  private readonly alerts = inject(FeedbackAlertService);
  readonly emailNotifications = signal(true);
  readonly taskReminders = signal(true);

  user(): CurrentUser | null {
    return this.authService.getCurrentUserSnapshot();
  }

  fullName(): string {
    return this.authService.getFullName(this.user()) || 'User';
  }

  initials(): string {
    return this.authService.getInitials(this.user());
  }

  toggleEmailNotifications(): void {
    this.emailNotifications.update((value) => !value);
    void this.alerts.success('Preferences updated', 'Email notification preferences were updated.');
  }

  toggleTaskReminders(): void {
    this.taskReminders.update((value) => !value);
    void this.alerts.success('Preferences updated', 'Task reminder preferences were updated.');
  }
}
