import { CommonModule, DatePipe, LowerCasePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { catchError, finalize, of } from 'rxjs';
import { TaskRecommendation } from '../../shared/models/task.models';
import { TaskService } from '../../core/services/task.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { LoadingStateComponent } from '../../shared/components/loading-state/loading-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-my-tasks',
  standalone: true,
  imports: [CommonModule, DatePipe, LowerCasePipe, ReactiveFormsModule, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent],
  template: `
    <section class="screen">
      <app-page-header eyebrow="My Tasks" title="My Tasks" subtitle="View and progress the work assigned to you." />

      @if (loading()) {
        <app-loading-state message="Loading your tasks..." />
      } @else if (error()) {
        <section class="error-state"><p>{{ error() }}</p></section>
      } @else {
        <section class="panel filters form-grid" [formGroup]="filters">
          <label><input placeholder="Search" formControlName="search" /></label>
          <label>
            <select formControlName="status">
              <option value="">Status</option>
              <option value="Todo">Todo</option>
              <option value="InProgress">In Progress</option>
              <option value="Completed">Completed</option>
            </select>
          </label>
          <label>
            <select formControlName="priority">
              <option value="">Priority</option>
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Critical">Critical</option>
            </select>
          </label>
          <div class="actions full-width">
            <button type="button" class="secondary" (click)="applyFilters()">Apply Filters</button>
            <button type="button" class="secondary" (click)="resetFilters()">Reset</button>
          </div>
        </section>

        @if (tasks().length) {
          <div class="task-list">
            @for (task of tasks(); track task.taskId) {
              <article class="task-card">
                <div class="task-card__header">
                  <div>
                    <h3>{{ task.title }}</h3>
                    <p>{{ task.clientName }} / {{ task.projectName }}</p>
                  </div>
                  <span class="priority priority-{{ task.priority | lowercase }}">{{ task.priority }}</span>
                </div>
                <div class="task-card__meta">
                  <span>{{ task.status }}</span>
                  <span>{{ task.deadline | date:'mediumDate' }}</span>
                </div>
                <p class="reason">{{ task.reason }}</p>
                <div class="task-card__actions">
                  @if (task.status === 'Todo') {
                    <button type="button" (click)="startTask(task.taskId)">Start</button>
                  }
                  @if (task.status === 'InProgress') {
                    <button type="button" (click)="completeTask(task.taskId)">Complete</button>
                  }
                  <button type="button" class="secondary" (click)="viewTask(task)">View</button>
                </div>
              </article>
            }
          </div>
        } @else {
          <app-empty-state title="You're clear for today." message="No assigned tasks need your attention right now." />
        }
      }
    </section>
  `
})
export class MyTasksComponent implements OnInit {
  private readonly taskService = inject(TaskService);
  private readonly fb = inject(FormBuilder);

  readonly tasks = signal<TaskRecommendation[]>([]);
  readonly sourceTasks = signal<TaskRecommendation[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly filters = this.fb.group({
    search: [''],
    status: [''],
    priority: ['']
  });

  ngOnInit(): void {
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.taskService
      .getMyTasks()
      .pipe(
        catchError(() => {
          this.error.set('Unable to load your tasks.');
          return of([] as TaskRecommendation[]);
        }),
        finalize(() => this.loading.set(false))
      )
      .subscribe((tasks) => {
        this.sourceTasks.set(tasks);
        this.tasks.set(tasks);
      });
  }

  applyFilters(): void {
    const { search, status, priority } = this.filters.getRawValue();
    const filtered = this.sourceTasks().filter((task) => {
      const matchesSearch = !search || `${task.title} ${task.clientName} ${task.projectName}`.toLowerCase().includes(search.toLowerCase());
      const matchesStatus = !status || task.status === status;
      const matchesPriority = !priority || task.priority === priority;
      return matchesSearch && matchesStatus && matchesPriority;
    });
    this.tasks.set(filtered);
  }

  resetFilters(): void {
    this.filters.reset();
    this.tasks.set(this.sourceTasks());
  }

  startTask(taskId: string): void {
    this.taskService.startTask(taskId).subscribe(() => this.loadTasks());
  }

  completeTask(taskId: string): void {
    this.taskService.completeTask(taskId).subscribe(() => this.loadTasks());
  }

  viewTask(task: TaskRecommendation): void {
    void task;
  }
}