import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { catchError, finalize, of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';
import { TaskRecommendation } from '../../shared/models/task.models';
import { TaskService } from '../../core/services/task.service';

@Component({
  selector: 'app-my-tasks',
  standalone: true,
  imports: [CommonModule, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent, PriorityBadgeComponent, StatusBadgeComponent],
  templateUrl: './my-tasks.component.html',
  styleUrl: './my-tasks.component.scss'
})
export class MyTasksComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly authService = inject(AuthService);

  readonly loading = signal(true);
  readonly error = signal('');
  readonly tasks = signal<TaskRecommendation[]>([]);
  readonly activeTab = signal< 'upcoming' | 'completed'>('upcoming');

  readonly currentSprintTasks = computed(() => this.tasks().filter((task) => this.isCurrentSprint(task) && task.status !== 'Completed'));
  readonly upcomingTasks = computed(() => this.tasks().filter((task) => this.isUpcoming(task)));
  readonly completedTasks = computed(() => this.tasks().filter((task) => task.status === 'Completed'));

  readonly summary = computed(() => {
    const tasks = this.currentSprintTasks();
    return {
      currentSprintCount: tasks.length,
      storyPoints: tasks.reduce((sum, task) => sum + (task.storyPoints ?? 0), 0),
      inProgress: tasks.filter((task) => task.status === 'InProgress').length,
      blocked: tasks.filter((task) => this.isBlocked(task)).length
    };
  });

  readonly currentUserName = computed(() => this.authService.getFullName(this.authService.getCurrentUserSnapshot()) || 'Admin User');

  tabLabel(tab: 'upcoming' | 'completed'): string {
    switch (tab) {
      case 'upcoming':
        return 'Upcoming';
      case 'completed':
        return 'Completed';
    }
  }

  setActiveTab(tab: 'upcoming' | 'completed'): void {
    this.activeTab.set(tab);
    const element = document.getElementById(tab);
    element?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  activeTabTitle(): string {
    return this.tabLabel(this.activeTab());
  }

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.error.set('');

    this.taskService
      .getMyTasks()
      .pipe(
        catchError(() => {
          this.error.set('We could not load your tasks right now. Please try again in a moment.');
          return of([] as TaskRecommendation[]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((tasks) => this.tasks.set(tasks));
  }

  sprintLabel(task: TaskRecommendation): string {
    return task.sprintName?.trim() ? task.sprintName : 'Unplanned';
  }

  taskDueLabel(task: TaskRecommendation): string {
    if (!task.deadline) {
      return 'No due date';
    }

    return new Date(task.deadline).toLocaleDateString(undefined, { day: 'numeric', month: 'short' });
  }

  taskSummary(task: TaskRecommendation): string {
    const segments = [task.status, `${task.storyPoints ?? 0} SP`, task.priority];
    return segments.filter(Boolean).join(' · ');
  }

  taskMeta(task: TaskRecommendation): string {
    return `${this.sprintLabel(task)}${task.deadline ? ` · Due ${this.taskDueLabel(task)}` : ''}`;
  }

  blockedTasks(): TaskRecommendation[] {
    return this.currentSprintTasks().filter((task) => this.isBlocked(task));
  }

  hasBlockedTasks(): boolean {
    return this.blockedTasks().length > 0;
  }

  visibleSection(): 'upcoming' | 'completed' {
    return this.activeTab();
  }

  private isCurrentSprint(task: TaskRecommendation): boolean {
    return !!task.sprintId && task.sprintName?.trim() !== 'Backlog';
  }

  private isUpcoming(task: TaskRecommendation): boolean {
    return task.status === 'Todo' && (!task.sprintId || task.sprintName?.trim() === 'Backlog');
  }

  private isBlocked(task: TaskRecommendation): boolean {
    return task.reason?.toLowerCase().includes('blocked');
  }
}
