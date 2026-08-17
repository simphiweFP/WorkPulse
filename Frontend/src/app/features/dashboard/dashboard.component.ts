import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { TaskService } from '../../core/services/task.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { FeedbackAlertService } from '../../shared/services/feedback-alert.service';
import { TaskCardComponent } from '../../shared/components/task-card/task-card.component';
import { TaskRecommendation, TodayDashboardResponse } from '../../shared/models/task.models';
import { Router } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadingStateComponent, EmptyStateComponent, TaskCardComponent],
  styleUrl: './dashboard.component.scss',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly alerts = inject(FeedbackAlertService);

  readonly dashboard = signal<TodayDashboardResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.reload();
  }

  greeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) {
      return 'Good morning';
    }
    if (hour < 18) {
      return 'Good afternoon';
    }
    return 'Good evening';
  }

  greetingName(model: TodayDashboardResponse | null): string {
    return model?.firstName || 'User';
  }

  viewTask(task: TaskRecommendation): void {
    void this.router.navigate(['/task-details', task.taskId]);
  }

  async performTaskAction(task: TaskRecommendation): Promise<void> {
    if (task.status === 'Todo') {
      if (!(await this.alerts.confirmAction('Start task?', 'This will move the task into progress.', 'Start'))) {
        return;
      }

      this.taskService.startTask(task.taskId).subscribe({
        next: () => {
          void this.alerts.success('Task started', 'The task was started successfully.');
          this.reload();
        },
        error: () => void this.alerts.error('Start failed', 'We could not start that task right now. Please try again.')
      });
      return;
    }

    if (task.status === 'InProgress') {
      if (!(await this.alerts.confirmAction('Complete task?', 'This will mark the task as completed.', 'Complete'))) {
        return;
      }

      this.taskService.completeTask(task.taskId).subscribe({
        next: () => {
          void this.alerts.success('Task completed', 'The task was completed successfully.');
          this.reload();
        },
        error: () => void this.alerts.error('Complete failed', 'We could not complete that task right now. Please try again.')
      });
    }
  }

  formatDate(value?: string): string {
    if (!value) {
      return new Date().toLocaleDateString(undefined, {
        weekday: 'long',
        day: 'numeric',
        month: 'long'
      });
    }

    return new Date(value).toLocaleDateString(undefined, {
      weekday: 'long',
      day: 'numeric',
      month: 'long'
    });
  }

  private reload(): void {
    this.loading.set(true);
    this.error.set('');

    const isAdmin = this.authService.getCurrentUserSnapshot()?.role === 'Admin';
    const todayRequest$ = isAdmin ? this.dashboardService.getAdminToday() : this.dashboardService.getTodayDashboard();

    forkJoin({
      today: todayRequest$,
      completed: this.taskService.getMyTasks({ status: 'Completed' }).pipe(catchError(() => of([] as TaskRecommendation[])))
    })
      .pipe(
        catchError(() => {
          this.error.set('Unable to load your Today Dashboard.');
          return of(null);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((result) => {
        if (!result) {
          return;
        }

        const todayTasks = [result.today.topPriority, ...result.today.overdue, ...result.today.dueToday, ...result.today.recommendedNext];
        const todayTaskIds = new Set(todayTasks.filter((task) => !!task?.taskId).map((task) => task.taskId));

        this.dashboard.set({
          ...result.today,
          completedToday: result.completed.filter((task) => !todayTaskIds.has(task.taskId)).slice(0, 4)
        });
      });
  }
}
