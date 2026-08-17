import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { catchError, finalize, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { TaskAdminSummary } from '../../core/models/task-admin.models';
import { TaskService } from '../../core/services/task.service';

@Component({
  selector: 'app-backlog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LoadingStateComponent, ErrorStateComponent, EmptyStateComponent, PriorityBadgeComponent],
  templateUrl: './backlog.component.html',
  styleUrl: './backlog.component.scss'
})
export class BacklogComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly items = signal<TaskAdminSummary[]>([]);
  readonly filters = this.fb.group({
    search: [''],
    projectId: [''],
    priority: ['']
  });

  readonly backlogItems = computed(() => this.items().filter((item) => this.isBacklogWork(item)));
  readonly health = computed(() => {
    const backlogItems = this.backlogItems();

    return {
      items: backlogItems.length,
      storyPoints: backlogItems.reduce((sum, item) => sum + this.storyPointsValue(item), 0),
      readyToPlan: backlogItems.filter((item) => this.isPlanningReady(item)).length
    };
  });

  readonly planningReadyItems = computed(() => this.backlogItems().filter((item) => this.isPlanningReady(item)));
  readonly needsAttentionItems = computed(() => this.backlogItems().filter((item) => this.isNeedsAttention(item)));
  readonly otherBacklogItems = computed(() => this.backlogItems().filter((item) => !this.isPlanningReady(item) && !this.isNeedsAttention(item)));

  ngOnInit(): void {
    this.loadItems();
  }

  loadItems(): void {
    this.loading.set(true);
    this.error.set('');

    const isAdmin = this.authService.getCurrentUserSnapshot()?.role === 'Admin';
    const backlog$ = isAdmin ? this.taskService.getBacklog() : this.taskService.getDeveloperBacklog();

    backlog$
      .pipe(
        catchError(() => {
          this.error.set('We could not load the backlog right now. Please try again in a moment.');
          return of([] as TaskAdminSummary[]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((items) => this.items.set(items));
  }

  resetFilters(): void {
    this.filters.reset({
      search: '',
      projectId: '',
      priority: ''
    });
  }

  storyPointsLabel(item: TaskAdminSummary): string {
    return this.storyPointsValue(item) > 0 ? `${this.storyPointsValue(item)} SP` : 'Unestimated';
  }

  sprintLabel(item: TaskAdminSummary): string {
    return item.sprintName?.trim() ? item.sprintName : '—';
  }

  assigneeLabel(item: TaskAdminSummary): string {
    return item.assigneeName?.trim() ? item.assigneeName : 'Unassigned';
  }

  itemType(item: TaskAdminSummary): string {
    return item.type?.trim() ? item.type : 'Task';
  }

  readinessLabel(item: TaskAdminSummary): string {
    if ((item.storyPoints ?? 0) <= 0) {
      return 'Needs estimate';
    }

    if (!item.assigneeId) {
      return 'Needs assignment';
    }

    return 'Ready';
  }

  readinessClass(item: TaskAdminSummary): string {
    return `readiness-pill readiness-pill--${this.readinessLabel(item).toLowerCase().replace(/\s+/g, '-')}`;
  }

  private isBacklogWork(item: TaskAdminSummary): boolean {
    return item.status !== 'Completed' && !item.sprintId;
  }

  private isPlanningReady(item: TaskAdminSummary): boolean {
    return this.storyPointsValue(item) > 0 && !!item.assigneeId;
  }

  private isNeedsAttention(item: TaskAdminSummary): boolean {
    return this.storyPointsValue(item) <= 0 || !item.assigneeId;
  }

  private storyPointsValue(item: TaskAdminSummary): number {
    return item.storyPoints ?? 0;
  }
}
