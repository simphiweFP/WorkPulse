import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { TaskService } from '../../core/services/task.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { TaskCardComponent } from '../../shared/components/task-card/task-card.component';
import { TaskRecommendation, TodayDashboardResponse } from '../../shared/models/task.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, TaskCardComponent],
  styleUrl: './dashboard.component.scss',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);

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
    return model?.firstName || this.authService.getFullName(this.authService.getCurrentUserSnapshot()) || 'User';
  }

  performTaskAction(task: TaskRecommendation): void {
    if (task.status === 'Todo') {
      this.taskService.startTask(task.taskId).subscribe(() => this.reload());
      return;
    }

    if (task.status === 'InProgress') {
      this.taskService.completeTask(task.taskId).subscribe(() => this.reload());
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

    forkJoin({
      today: this.dashboardService.getTodayDashboard(),
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

        this.dashboard.set({
          ...result.today,
          completedToday: result.completed.slice(0, 4)
        });
      });
  }
}
