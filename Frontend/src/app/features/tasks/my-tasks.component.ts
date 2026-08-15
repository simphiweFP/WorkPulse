import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { TaskRecommendation } from '../../shared/models/task.models';
import { TaskService } from '../../core/services/task.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/components/error-state/error-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PriorityBadgeComponent } from '../../shared/components/priority-badge/priority-badge.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'app-my-tasks',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent, PriorityBadgeComponent, StatusBadgeComponent],
  templateUrl: './my-tasks.component.html',
  styleUrl: './my-tasks.component.scss'
})
export class MyTasksComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly tasks = signal<TaskRecommendation[]>([]);
  readonly sourceTasks = signal<TaskRecommendation[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');
  readonly activeTab = signal<'active' | 'backlog' | 'completed'>(this.router.url.includes('/backlog') ? 'backlog' : 'active');

  readonly filters = this.fb.group({
    search: [''],
    priority: ['']
  });

  pageTitle(): string {
    return this.router.url.includes('/backlog') ? 'Backlog' : 'My Tasks';
  }

  pageSubtitle(): string {
    return this.router.url.includes('/backlog')
      ? 'Work that is still in backlog and not assigned to a sprint yet.'
      : 'View and progress the work assigned to you.';
  }

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.error.set('');
    this.notice.set('');
    this.taskService
      .getMyTasks()
      .pipe(
        catchError(() => {
          this.error.set('We could not load your tasks right now. Please try again in a moment.');
          return of([] as TaskRecommendation[]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((tasks) => {
        this.sourceTasks.set(tasks);
        this.tasks.set(tasks);
        this.applyFilters();
      });
  }

  setTab(tab: 'active' | 'backlog' | 'completed'): void {
    this.activeTab.set(tab);
    this.applyFilters();
  }

  applyFilters(): void {
    const { search, priority } = this.filters.getRawValue();
    const filtered = this.sourceTasks().filter((task) => {
      const matchesTab = this.matchesTab(task);
      const matchesSearch = !search || `${task.title} ${task.clientName} ${task.projectName}`.toLowerCase().includes(search.toLowerCase());
      const matchesPriority = !priority || task.priority === priority;
      return matchesTab && matchesSearch && matchesPriority;
    });
    this.tasks.set(filtered);
  }

  resetFilters(): void {
    this.filters.reset();
    this.activeTab.set(this.router.url.includes('/backlog') ? 'backlog' : 'active');
    this.applyFilters();
  }

  performTaskAction(task: TaskRecommendation): void {
    if (task.status === 'Todo') {
      this.startTask(task.taskId);
      return;
    }

    if (task.status === 'InProgress') {
      this.completeTask(task.taskId);
      return;
    }

    this.viewTask(task);
  }

  startTask(taskId: string): void {
    this.notice.set('');
    this.taskService
      .startTask(taskId)
      .pipe(
        catchError(() => {
          this.notice.set('We could not start that task just now. Please try again.');
          return of(null);
        })
      )
      .subscribe(() => this.loadTasks());
  }

  completeTask(taskId: string): void {
    this.notice.set('');
    this.taskService
      .completeTask(taskId)
      .pipe(
        catchError(() => {
          this.notice.set('We could not complete that task just now. Please try again.');
          return of(null);
        })
      )
      .subscribe(() => this.loadTasks());
  }

  viewTask(task: TaskRecommendation): void {
    void task;
  }

  visibleTasks(): TaskRecommendation[] {
    return this.tasks();
  }

  taskReason(task: TaskRecommendation): string {
    return task.reason?.trim() || 'No delivery note provided.';
  }

  private matchesTab(task: TaskRecommendation): boolean {
    switch (this.activeTab()) {
      case 'completed':
        return task.status === 'Completed';
      case 'backlog':
        return task.status === 'Todo' && (task.sprintId == null || task.sprintName === 'Backlog');
      default:
        return task.status !== 'Completed';
    }
  }
}
