import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { catchError, finalize, of } from 'rxjs';
import { DashboardService } from '../../core/services/dashboard.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { TaskCardComponent } from '../../shared/components/task-card/task-card.component';
import { TodayDashboardResponse } from '../../shared/models/task.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, TaskCardComponent],
  template: `
    <section class="dashboard">
      @if (loading()) {
        <app-loading-state message="Loading today's recommendations..." />
      } @else if (error()) {
        <section class="error-state">
          <h2>Dashboard unavailable</h2>
          <p>{{ error() }}</p>
        </section>
      } @else if (dashboard(); as model) {
        <app-page-header
          eyebrow="Today Dashboard"
          [title]="'Good morning, ' + model.firstName"
          subtitle="Here's what needs your attention today."
          [meta]="formatDate(model.date)"
        />

        <div class="summary-grid">
          <article><span>Tasks Today</span><strong>{{ model.summary.tasksToday }}</strong></article>
          <article><span>Overdue</span><strong>{{ model.summary.overdue }}</strong></article>
          <article><span>Due Today</span><strong>{{ model.summary.dueToday }}</strong></article>
          <article><span>High / Critical</span><strong>{{ model.summary.highOrCritical }}</strong></article>
        </div>

        <section class="task-groups">
          <div>
            <h2>Overdue</h2>
            @if (model.overdue.length) {
              <div class="task-list">
                @for (task of model.overdue; track task.taskId) {
                  <app-task-card [task]="task" actionLabel="Start" />
                }
              </div>
            } @else {
              <app-empty-state title="You're clear for today." message="No overdue tasks need your attention." />
            }
          </div>

          <div>
            <h2>Due Today</h2>
            @if (model.dueToday.length) {
              <div class="task-list">
                @for (task of model.dueToday; track task.taskId) {
                  <app-task-card [task]="task" [actionLabel]="task.status === 'Todo' ? 'Start' : 'Complete'" />
                }
              </div>
            } @else {
              <app-empty-state title="You're clear for today." message="No tasks are due today." />
            }
          </div>

          <div>
            <h2>Recommended Next</h2>
            @if (model.recommendedNext.length) {
              <div class="task-list">
                @for (task of model.recommendedNext; track task.taskId) {
                  <app-task-card [task]="task" actionLabel="Start" />
                }
              </div>
            } @else {
              <app-empty-state title="You're clear for today." message="No urgent or upcoming tasks need your attention." />
            }
          </div>

          @if (model.completedToday.length) {
            <div>
              <h2>Completed Today</h2>
              <div class="task-list">
                @for (task of model.completedToday; track task.taskId) {
                  <app-task-card [task]="task" actionLabel="" />
                }
              </div>
            </div>
          }
        </section>
      }
    </section>
  `
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  readonly dashboard = signal<TodayDashboardResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.dashboardService
      .getTodayDashboard()
      .pipe(
        catchError(() => {
          this.error.set('Unable to load your Today Dashboard.');
          return of(null);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((model) => {
        if (model) {
          this.dashboard.set(model);
        }
      });
  }

  formatDate(value: string): string {
    return new Date(value).toLocaleDateString(undefined, {
      weekday: 'long',
      day: 'numeric',
      month: 'long'
    });
  }
}