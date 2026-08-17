import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { catchError, forkJoin, finalize, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { DashboardService, AdminDashboardResponse } from '../../core/services/dashboard.service';
import { TaskService } from '../../core/services/task.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { TaskPriority } from '../../shared/models/task.models';
import { TaskAssigneeOption } from '../../core/models/task-admin.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, LoadingStateComponent, EmptyStateComponent],
  styleUrl: './admin-dashboard.component.scss',
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);

  readonly dashboard = signal<AdminDashboardResponse | null>(null);
  readonly developers = signal<TaskAssigneeOption[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  readonly sortedRecentTasks = computed(() => {
    const model = this.dashboard();
    return (model?.recentTasks ?? []).slice().sort((a, b) => {
      const priorityRank = this.priorityRank(b.priority) - this.priorityRank(a.priority);
      if (priorityRank !== 0) {
        return priorityRank;
      }

      const left = a.deadline ? new Date(a.deadline).getTime() : Number.MAX_SAFE_INTEGER;
      const right = b.deadline ? new Date(b.deadline).getTime() : Number.MAX_SAFE_INTEGER;
      return left - right;
    });
  });

  readonly topPriorities = computed(() =>
    this.sortedRecentTasks()
      .filter((task) => task.priority === 'Critical' || task.priority === 'High')
      .slice(0, 5)
  );

  readonly upcomingDeadlines = computed(() =>
    this.sortedRecentTasks()
      .filter((task) => !!task.deadline)
      .slice(0, 5)
  );

  readonly teamWorkload = computed(() =>
    [...this.developers()]
      .sort((a, b) => (b.activeTaskCount ?? 0) - (a.activeTaskCount ?? 0))
      .slice(0, 4)
  );

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      dashboard: this.dashboardService.getAdminDashboard().pipe(catchError(() => of(null))),
      developers: this.taskService.getDevelopers().pipe(catchError(() => of([] as TaskAssigneeOption[])))
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(({ dashboard, developers }) => {
        if (!dashboard) {
          this.error.set('Unable to load the admin dashboard right now.');
          return;
        }

        this.dashboard.set(dashboard);
        this.developers.set(developers);
      });
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

  greetingName(): string {
    return this.authService.getFullName(this.authService.getCurrentUserSnapshot()) || 'User';
  }

  percentOf(total: number, value: number): number {
    if (total <= 0) {
      return 0;
    }

    return Math.round((value / total) * 100);
  }

  priorityRank(priority: TaskPriority): number {
    switch (priority) {
      case 'Critical': return 4;
      case 'High': return 3;
      case 'Medium': return 2;
      default: return 1;
    }
  }

  formatDate(value?: string): string {
    if (!value) {
      return '';
    }

    return new Date(value).toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric'
    });
  }

  developerLoadLabel(count?: number): string {
    return `${count ?? 0} tasks`;
  }
}
